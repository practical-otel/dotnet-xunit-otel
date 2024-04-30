using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit.Abstractions;

[assembly: TestFramework("PracticalOtel.xUnit.OpenTelemetry.Tests.OtelTestFramework", "PracticalOtel.xUnit.OpenTelemetry.Tests")]

namespace PracticalOtel.xUnit.OpenTelemetry.Tests;

public class OtelTestFramework : TracedTestFramework {
    public OtelTestFramework(IMessageSink messageSink) : base(messageSink) {
        traceProviderSetup = tpb => {
            tpb
                .ConfigureResource(resource => resource.AddService("Unit-Tests"))
                .AddSource("UnitTests")
                .AddConsoleExporter()
                .AddOtlpExporter();
        };
    }
}
