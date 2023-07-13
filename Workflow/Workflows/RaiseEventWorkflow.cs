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
 
            // UNCOMMENT THIS TO MAKE THE TASK.WHENALL WORK SUCCESSFULLY
            //await context.CreateTimer(TimeSpan.FromSeconds(5));

            var ev1 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev2 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev3 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev4 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev5 = context.WaitForExternalEventAsync<string>("wait-event");
            var externalEvents = Task.WhenAll(ev1, ev2, ev3, ev4, ev5);
            
            var winner = await Task.WhenAny(externalEvents, timer);

            if (winner == externalEvents)
            {
                cts.Cancel();
                return $"external events all received : {ev1.Result}, {ev2.Result}, {ev3.Result}, {ev4.Result}, {ev5.Result}";
            }
            else if (winner == timer)
            {
                var taskStatus = $"all events : {externalEvents.Status}, ";
                taskStatus += $"event 1 : {ev1.Status}, ";
                taskStatus += $"event 2 : {ev2.Status}, ";
                taskStatus += $"event 3 : {ev3.Status}, ";
                taskStatus += $"event 4 : {ev4.Status}, ";
                taskStatus += $"event 5 : {ev5.Status}, ";

                if (payload.failOnTimeout)
                    throw new Exception($"Workflow Timed out after 30 seconds, {taskStatus}");
                else
                    return $"timed out after 30s, {taskStatus}";
            }
            
            return "error";
        }
    }
}
