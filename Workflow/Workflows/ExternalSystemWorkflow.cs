using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class ExternalSystemWorkflow : Workflow<ExternalSystemWorkflowPayload, string>
    {
        public override async Task<string> RunAsync(WorkflowContext context, ExternalSystemWorkflowPayload payload)
        {
            
            
            var cts = new CancellationTokenSource();

            Task timeout = context.CreateTimer(TimeSpan.FromSeconds(30), cts.Token);

            var ev1 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev2 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev3 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev4 = context.WaitForExternalEventAsync<string>("wait-event");
            var ev5 = context.WaitForExternalEventAsync<string>("wait-event");

            // WhenAll == AND(x, y, z, a, b)
            var externalEvents = Task.WhenAll(ev1, ev2, ev3, ev4, ev5);

            // WhenAny == XOR(x, y)
            var winner = await Task.WhenAny(externalEvents, timeout);

            if (winner == externalEvents)
            {
                cts.Cancel();
                return $"external events all received : {ev1.Result}, {ev2.Result}, {ev3.Result}, {ev4.Result}, {ev5.Result}";
            }
            else if (winner == timeout)
            {
                var taskStatus = $"all events : {externalEvents.Status}. ";
                taskStatus += $"[ev1 = {ev1.Status}] ";
                taskStatus += $"[ev2 = {ev2.Status}] ";
                taskStatus += $"[ev3 = {ev3.Status}] ";
                taskStatus += $"[ev4 = {ev4.Status}] ";
                taskStatus += $"[ev5 = {ev5.Status}] ";

                if (payload.failOnTimeout)
                    throw new Exception($"Workflow Timed out after 30 seconds, {taskStatus}");
                else
                    return $"timed out after 30s, {taskStatus}";
            }
            
            return "error";
        }
    }
}
