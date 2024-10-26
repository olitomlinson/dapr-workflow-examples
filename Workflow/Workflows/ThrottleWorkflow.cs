using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class ThrottleWorkflow : Workflow<ThrottleState, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, ThrottleState state)
        {
            // create capacity by handling signals first...
            context.SetCustomStatus("CREATE CAPACITY");
            while (state.PendingSignals.Any())
            {
                var signal1 = state.PendingSignals.Dequeue();
                state.ActiveWaits.Remove(signal1.InstanceId);

                context.ContinueAsNew(state, true);
                return true;
            }

            // consume capacity by starting new work...
            context.SetCustomStatus("CONSUME CAPACITY");

            while (state.PendingWaits.Any() &&
                (state.ActiveWaits.Count() < state.MaxConcurrency))
            {
                //looks like there is some work to do...
                WaitEvent waitEvent = state.PendingWaits.Dequeue();
                state.ActiveWaits.Add(waitEvent.InstanceId, waitEvent);

                // this doesn't work due to a missing implementation in the wf runtime
                //context.SendEvent(waitEvent.InstanceId, waitEvent.ProceedEventName, null);

                // so we have to do it via an activity instead...
                await context.CallActivityAsync<bool>(nameof(RaiseProceedEventActivity), new Tuple<string, string>(waitEvent.InstanceId, waitEvent.ProceedEventName));
            }


            //nothing to do, other than wait for a completion event or a wait event
            var wait = context.WaitForExternalEventAsync<WaitEvent>("wait");
            var signal = context.WaitForExternalEventAsync<SignalEvent>("signal");
            var adjust = context.WaitForExternalEventAsync<string>("adjust");

            context.SetCustomStatus($"WAITING - Max: {state.MaxConcurrency}, ActiveWaits: {state.ActiveWaits.Count()}, PendingWaits: {state.PendingWaits.Count()} ");
            var winner = await Task.WhenAny(wait, signal, adjust);
            if (winner == wait)
                state.PendingWaits.Enqueue(wait.Result);
            else if (winner == signal)
                state.PendingSignals.Enqueue(signal.Result);
            else if (winner == adjust)
                state.MaxConcurrency = Convert.ToInt32(adjust.Result);
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
