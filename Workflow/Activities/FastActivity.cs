using Dapr.Workflow;

namespace WorkflowConsoleApp.Activities
{
    public record Notification(string Message, Guid[]? Data = default);

    public class FastActivity : WorkflowActivity<Notification, Guid[]?>
    {
        readonly ILogger logger;

        public FastActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<FastActivity>();
        }

        public override async Task<Guid[]> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            this.logger.LogInformation(notification.Message);

            return Enumerable.Range(0, 3000).Select(_ => Guid.NewGuid()).ToArray();
        }
    }
}