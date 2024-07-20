using Dapr.Workflow;

namespace WorkflowConsoleApp.Activities
{
    public class VerySlowActivity : WorkflowActivity<Notification,bool>
    {
        readonly ILogger logger;

        public VerySlowActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<VerySlowActivity>();
        }

        public override async Task<bool> RunAsync(WorkflowActivityContext context, Notification notification)
        {           
            var message = notification.Message + $" activated={DateTime.UtcNow.ToString("HH:mm:ss")}";

            await Task.Delay(10000);

            this.logger.LogInformation(message);

            return true;
        }
    }
}