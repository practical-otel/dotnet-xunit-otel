# An xUnit framework that turns every test into it's own trace

When writing complex code for production, it's important to know how we might be able to see what is going in production. That's where OpenTelemetry and telemetry signals come in.

This library makes every test a trace, and therefore allows you to see what the tracing would look like in a production scenario for a given usecase, therefore allowing you to better design your telemetry.

## Setup

You'll need to install the OpenTelemetry SDK, and the exporters that you want to use. The most common would be to use the OTLP exporter. For this example we'll use the OTLP exporter with default parameters.

```shell
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

Add a file to your test solution that looks like this

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit.Abstractions;

[assembly: TestFramework("PracticalOtel.xUnit.OpenTelemetry.Tests.OtelTestFramework", "PracticalOtel.xUnit.OpenTelemetry.Tests")]

namespace PracticalOtel.xUnit.OpenTelemetry.Tests;

public class OtelTestFramework : TracedTestFramework
{
    public OtelTestFramework(IMessageSink messageSink) : base(messageSink)
    {
        traceProviderSetup = tpb => {
            tpb
                .ConfigureResource(resource => resource.AddService("Unit-Tests"))
                .AddSource("UnitTests")
                .AddOtlpExporter();
        };
    }
}
```

This will also monitor the ActivitySource named `UnitTests`, you should replace this with the ActivitySource you're using in your code.

### Note on Environment Variables

Unfortunately, your environment variables from your shell are no passed to tests, so you can't control the exporter using them.

## Viewing the data

By default, this will output the span data via gRPC to localhost:4317. You can use the Aspire Dashboard to view that.

```shell
docker run -it -p 18888:18888 -p 4317:18889 -d -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true --name aspire-dashboard mcr.microsoft.com/dotnet/nightly/aspire-dashboard:8.0.0-preview.5
```

Then navigate to http://localhost:18888
