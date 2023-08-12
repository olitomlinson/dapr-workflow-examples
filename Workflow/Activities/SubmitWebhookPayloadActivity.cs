using Dapr.Workflow;
using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace WorkflowConsoleApp.Activities
{
    public class SubmitWebhookPayloadActivity : WorkflowActivity<WebhookPayload, bool>
    {
        readonly ILogger logger;
        readonly DaprClient _daprClient;

        public SubmitWebhookPayloadActivity(ILoggerFactory loggerFactory, DaprClient daprClient)
        {
            this.logger = loggerFactory.CreateLogger<SubmitWebhookPayloadActivity>();
            this._daprClient = daprClient;
        }

        public async override Task<bool> RunAsync(WorkflowActivityContext context, WebhookPayload webhookPayload)
        {
            this.logger.LogInformation(webhookPayload.Payload);

            await _daprClient.RaiseWorkflowEventAsync("monitor-1", "dapr", "payload", webhookPayload);

            return true;
        }
    }
}