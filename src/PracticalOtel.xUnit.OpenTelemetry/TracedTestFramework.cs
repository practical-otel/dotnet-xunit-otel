using System.Diagnostics;
using OpenTelemetry.Trace;
using Xunit.Abstractions;
using Xunit.Sdk;
using OpenTelemetry;
using System.Reflection;

namespace PracticalOtel.xUnit.OpenTelemetry;

public class TracedTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{

    internal static readonly ActivitySource activitySource = new("PracticalOtel.xUnit.OpenTelemetry");
    private TracerProvider? _tracerProvider;
    protected Action<TracerProviderBuilder>? traceProviderSetup;

    private void InitializeTracer()
    {
        var builder = Sdk.CreateTracerProviderBuilder();

        traceProviderSetup?.Invoke(builder);

        _tracerProvider = builder
            .AddSource(activitySource.Name)
            .AddProcessor(new TestRunIdProcessor())
            .Build();
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
    {
        InitializeTracer();
        return new TracedExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink, _tracerProvider);
    }

    private class TracedExecutor : XunitTestFrameworkExecutor
    {
        private readonly TracerProvider? _tracerProvider;

        public TracedExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink, TracerProvider? tracerProvider)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
            _tracerProvider = tracerProvider;
        }

        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using var assemblyRunner = new TracedAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions, _tracerProvider);
            await assemblyRunner.RunAsync();
        }
    }

    private class TracedAssemblyRunner : XunitTestAssemblyRunner
    {
        private readonly TracerProvider? _tracerProvider;

        public TracedAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, TracerProvider? tracerProvider)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
            _tracerProvider = tracerProvider;
        }

        protected override async Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            var runner = new TracedTestCollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource);
            var summary = await runner.RunAsync();
            _tracerProvider?.ForceFlush();
            return summary;
        }
    }

    private class TracedTestCollectionRunner(
        ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer,
        ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        : XunitTestCollectionRunner(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
    {
        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
            => new TracedTestClassRunner(
                testClass, @class, testCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer,
                new ExceptionAggregator(Aggregator), CancellationTokenSource, CollectionFixtureMappings)
                .RunAsync();
    }

    private class TracedTestClassRunner(
        ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer,
        ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, IDictionary<Type, object> collectionFixtureMappings)
        : XunitTestClassRunner(testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
    {
        protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
            => new TracedTestMethodRunner(testMethod, this.Class, method, testCases, this.DiagnosticMessageSink, this.MessageBus, new ExceptionAggregator(this.Aggregator), this.CancellationTokenSource, constructorArguments)
                .RunAsync();
    }

}
