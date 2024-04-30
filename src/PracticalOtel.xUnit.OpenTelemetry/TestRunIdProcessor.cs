using System.Diagnostics;
using OpenTelemetry;

namespace PracticalOtel.xUnit.OpenTelemetry;

internal class TestRunIdProcessor : BaseProcessor<Activity>
{
    private static readonly Guid _testRunId = Guid.NewGuid();

    public override void OnStart(Activity activity)
    {
        activity.SetTag("test.run_id", _testRunId.ToString());
    }
}
