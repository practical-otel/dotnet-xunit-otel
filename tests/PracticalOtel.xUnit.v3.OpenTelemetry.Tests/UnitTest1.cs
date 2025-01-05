using System.Diagnostics;

namespace PracticalOtel.xUnit.OpenTelemetry.Tests;

public class NestedActivityTests
{
    private readonly ActivitySource _activitySource = new("UnitTests");

    [Fact]
    public async Task InternalActivity()
    {
        using var activity = _activitySource.StartActivity("My new Activity");
        await Task.Delay(1000, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void NestedActivity()
    {
        using var activity = _activitySource.StartActivity("Parent Activity");
        using var childActivity = _activitySource.StartActivity("Child Activity");
    }
}
