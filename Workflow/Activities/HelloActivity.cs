using Dapr.Workflow;

namespace WorkflowConsoleApp.Activities
{
    public class HelloActivity : WorkflowActivity<string, string>
    {
        readonly ILogger logger;

        public HelloActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<HelloActivity>();
        }

        public override Task<string> RunAsync(WorkflowActivityContext context, string input)
        {
            this.logger.LogInformation(input);

            return Task.FromResult<string>($"hello, {input}");
        }
    }
}