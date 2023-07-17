using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class FanOutWorkflow : Workflow<WorkflowPayload, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, WorkflowPayload payload)
        {
            string workflowId = context.InstanceId;

            Enumerable.Range(0,100).ToList().ForEach(async index => { 
            await context.CallActivityAsync(
                nameof(DelayActivity),
                new Notification($"{ workflowId} - Activity #{index}"));
            });      
        
            return true; 
        }
    }
}
