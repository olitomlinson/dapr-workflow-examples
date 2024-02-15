using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace WorkflowConsoleApp.Activities
{
    public record Notification(string Message);

    public class NotifyActivity : WorkflowActivity<Notification, object>
    {
        readonly ILogger logger;

        public NotifyActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<NotifyActivity>();
        }

        public override Task<object> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            this.logger.LogInformation(notification.Message);

            return Task.FromResult<object>(null);
        }
    }

    public class HelloActivity : WorkflowActivity<string, string>
    {
        readonly ILogger logger;

        public HelloActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<NotifyActivity>();
        }

        public override Task<string> RunAsync(WorkflowActivityContext context, string input)
        {
            this.logger.LogInformation(input);

            return Task.FromResult<string>($"hello, {input}");
        }
    }
}