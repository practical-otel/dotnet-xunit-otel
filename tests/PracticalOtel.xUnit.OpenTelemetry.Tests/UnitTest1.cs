using System.Diagnostics;

namespace PracticalOtel.xUnit.OpenTelemetry;

public class NestedActivityTests
{
    private readonly ActivitySource _activitySource = new("UnitTests");

    [Fact]
    public async Task InternalActivity()
    {
        using var activity = _activitySource.StartActivity("My new Activity");
        await Task.Delay(1000);
    }
}
