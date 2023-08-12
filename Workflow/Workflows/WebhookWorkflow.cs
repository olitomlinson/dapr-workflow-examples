using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class WebhookWorkflow : Workflow<string, WebhookResponse>
    {
        public override async Task<WebhookResponse> RunAsync(WorkflowContext context, string webhookPayload)
        {
            var payload = new WebhookPayload(context.InstanceId, webhookPayload);

            var success = await context.CallActivityAsync<bool>(nameof(SubmitWebhookPayloadActivity), payload);
            context.SetCustomStatus($"submitted = {success}");

            var response = await context.WaitForExternalEventAsync<WebhookResponse>("response");
            return response;
        }
    }
}
