using PracticalOtel.xUnit.v3.OpenTelemetry.Tests;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(OtelTestFramework))]

namespace PracticalOtel.xUnit.v3.OpenTelemetry.Tests;

public class OtelTestFramework : TracedPipelineStartup {
    public OtelTestFramework() {
        traceProviderSetup = tpb => {
            tpb
                .AddSource("UnitTests");
        };
    }
}
