using System.Diagnostics;
using OpenTelemetry.Trace;
using Xunit.Sdk;
using OpenTelemetry;
using System.Reflection;
using Xunit.v3;
using OpenTelemetry.Resources;
using System.Diagnostics.Tracing;
using Xunit;

namespace PracticalOtel.xUnit.v3.OpenTelemetry;

public class TracedPipelineStartup : ITestPipelineStartup
{
    internal static readonly ActivitySource activitySource = new("PracticalOtel.xUnit.OpenTelemetry");
    private TracerProvider? _tracerProvider;
    protected Action<TracerProviderBuilder>? traceProviderSetup;
    private xUnitEventSourceListener? _eventSourceListener;

    private void InitializeTracer()
    {
        var builder = Sdk.CreateTracerProviderBuilder();

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            builder.AddOtlpExporter();

        traceProviderSetup?.Invoke(builder);

        _tracerProvider = builder
            .ConfigureResource(resource => resource
                .AddService(Assembly.GetExecutingAssembly().GetName().Name!, 
                    serviceInstanceId: TestRunResourceAttributes.TestSessionId.ToString())
                .AddTestRun())
            .AddSource(activitySource.Name)
            .Build();
    }

    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        _eventSourceListener = new xUnitEventSourceListener();
        InitializeTracer();
        return default;
    }

    public ValueTask StopAsync()
    {
        _tracerProvider?.Dispose();
        _eventSourceListener?.Dispose();
        return default;
    }
}

internal class xUnitEventSourceListener : EventListener
{
    private const int TestStartedId = 1;
    private const int TestStoppedId = 2;
    private AsyncLocal<Activity?> currentActivity = new();
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "xUnit.TestEventSource" &&
            TracedPipelineStartup.activitySource.HasListeners())
        {
            EnableEvents(eventSource, EventLevel.Informational);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventId == TestStartedId)
        {
            var test = eventData.Payload![0] as string;
            currentActivity.Value = TracedPipelineStartup.activitySource.StartActivity(test, ActivityKind.Server, null, tags: new ActivityTagsCollection
            {
            { "xunit.class.name", TestContext.Current?.TestClass?.TestClassSimpleName },
            { "xunit.class.namespace", TestContext.Current?.TestClass?.TestClassNamespace},
            { "xunit.class.method", TestContext.Current?.TestMethod?.MethodName },
            { "test.framework", "xunit" },
            });
        }
        else if (eventData.EventId == TestStoppedId)
        {
            currentActivity.Value!.SetTag("test.status", TestContext.Current?.TestStatus);
            currentActivity.Value!.Stop();
        }
    }
}

