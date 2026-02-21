using VikingJamGame.Models.GameEvents.Conditions;
using VikingJamGame.Models.GameEvents.Effects;
using VikingJamGame.Models.GameEvents.Runtime;
using VikingJamGame.Repositories.GameEvents;

namespace VikingJamGame.Tests.Models.GameEvents.Repository;

public sealed class InMemoryGameEventRepositoryTests
{
    [Fact]
    public void Constructor_ThrowsWhenEventIdsAreDuplicated()
    {
        var events = new[]
        {
            CreateEvent("dup"),
            CreateEvent("dup")
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new InMemoryGameEventRepository(events));

        Assert.Contains("Duplicate event id 'dup'", exception.Message);
    }

    [Fact]
    public void GetById_ReturnsExpectedEvent()
    {
        var repository = new InMemoryGameEventRepository([CreateEvent("event.a"), CreateEvent("event.b")]);

        GameEvent gameEvent = repository.GetById("event.b");

        Assert.Equal("event.b", gameEvent.Id);
    }

    [Fact]
    public void TryGetNextEvent_ReturnsTrueWhenNextEventExists()
    {
        GameEventOption startOption = CreateOption(nextEventId: "event.next");
        GameEvent start = CreateEvent("event.start", startOption);
        GameEvent next = CreateEvent("event.next");
        var repository = new InMemoryGameEventRepository([start, next]);

        var found = repository.TryGetNextEvent(startOption, out GameEvent resolvedNext);

        Assert.True(found);
        Assert.Equal("event.next", resolvedNext.Id);
    }

    [Fact]
    public void TryGetNextEvent_ReturnsFalseWhenOptionHasNoNextEventId()
    {
        GameEventOption optionWithoutLink = CreateOption(nextEventId: null);
        var repository = new InMemoryGameEventRepository([CreateEvent("event.start", optionWithoutLink)]);

        var found = repository.TryGetNextEvent(optionWithoutLink, out _);

        Assert.False(found);
    }

    private static GameEvent CreateEvent(string id, params GameEventOption[] options) =>
        new()
        {
            Id = id,
            Name = id,
            Description = "desc",
            Options = options
        };

    private static GameEventOption CreateOption(string? nextEventId) =>
        new()
        {
            DisplayText = "Go",
            ResolutionText = "Goes",
            Order = 1,
            DisplayCosts = false,
            VisibilityConditions = [],
            Costs = [],
            Effects = [],
            NextEventId = nextEventId
        };
}
