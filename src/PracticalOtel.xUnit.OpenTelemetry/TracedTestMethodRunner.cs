using System.Diagnostics;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PracticalOtel.xUnit.OpenTelemetry;

class TracedTestMethodRunner(ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, object[] constructorArguments) : XunitTestMethodRunner(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, constructorArguments) {
	private readonly IReflectionTypeInfo _class = @class;

    protected override async Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase) {
        var parameters = string.Empty;

        if (testCase.TestMethodArguments != null) {
            parameters = string.Join(", ", testCase.TestMethodArguments.Select(a => a?.ToString() ?? "null"));
        }

        var test = $"{_class.Type.Name}.{TestMethod.Method.Name}({parameters})";

        var activity = TracedTestFramework.activitySource.StartActivity(test, ActivityKind.Server, null, tags: new ActivityTagsCollection {
            { "test.class.name", _class.Type.Name },
            { "test.class.namespace", _class.Type.Namespace},
            { "test.class.method", TestMethod.Method.Name },
            { "test.parameters", parameters },
            { "test.framework", "xunit" },
        });

        try {
            var result = await base.RunTestCaseAsync(testCase);
            activity?.SetTag("test.failed_count", result.Failed);
            activity?.SetTag("test.skipped_count", result.Skipped);
            activity?.SetTag("test.total_count", result.Total);
            activity?.SetTag("test.time", result.Time);

            if (result.Failed > 0)
                activity?.SetStatus(ActivityStatusCode.Error);

            return result;
        }
        catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally {
            activity?.Stop();
        }
    }
}

