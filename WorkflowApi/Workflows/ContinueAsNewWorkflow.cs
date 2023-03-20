using Dapr.Workflow;
using WorkflowConsoleApp.Activities;
using WorkflowConsoleApp.Models;

namespace WorkflowConsoleApp.Workflows
{
    public class ContinueAsNewWorkflow : Workflow<WorkflowPayload, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, WorkflowPayload payload)
        {
            string workflowId = context.InstanceId;

            await context.CallActivityAsync(
                nameof(NotifyActivity),
                new Notification($"{payload.Count} - Notificaiton Sent ayoooo {workflowId} for  {payload.RandomData} at ${payload.Count}"));

            if (payload.Count > 10)
                return true;

            await context.CreateTimer(TimeSpan.FromSeconds(3));

            var newWorkflowPayload = new WorkflowPayload(
                payload.RandomData,
                payload.Count + 1
            );

            context.ContinueAsNew(newWorkflowPayload);
            return false;
        }
    }
}
