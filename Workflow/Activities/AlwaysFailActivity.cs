using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace WorkflowConsoleApp.Activities
{
    public class AlwaysFailActivity : WorkflowActivity<Notification, object>
    {
        readonly ILogger logger;

        public AlwaysFailActivity(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<AlwaysFailActivity>();
        }

        public override Task<object> RunAsync(WorkflowActivityContext context, Notification notification)
        {
            throw new Exception($"throwing random failure : {notification.Message}");

            return Task.FromResult<object>(null);
        }
    }
}