using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class MonitorWorkflow : Workflow<WorkflowPayload, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, WorkflowPayload payload)
        {
            string workflowId = context.InstanceId;

            await context.CallActivityAsync<bool>(
                nameof(FastActivity),
                new Notification($"{workflowId} - Activity #{payload.Itterations}"));

            await context.CreateTimer(TimeSpan.FromSeconds(3));

            var newWorkflowPayload = new WorkflowPayload(
                payload.RandomData,
                payload.Itterations - 1
            );

            if (payload.Itterations == 0)
                 return true;

            context.ContinueAsNew(newWorkflowPayload);
            return false;
        }
    }
}
