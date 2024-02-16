using Dapr.Workflow;

namespace WorkflowConsoleApp.Activities
{
    public record Notification(string Message);

    public class FastActivity : WorkflowActivity<Notification, bool>
    {
        readonly ILogger logger;

        public FastActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<FastActivity>();
        }

        public override async Task<bool> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            await Task.Delay(1000);

            this.logger.LogInformation(notification.Message);

            return true;
        }
    }
}