using OpenTelemetry.Resources;
using Xunit.Sdk;

namespace PracticalOtel.xUnit.v3.OpenTelemetry;

internal static class TestRunResourceAttributes
{
    internal static readonly Guid TestSessionId = Guid.NewGuid();

    public static ResourceBuilder AddTestRun(this ResourceBuilder builder)
    {
        return builder.AddAttributes(new Dictionary<string, object>
        {
            ["test.session_id"] = TestSessionId.ToString(),
            ["test.framework.name"] = "xunit",
            ["test.framework.version"] = typeof(ITestFrameworkExecutionOptions).Assembly.GetName().Version?.ToString() ?? "unknown",
        });
    }
}
