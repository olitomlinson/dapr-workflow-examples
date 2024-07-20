using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class FanOutWorkflow : Workflow<WorkflowPayload, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, WorkflowPayload payload)
        {
            string workflowId = context.InstanceId;

            var fanOut = new List<Task>();
    
            for(int i = 0; i < payload.Itterations; i++)
            {
                fanOut.Add(context.CallActivityAsync<bool>(nameof(SlowActivity), new Notification($"{workflowId} - Slow Activity #{i} - scheduled={DateTime.UtcNow.ToString("HH:mm:ss")}")));
            };     

            for(int i = 0; i < payload.Itterations; i++)
            {
                fanOut.Add(context.CallActivityAsync<bool>(nameof(VerySlowActivity), new Notification($"{workflowId} - Very Slow Activity #{i} - scheduled={DateTime.UtcNow.ToString("HH:mm:ss")}")));
            };  

            await Task.WhenAll(fanOut); 
        
            return true; 
        }
    }
}
