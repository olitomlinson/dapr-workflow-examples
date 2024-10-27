using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class ThrottleWorkflow : Workflow<ThrottleState, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, ThrottleState state)
        {
            // 1. Handle any signals first... (which will create capacity for step 2)
            while (state.PendingSignals.Any())
            {
                var signal1 = state.PendingSignals.Dequeue();
                state.ActiveWaits.Remove(signal1.InstanceId);

                if (!state.PendingSignals.Any())
                {
                    // Don't technically have to do this, but it allows us to do a 
                    // GET on the workflow and see the state accurately
                    context.ContinueAsNew(state, true);
                    return true;
                }
            }

            // 2. Ensure that enough work is active (up to the Max Concurrency limit)
            while (state.PendingWaits.Any() &&
                (state.ActiveWaits.Count() < state.MaxConcurrency))
            {
                WaitEvent waitEvent = state.PendingWaits.Dequeue();
                state.ActiveWaits.Add(waitEvent.InstanceId, waitEvent);

                // https://github.com/dapr/dapr/issues/8243   
                // context.SendEvent(waitEvent.InstanceId, waitEvent.ProceedEventName, null);
                await context.CallActivityAsync<bool>(nameof(RaiseProceedEventActivity), new Tuple<string, string>(waitEvent.InstanceId, waitEvent.ProceedEventName));
            }


            // 3. Wait for a `wait` or `signal` from a Workflow
            var wait = context.WaitForExternalEventAsync<WaitEvent>("wait");
            var signal = context.WaitForExternalEventAsync<SignalEvent>("signal");
            var adjust = context.WaitForExternalEventAsync<int>("adjust-concurrency");

            context.SetCustomStatus($"WAITING - Max: {state.MaxConcurrency}, ActiveWaits: {state.ActiveWaits.Count()}, PendingWaits: {state.PendingWaits.Count()} ");
            var winner = await Task.WhenAny(wait, signal, adjust);
            if (winner == wait)
                state.PendingWaits.Enqueue(wait.Result);
            else if (winner == signal)
                state.PendingSignals.Enqueue(signal.Result);
            else if (winner == adjust)
                state.MaxConcurrency = adjust.Result;
            else
                throw new Exception("unknown event");

            context.ContinueAsNew(state, true);
            return true;
        }
    }

    public class WaitEvent
    {
        public string InstanceId { get; set; }

        public string ProceedEventName { get; set; }
    }

    public class SignalEvent
    {
        public string InstanceId { get; set; }
    }

    public class ThrottleState
    {
        public int MaxConcurrency { get; set; }

        public Queue<WaitEvent> PendingWaits = new Queue<WaitEvent>();

        public Dictionary<string, WaitEvent> ActiveWaits = new Dictionary<string, WaitEvent>();

        public Queue<SignalEvent> PendingSignals = new Queue<SignalEvent>();
    }
}
