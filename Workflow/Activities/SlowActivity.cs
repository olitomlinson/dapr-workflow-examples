using Dapr.Workflow;

namespace WorkflowConsoleApp.Activities
{
    public class SlowActivity : WorkflowActivity<Notification,bool>
    {
        readonly ILogger logger;

        public SlowActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<SlowActivity>();
        }

        public override async Task<bool> RunAsync(WorkflowActivityContext context, Notification notification)
        {           
            await Task.Delay(3000);

            this.logger.LogInformation(notification.Message);

            return true;
        }
    }
}