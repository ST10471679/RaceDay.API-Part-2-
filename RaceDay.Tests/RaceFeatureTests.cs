using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;
using RaceDay.API.Services;
using Xunit;

namespace RaceDay.Tests;

public class RaceFeatureTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        context.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" });
        context.Users.AddRange(
            new User { UserId = 1, FirstName = "Olive", LastName = "Organiser", Email = "o@x.com", PasswordHash = "x", RoleId = 1 },
            new User { UserId = 2, FirstName = "Pat", LastName = "Runner", Email = "p@x.com", PasswordHash = "x", RoleId = 2 },
            new User { UserId = 3, FirstName = "Sam", LastName = "Sprinter", Email = "s@x.com", PasswordHash = "x", RoleId = 2 });
        context.Events.Add(new Event
        {
            EventId = 10,
            EventName = "City 10k",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Pretoria",
            Distance = "10km",
            EventType = "Running",
            OrganiserId = 1
        });
        context.EventCategories.AddRange(
            new EventCategory { EventCategoryId = 100, EventId = 10, Name = "10km", Distance = "10km", MaxParticipants = 1 },
            new EventCategory { EventCategoryId = 101, EventId = 10, Name = "5km", Distance = "5km", MaxParticipants = 0 });
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task EnrolUser_SavesChosenCategory_AsConfirmed()
    {
        var context = NewContext();
        var controller = new EnrolmentsController(context);

        var result = await controller.EnrolUser(eventId: 10, userId: 2, categoryId: 101);

        Assert.IsType<OkObjectResult>(result);
        var saved = await context.Enrolments.SingleAsync();
        Assert.Equal(101, saved.CategoryId);
        Assert.Equal("Confirmed", saved.Status);
    }

    [Fact]
    public async Task EnrolUser_PutsParticipantOnWaitingList_WhenCategoryIsFull()
    {
        var context = NewContext();
        var controller = new EnrolmentsController(context);

        await controller.EnrolUser(10, 2, 100);   // takes the only place
        await controller.EnrolUser(10, 3, 100);   // category now full

        var second = await context.Enrolments.SingleAsync(e => e.UserId == 3);
        Assert.Equal("Pending", second.Status);
    }

    [Fact]
    public async Task EnrolUser_RejectsDuplicateEnrolment()
    {
        var context = NewContext();
        var controller = new EnrolmentsController(context);

        await controller.EnrolUser(10, 2, 101);
        var again = await controller.EnrolUser(10, 2, 101);

        Assert.IsType<BadRequestObjectResult>(again);
    }

    [Fact]
    public async Task SaveEventResults_RanksByFinishTime_WhenPositionsAreBlank()
    {
        var context = NewContext();
        await new EnrolmentsController(context).EnrolUser(10, 2, 101);
        await new EnrolmentsController(context).EnrolUser(10, 3, 101);
        var controller = new ResultsController(context);

        var result = await controller.SaveEventResults(10, new SaveResultsDto
        {
            OrganiserId = 1,
            Results = new List<ResultEntryDto>
            {
                new() { UserId = 2, FinishTime = "00:50:10" },
                new() { UserId = 3, FinishTime = "00:45:30" }
            }
        });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, (await context.Results.SingleAsync(r => r.UserId == 3)).Position);
        Assert.Equal(2, (await context.Results.SingleAsync(r => r.UserId == 2)).Position);
    }

    [Fact]
    public async Task SaveEventResults_RejectsOtherOrganisers()
    {
        var context = NewContext();
        var controller = new ResultsController(context);

        var result = await controller.SaveEventResults(10, new SaveResultsDto { OrganiserId = 99 });

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, status.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_ReturnsPositionAndTotalFinishers()
    {
        var context = NewContext();
        await new EnrolmentsController(context).EnrolUser(10, 2, 101);
        await new EnrolmentsController(context).EnrolUser(10, 3, 101);
        var controller = new ResultsController(context);
        await controller.SaveEventResults(10, new SaveResultsDto
        {
            OrganiserId = 1,
            Results = new List<ResultEntryDto>
            {
                new() { UserId = 2, FinishTime = "00:50:10" },
                new() { UserId = 3, FinishTime = "00:45:30" }
            }
        });

        var response = await controller.GetMyResults(2);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var mine = Assert.Single(Assert.IsAssignableFrom<IEnumerable<MyResultDto>>(ok.Value));
        Assert.Equal(2, mine.Position);
        Assert.Equal(2, mine.TotalFinishers);
        Assert.Equal("5km", mine.CategoryName);
    }

    [Theory]
    [InlineData("00:45:30", 2730)]
    [InlineData("1:05:09", 3909)]
    [InlineData("45:30", 2730)]
    public void ResultTimeFormatter_ParsesCommonFormats(string input, int expectedSeconds)
    {
        Assert.True(ResultTimeFormatter.TryParse(input, out var time));
        Assert.Equal(expectedSeconds, (int)time.TotalSeconds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("00:00:00")]
    public void ResultTimeFormatter_RejectsBadInput(string input)
    {
        Assert.False(ResultTimeFormatter.TryParse(input, out _));
    }
}
