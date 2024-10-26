using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class ConstrainedWorkflow : Workflow<bool, string>
    {
        public override async Task<string> RunAsync(WorkflowContext context, bool state)
        {
            context.SetCustomStatus("STARTED");
            var waitEvent = new WaitEvent() { InstanceId = context.InstanceId, ProceedEventName = "proceed" };
            //context.SendEvent("throttle", "wait", waitEvent );
            await context.CallActivityAsync<bool>(nameof(RaiseWaitEventActivity), new Tuple<string, WaitEvent>("throttle", waitEvent));
            var startTime = context.CurrentUtcDateTime.ToUniversalTime();

            context.SetCustomStatus("WAITING");
            await context.WaitForExternalEventAsync<object>("proceed");

            var endTime = context.CurrentUtcDateTime.ToUniversalTime();
            context.SetCustomStatus("PROCEED");
            await context.CallActivityAsync<object>(
                nameof(VerySlowActivity),
                new Notification($"{context.InstanceId} - {nameof(VerySlowActivity)} - scheduled={DateTime.UtcNow:HH:mm:ss}")
                );
            context.SetCustomStatus("DONE");

            //tell the throttler that we are done
            var signalEvent = new SignalEvent() { InstanceId = context.InstanceId };
            //context.SendEvent("throttle", "signal", signalEvent );
            await context.CallActivityAsync<bool>(nameof(RaiseSignalEventActivity), new Tuple<string, SignalEvent>("throttle", signalEvent));
            context.SetCustomStatus("SIGNALLED");

            return $"workflow throttled for {(endTime - startTime).TotalMilliseconds}ms";
        }
    }
}
