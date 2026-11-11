using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using SchoolAdmission.Api.DTOs;
using Xunit;

namespace SchoolAdmission.Tests;

public class ConcurrencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConcurrencyTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ConcurrentHoldRequests_NeverExceedTotalSeats()
    {
        const int totalSeats = 5;
        const int totalApplicants = 30;

        // 1. Create a class with strictly limited seats (e.g. 5 seats)
        var createResponse = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "High Demand Class - Concurrency Test",
            TotalSeats = totalSeats
        });
        var createdClass = await createResponse.Content.ReadFromJsonAsync<ClassCreatedResponseDto>();
        Assert.NotNull(createdClass);
        var classId = createdClass.Id;

        // 2. Prepare 30 distinct 17-digit student IDs
        var studentIds = Enumerable.Range(1, totalApplicants)
            .Select(i => $"201500000000{i:D5}")
            .ToList();

        var successCount = 0;
        var conflictCount = 0;
        var otherStatusCodes = new ConcurrentBag<HttpStatusCode>();
        var heldIds = new ConcurrentBag<string>();

        // 3. Fire all 30 hold requests simultaneously
        var tasks = studentIds.Select(async studentId =>
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
            req.Headers.Add("X-Student-Id", studentId);

            var response = await _client.SendAsync(req);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                Interlocked.Increment(ref successCount);
                var body = await response.Content.ReadFromJsonAsync<HoldCreatedResponseDto>();
                if (body != null)
                {
                    heldIds.Add(body.HoldId);
                }
            }
            else if (response.StatusCode == HttpStatusCode.Conflict)
            {
                Interlocked.Increment(ref conflictCount);
            }
            else
            {
                otherStatusCodes.Add(response.StatusCode);
            }
        });

        await Task.WhenAll(tasks);

        // 4. Assert: Exactly 5 succeeded, exactly 25 got 409 Conflict, 0 unexpected codes
        Assert.Empty(otherStatusCodes);
        Assert.Equal(totalSeats, successCount);
        Assert.Equal(totalApplicants - totalSeats, conflictCount);
        Assert.Equal(totalSeats, heldIds.Count);

        // 5. Verify the class seat counts from the API:
        // totalSeats: 5, heldSeats: 5, confirmedSeats: 0, availableSeats: 0
        var classCheckResp = await _client.GetAsync($"/classes/{classId}");
        Assert.Equal(HttpStatusCode.OK, classCheckResp.StatusCode);
        var classCheck = await classCheckResp.Content.ReadFromJsonAsync<ClassSummaryDto>();
        Assert.NotNull(classCheck);
        Assert.Equal(totalSeats, classCheck.TotalSeats);
        Assert.Equal(totalSeats, classCheck.HeldSeats);
        Assert.Equal(0, classCheck.ConfirmedSeats);
        Assert.Equal(0, classCheck.AvailableSeats);
        Assert.True(classCheck.HeldSeats + classCheck.ConfirmedSeats <= classCheck.TotalSeats);
    }
}
