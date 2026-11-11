using System.Net;
using System.Net.Http.Json;
using SchoolAdmission.Api.DTOs;
using Xunit;

namespace SchoolAdmission.Tests;

public class HoldWorkflowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HoldWorkflowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteHoldLifecycle_HoldConfirmCancelAndIdempotency()
    {
        var student1 = "20152691234567890";
        var student2 = "20152691234567891";

        // 1. Create a class
        var classResp = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 8 - Morning",
            TotalSeats = 10
        });
        var createdClass = await classResp.Content.ReadFromJsonAsync<ClassCreatedResponseDto>();
        Assert.NotNull(createdClass);

        // 2. Request hold without X-Student-Id -> 400 Bad Request
        var badHoldReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        var badHoldResp = await _client.SendAsync(badHoldReq);
        Assert.Equal(HttpStatusCode.BadRequest, badHoldResp.StatusCode);

        // 3. Request hold with invalid length X-Student-Id -> 400 Bad Request
        var invalidIdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        invalidIdReq.Headers.Add("X-Student-Id", "123");
        var invalidIdResp = await _client.SendAsync(invalidIdReq);
        Assert.Equal(HttpStatusCode.BadRequest, invalidIdResp.StatusCode);

        // 4. Request valid hold for student1 -> 201 Created
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", student1);
        var holdResp = await _client.SendAsync(holdReq);
        Assert.Equal(HttpStatusCode.Created, holdResp.StatusCode);

        var holdData = await holdResp.Content.ReadFromJsonAsync<HoldCreatedResponseDto>();
        Assert.NotNull(holdData);
        Assert.False(string.IsNullOrEmpty(holdData.HoldId));
        Assert.Equal("HELD", holdData.Status);
        Assert.Equal(createdClass.Id, holdData.ClassId);
        Assert.Equal(student1, holdData.StudentId);
        Assert.False(string.IsNullOrEmpty(holdData.ExpiresAt));

        // 5. Verify Rule 3: student1 cannot create another hold in same class while active -> 409 Conflict
        var dupReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        dupReq.Headers.Add("X-Student-Id", student1);
        var dupResp = await _client.SendAsync(dupReq);
        Assert.Equal(HttpStatusCode.Conflict, dupResp.StatusCode);

        // 6. Check hold status via GET /holds/{holdId}
        var getHoldResp = await _client.GetAsync($"/holds/{holdData.HoldId}");
        Assert.Equal(HttpStatusCode.OK, getHoldResp.StatusCode);
        var statusData = await getHoldResp.Content.ReadFromJsonAsync<HoldStatusResponseDto>();
        Assert.NotNull(statusData);
        Assert.Equal("HELD", statusData.Status);

        // 7. Verify Rule 4: Another student cannot confirm student1's hold -> 403 Forbidden
        var foreignConfirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData.HoldId}/confirm");
        foreignConfirmReq.Headers.Add("X-Student-Id", student2);
        var foreignConfirmResp = await _client.SendAsync(foreignConfirmReq);
        Assert.Equal(HttpStatusCode.Forbidden, foreignConfirmResp.StatusCode);

        // 8. Confirm hold for student1 -> 200 OK
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData.HoldId}/confirm");
        confirmReq.Headers.Add("X-Student-Id", student1);
        var confirmResp = await _client.SendAsync(confirmReq);
        Assert.Equal(HttpStatusCode.OK, confirmResp.StatusCode);
        var confirmData = await confirmResp.Content.ReadFromJsonAsync<HoldConfirmResponseDto>();
        Assert.NotNull(confirmData);
        Assert.Equal("CONFIRMED", confirmData.Status);

        // 9. Rule 5: Idempotency - Confirming again returns 200 OK and confirmedSeats does not double
        var confirmAgainReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{holdData.HoldId}/confirm");
        confirmAgainReq.Headers.Add("X-Student-Id", student1);
        var confirmAgainResp = await _client.SendAsync(confirmAgainReq);
        Assert.Equal(HttpStatusCode.OK, confirmAgainResp.StatusCode);

        // Check class seats: 1 confirmed, 0 held, 9 available
        var checkClassResp = await _client.GetAsync($"/classes/{createdClass.Id}");
        var checkClass = await checkClassResp.Content.ReadFromJsonAsync<ClassSummaryDto>();
        Assert.NotNull(checkClass);
        Assert.Equal(1, checkClass.ConfirmedSeats);
        Assert.Equal(0, checkClass.HeldSeats);
        Assert.Equal(9, checkClass.AvailableSeats);

        // 10. Check GET /me/holds for student1
        var meReq = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        meReq.Headers.Add("X-Student-Id", student1);
        var meResp = await _client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);
        var myHolds = await meResp.Content.ReadFromJsonAsync<List<MyHoldResponseDto>>();
        Assert.NotNull(myHolds);
        Assert.Contains(myHolds, h => h.HoldId == holdData.HoldId && h.Status == "CONFIRMED");
    }

    [Fact]
    public async Task CancelHold_ReleasesSeatBackToAvailable()
    {
        var studentId = "20152691234567899";

        // Create class with 2 seats
        var classResp = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class Cancel Test",
            TotalSeats = 2
        });
        var createdClass = await classResp.Content.ReadFromJsonAsync<ClassCreatedResponseDto>();
        Assert.NotNull(createdClass);

        // Hold seat
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", studentId);
        var holdResp = await _client.SendAsync(holdReq);
        var holdData = await holdResp.Content.ReadFromJsonAsync<HoldCreatedResponseDto>();
        Assert.NotNull(holdData);

        // Check held seats = 1, available = 1
        var c1 = await (await _client.GetAsync($"/classes/{createdClass.Id}")).Content.ReadFromJsonAsync<ClassSummaryDto>();
        Assert.NotNull(c1);
        Assert.Equal(1, c1.HeldSeats);
        Assert.Equal(1, c1.AvailableSeats);

        // Cancel hold -> 204 No Content
        var cancelReq = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{holdData.HoldId}");
        cancelReq.Headers.Add("X-Student-Id", studentId);
        var cancelResp = await _client.SendAsync(cancelReq);
        Assert.Equal(HttpStatusCode.NoContent, cancelResp.StatusCode);

        // Check seat is released: held = 0, available = 2
        var c2 = await (await _client.GetAsync($"/classes/{createdClass.Id}")).Content.ReadFromJsonAsync<ClassSummaryDto>();
        Assert.NotNull(c2);
        Assert.Equal(0, c2.HeldSeats);
        Assert.Equal(2, c2.AvailableSeats);
    }
}
