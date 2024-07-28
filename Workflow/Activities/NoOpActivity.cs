using Dapr.Workflow;

namespace WorkflowConsoleApp.Activities
{
    public class NoOpActivity : WorkflowActivity<Notification, bool>
    {
        readonly ILogger logger;

        public NoOpActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<NoOpActivity>();
        }

        public override async Task<bool> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            this.logger.LogInformation(notification.Message);

            return true;
        }
    }
}