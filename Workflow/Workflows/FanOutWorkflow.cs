using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class FanOutWorkflow : Workflow<WorkflowPayload, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, WorkflowPayload payload)
        {
            string workflowId = context.InstanceId;

            await context.CallActivityAsync<bool>(nameof(SlowActivity), new Notification($"{workflowId} - Luma"));

            await context.CallActivityAsync<bool>(nameof(FastActivity), new Notification($"{workflowId} - Platform"));
            
            var fanOut = new List<Task>();
    
            for(int i = 0; i < payload.Itterations; i++)
            {
                fanOut.Add(context.CallActivityAsync<bool>(nameof(SlowActivity), new Notification($"{workflowId} - Activity #{i}")));
            };     

            // WhenAll == AND(x, y, z, a, b)
            await Task.WhenAll(fanOut); 
        
            return true; 
        }
    }
}
