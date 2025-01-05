# An xUnit framework that turns every test into it's own trace

When writing complex code for production, it's important to know how we might be able to see what is going in production. That's where OpenTelemetry and telemetry signals come in.

This library makes every test a trace, and therefore allows you to see what the tracing would look like in a production scenario for a given usecase, therefore allowing you to better design your telemetry.

## V2 vs V3

There are 2 xunit libraries. When V3 was created, it was a massive redesign with lots of breaking changes (mainly in the running and extension space it seems) so it has a different package name `xunit.v3`.

To map to that, I've created a second library which also follows that.

In addition, there were improvements in v3 that allowed the setup to be a lot simpler.

If you're stuck on v2, you can follow the steps for the v2 guide at the end.

## Setup v3

To add a span around each test case, you can add the following package:

```shell
dotnet add package PracticalOtel.xUnit.v3.OpenTelemetry
```

Then add this anywhere in your test project.

```csharp
using PracticalOtel.xUnit.v3.OpenTelemetry;
[assembly: TestPipelineStartup(typeof(TracedPipelineStartup))]
```

Then you can configure where to send the telemetry using the standard OpenTelemetry Environment variables defined in the [OpenTelemetry Protocol Specification](https://opentelemetry.io/docs/specs/otel/protocol/exporter/#specifying-headers-via-environment-variables)

For aspire

```shell
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

For destinations that need authentication via headers

```shell
export OTEL_EXPORTER_OTLP_ENDPOINT=https://api.honeycomb.io
export OTEL_EXPORTER_OTLP_HEADERS=x-honeycomb-team=<api-key>
```

This will produce a single span for each test, with information about the test and it's status.

### Customizing the telemetry

The library exposes an extension point that will allow you to include other Activities from different frameworks too. This allows you to include spans from the ASP.NET framework, SQL calls, or other activities that libraries you use will create.

This is especially useful when it's coupled with using the WebApplicationFactory in ASP.NET, since you can now see each of the calls you make to the factory, but also the internal spans it creates.

To customize the `TracerProvider` you add a derived class from the `TracedPipelineStartup` class. Then you can add to the `TracerProviderBuilder`

```csharp
using PracticalOtel.xUnit.v3.OpenTelemetry.Tests;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(OtelTestFramework))]

namespace PracticalOtel.xUnit.v3.OpenTelemetry.Tests;

public class OtelTestFramework : TracedPipelineStartup {
    public OtelTestFramework() {
        traceProviderSetup = tpb => {
            tpb
                .AddAspNetCoreInstrumentation()
                .AddSqlClientInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(DiagnosticConfig.ActivitySource.Name);
        };
    }
}
```

## Setup v2

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
