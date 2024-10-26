using Dapr.Workflow;

namespace WorkflowConsoleApp.Activities
{
    public class RaiseProceedEventActivity : WorkflowActivity<Tuple<string, string>, bool>
    {
        readonly ILogger logger;
        readonly DaprWorkflowClient _daprWorkflowClient;

        public RaiseProceedEventActivity(ILoggerFactory loggerFactory, DaprWorkflowClient daprWorkflowClient)
        {
            this.logger = loggerFactory.CreateLogger<RaiseProceedEventActivity>();
            this._daprWorkflowClient = daprWorkflowClient;
        }

        public override async Task<bool> RunAsync(WorkflowActivityContext context, Tuple<string, string> @event)
        {
            this.logger.LogInformation("raising event : " + @event.Item1 + " " + @event.Item2);

            await _daprWorkflowClient.RaiseEventAsync(@event.Item1, @event.Item2, null);

            return true;
        }
    }

    public class RaiseWaitEventActivity : WorkflowActivity<Tuple<string, Workflows.WaitEvent>, bool>
    {
        readonly ILogger logger;
        readonly DaprWorkflowClient _daprWorkflowClient;

        public RaiseWaitEventActivity(ILoggerFactory loggerFactory, DaprWorkflowClient daprWorkflowClient)
        {
            this.logger = loggerFactory.CreateLogger<RaiseWaitEventActivity>();
            this._daprWorkflowClient = daprWorkflowClient;
        }

        public override async Task<bool> RunAsync(WorkflowActivityContext context, Tuple<string, Workflows.WaitEvent> @event)
        {
            this.logger.LogInformation("raising wait event : " + @event.Item1);

            await _daprWorkflowClient.RaiseEventAsync(@event.Item1, "wait", @event.Item2);

            return true;
        }
    }

    public class RaiseSignalEventActivity : WorkflowActivity<Tuple<string, Workflows.SignalEvent>, bool>
    {
        readonly ILogger logger;
        readonly DaprWorkflowClient _daprWorkflowClient;

        public RaiseSignalEventActivity(ILoggerFactory loggerFactory, DaprWorkflowClient daprWorkflowClient)
        {
            this.logger = loggerFactory.CreateLogger<RaiseSignalEventActivity>();
            this._daprWorkflowClient = daprWorkflowClient;
        }

        public override async Task<bool> RunAsync(WorkflowActivityContext context, Tuple<string, Workflows.SignalEvent> @event)
        {
            this.logger.LogInformation("raising signal event : " + @event.Item1);

            await _daprWorkflowClient.RaiseEventAsync(@event.Item1, "signal", @event.Item2);

            return true;
        }
    }
}