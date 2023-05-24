using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace WorkflowConsoleApp.Activities
{
    public class DelayActivity : WorkflowActivity<Notification, object>
    {
        readonly ILogger logger;

        public DelayActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<NotifyActivity>();
        }

        public override async Task<object> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            await Task.Delay(3000);

            this.logger.LogInformation(notification.Message);

            return Task.FromResult<object>(null);
        }
    }
}