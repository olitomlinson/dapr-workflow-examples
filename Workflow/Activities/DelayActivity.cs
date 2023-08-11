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
            this.logger.LogInformation("waiting..." + notification.Message);
            
            await Task.Delay(3000);

            this.logger.LogInformation("finished. " + notification.Message);

            return Task.FromResult<object>(null);
        }
    }
}