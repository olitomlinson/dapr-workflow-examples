using Dapr.Workflow;
using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace WorkflowConsoleApp.Activities
{
    public class SubmitWebhookResponseActivity : WorkflowActivity<WebhookResponse, bool>
    {
        readonly ILogger logger;
        readonly DaprClient _daprClient;

        public SubmitWebhookResponseActivity(ILoggerFactory loggerFactory, DaprClient daprClient)
        {
            this.logger = loggerFactory.CreateLogger<SubmitWebhookResponseActivity>();
            this._daprClient = daprClient;
        }

        public async override Task<bool> RunAsync(WorkflowActivityContext context, WebhookResponse webhookResponse)
        {
            this.logger.LogInformation(webhookResponse.InstanceId);

            await _daprClient.RaiseWorkflowEventAsync(webhookResponse.InstanceId, "dapr", "response", webhookResponse);

            return true;
        }
    }
}