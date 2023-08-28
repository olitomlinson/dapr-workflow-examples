using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace WorkflowConsoleApp.Activities
{
    public class NotifyCompensateActivity : WorkflowActivity<Notification, object>
    {
        readonly ILogger logger;

        public NotifyCompensateActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<NotifyCompensateActivity>();
        }

        public override Task<object> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            this.logger.LogInformation($"Compensation applied: {notification.Message}");

            return Task.FromResult<object>(null);
        }
    }
}