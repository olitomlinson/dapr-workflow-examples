using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class MaxConcurrentActivityWorkflow : Workflow<WorkflowPayload, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, WorkflowPayload payload)
        {
            string workflowId = context.InstanceId;

            try 
            {
                    Enumerable.Range(0,10).ToList().ForEach(async input => { 
                    await context.CallActivityAsync(
                        nameof(DelayActivity),
                        new Notification($"{input} - Notification Sent : {workflowId}"));

                });      
            }
            catch(Exception ex)
            {

            }     
            
            return true; 
        }
    }
}
