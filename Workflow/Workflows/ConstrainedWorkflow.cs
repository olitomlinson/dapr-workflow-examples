using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class ConstrainedWorkflow : Workflow<bool, string>
    {
        public override async Task<string> RunAsync(WorkflowContext context, bool state)
        {
            context.SetCustomStatus("STARTED");

            // 1. let's tell the throttler that we wan't to be told when its our turn to proceeed
            var waitEvent = new WaitEvent() { InstanceId = context.InstanceId, ProceedEventName = "proceed" };
            // https://github.com/dapr/dapr/issues/8243 
            // context.SendEvent("throttle", "wait", waitEvent );
            await context.CallActivityAsync<bool>(nameof(RaiseWaitEventActivity), new Tuple<string, WaitEvent>("throttle", waitEvent));

            // 2. now we wait... 
            var startTime = context.CurrentUtcDateTime.ToUniversalTime();
            context.SetCustomStatus("WAITING");
            await context.WaitForExternalEventAsync<object>("proceed");
            var endTime = context.CurrentUtcDateTime.ToUniversalTime();

            // 3. Ok, we can proceed with the constrained / slow activity
            context.SetCustomStatus("PROCEED");
            await context.CallActivityAsync<object>(
                nameof(VerySlowActivity),
                new Notification($"{context.InstanceId} - {nameof(VerySlowActivity)} - scheduled={DateTime.UtcNow:HH:mm:ss}")
                );
            context.SetCustomStatus("DONE");

            // 4. Tell the throttler that we are done (allowing the throttler to allow other work to proceed)
            var signalEvent = new SignalEvent() { InstanceId = context.InstanceId };
            // https://github.com/dapr/dapr/issues/8243 
            // context.SendEvent("throttle", "signal", signalEvent);
            await context.CallActivityAsync<bool>(nameof(RaiseSignalEventActivity), new Tuple<string, SignalEvent>("throttle", signalEvent));
            context.SetCustomStatus("SIGNALLED");

            // 5. Echo back how long this workflow waited for due to throttling
            return $"workflow throttled for {(endTime - startTime).TotalMilliseconds}ms";
        }
    }
}
