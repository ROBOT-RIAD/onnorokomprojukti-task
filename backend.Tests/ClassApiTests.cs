using System.Net;
using System.Net.Http.Json;
using SchoolAdmission.Api.DTOs;
using Xunit;

namespace SchoolAdmission.Tests;

public class ClassApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClassApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateClass_WithValidData_Returns201AndCorrectPayload()
    {
        var request = new CreateClassRequest
        {
            Name = "Class 6 - Morning Shift",
            TotalSeats = 120
        };

        var response = await _client.PostAsJsonAsync("/classes", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ClassCreatedResponseDto>();
        Assert.NotNull(created);
        Assert.False(string.IsNullOrEmpty(created.Id));
        Assert.Equal("Class 6 - Morning Shift", created.Name);
        Assert.Equal(120, created.TotalSeats);
    }

    [Fact]
    public async Task CreateClass_WithInvalidData_Returns400BadRequest()
    {
        var invalidRequest = new CreateClassRequest
        {
            Name = "",
            TotalSeats = -5
        };

        var response = await _client.PostAsJsonAsync("/classes", invalidRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetClasses_ReturnsClassListWithSeatCalculations()
    {
        var createResponse = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 7 - Day Shift",
            TotalSeats = 50
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ClassCreatedResponseDto>();
        Assert.NotNull(created);

        var listResponse = await _client.GetAsync("/classes");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var classes = await listResponse.Content.ReadFromJsonAsync<List<ClassSummaryDto>>();
        Assert.NotNull(classes);
        var target = classes.FirstOrDefault(c => c.Id == created.Id);
        Assert.NotNull(target);
        Assert.Equal(50, target.TotalSeats);
        Assert.Equal(0, target.HeldSeats);
        Assert.Equal(0, target.ConfirmedSeats);
        Assert.Equal(50, target.AvailableSeats);
    }
}
