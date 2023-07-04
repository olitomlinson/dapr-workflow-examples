using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class RaiseEventWorkflow : Workflow<RaiseEventWorkflowPayload, string>
    {
        public override async Task<string> RunAsync(WorkflowContext context, RaiseEventWorkflowPayload payload)
        {
            var cts = new CancellationTokenSource();

            Task timer = context.CreateTimer(TimeSpan.FromSeconds(30), cts.Token);

            var externalEvent = context.WaitForExternalEventAsync<string>("wait-event");

            var winner = await Task.WhenAny(timer, externalEvent);

            if (winner == externalEvent)
            {
                cts.Cancel();
                return $"external event : {externalEvent.Result}";
            }
            else if (winner == timer)
            {
                if (payload.failOnTimeout)
                    throw new Exception("Workflow Timed out after 30 seconds");
                else
                    return "timed out after 30s";
            }
            
            return "error";
        }
    }
}
