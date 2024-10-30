using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class ThrottleWorkflow : Workflow<ThrottleState, bool>
    {
        public override async Task<bool> RunAsync(WorkflowContext context, ThrottleState state)
        {
            #region Convenience functions
            Action<LogLevel, string> log = (LogLevel, message) =>
            {
                if (LogLevel >= state.RuntimeConfig.logLevel)
                    state.PersistentLog.Add(message);
            };
            #endregion

            #region Optimisations & Deadlock handling

            // 1. Sometimes a downstream workflow will not send it's signal, the consequence of this happening is that
            // eventually with enough failures, the semaphore will become blocked and no new downstream workflows will 
            // be able to progress.

            // So, every 15s we can scan the 'activeWaits' and if it has exceeded its ttl (default 60s) then a virtual signal 
            // is injected to unblock the sempahore.

            var expiryWatermark = context.CurrentUtcDateTime;
            var expiryPurgeCount = 0;
            foreach (var activeWait in state.ActiveWaits
                .Where(x => x.Value.Expiry.HasValue)
                .Where(x => expiryWatermark > x.Value.Expiry))
            {
                expiryPurgeCount += 1;
                log(LogLevel.Debug, $"active wait for {activeWait.Key} has expired [ts-now: {expiryWatermark}, ts-expiry: {activeWait.Value.Expiry.Value}, delta: {activeWait.Value.Expiry.Value.Subtract(expiryWatermark).TotalSeconds} seconds]");
                state.PendingSignals.Enqueue(new SignalEvent() { InstanceId = activeWait.Key });
            }
            if (expiryPurgeCount > 0)
                log(LogLevel.Info, $"purged {expiryPurgeCount} expired active wait(s)");

            // 2. Handle any signals first... (which will free-up capacity in the semaphore for step 3)
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

            #endregion

            // 3. Ensure that enough work is active (up to the Max Concurrency limit)
            while (state.PendingWaits.Any() &&
                (state.ActiveWaits.Count() < state.RuntimeConfig.MaxConcurrency))
            {
                WaitEvent waitEvent = state.PendingWaits.Dequeue();
                state.ActiveWaits.TryAdd(waitEvent.InstanceId, waitEvent);

                // https://github.com/dapr/dapr/issues/8243   
                // context.SendEvent(waitEvent.InstanceId, waitEvent.ProceedEventName, null);
                await context.CallActivityAsync<bool>(nameof(RaiseProceedEventActivity), new Tuple<string, string>(waitEvent.InstanceId, waitEvent.ProceedEventName));
            }


            // 4. Wait for a `wait` or `signal` from a Workflow (or `adjust` or `expiryScan` tick)
            var wait = context.WaitForExternalEventAsync<WaitEvent>("wait");
            var signal = context.WaitForExternalEventAsync<SignalEvent>("signal");
            var adjust = context.WaitForExternalEventAsync<RuntimeConfig>("adjust");
            var cts = new CancellationTokenSource();
            var expiryScan = context.CreateTimer(TimeSpan.FromSeconds(15), cts.Token);

            context.SetCustomStatus(new ThrottleSummary { Status = "WAITING", MaxWaits = state.RuntimeConfig.MaxConcurrency, ActiveWaits = state.ActiveWaits.Count(), PendingWaits = state.PendingWaits.Count() });
            var winner = await Task.WhenAny(wait, signal, adjust, expiryScan);
            if (winner == wait)
            {
                cts.Cancel();
                # region Expiry handling
                if (state.RuntimeConfig.DefaultTTLInSeconds > 0 && !wait.Result.Expiry.HasValue)
                    wait.Result.Expiry = context.CurrentUtcDateTime.AddSeconds(state.RuntimeConfig.DefaultTTLInSeconds);
                #endregion
                state.PendingWaits.Enqueue(wait.Result);
            }
            else if (winner == signal)
            {
                cts.Cancel();
                state.PendingSignals.Enqueue(signal.Result);
            }
            else if (winner == adjust)
            {
                cts.Cancel();
                state.RuntimeConfig = adjust.Result;
            }
            else if (winner == expiryScan)
            { // no-op 
            }
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

        public DateTime? Expiry { get; set; }
    }

    public class SignalEvent
    {
        public string InstanceId { get; set; }
    }

    public class ThrottleState
    {
        public RuntimeConfig RuntimeConfig = new RuntimeConfig();

        public Queue<WaitEvent> PendingWaits = new Queue<WaitEvent>();

        public Dictionary<string, WaitEvent> ActiveWaits = new Dictionary<string, WaitEvent>();

        public Queue<SignalEvent> PendingSignals = new Queue<SignalEvent>();

        public List<string> PersistentLog = new List<string>();
    }

    public class RuntimeConfig
    {
        public int MaxConcurrency { get; set; } = 10;

        public int DefaultTTLInSeconds { get; set; } = 60;

        public LogLevel logLevel { get; set; } = LogLevel.Info;
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
    }

    public class ThrottleSummary
    {
        public string Status { get; set; }
        public int MaxWaits { get; set; }
        public int ActiveWaits { get; set; }
        public int PendingWaits { get; set; }
    }
}
