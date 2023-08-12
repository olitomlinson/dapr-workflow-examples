using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class BatchMonitorWorkflow : Workflow<BatchMonitorState, BatchMonitorState>
    {
        public override async Task<BatchMonitorState> RunAsync(WorkflowContext context, BatchMonitorState state)
        {
            var newstate = state with { Payloads = state.Payloads ?? new Dictionary<string,string>() };

            var webhookPayload = await context.WaitForExternalEventAsync<WebhookPayload>("payload");

            context.SetCustomStatus(newstate.Payloads.Count());

            var payloads = newstate.Payloads;
            payloads.Add(webhookPayload.InstanceId, webhookPayload.Payload);

            var stateAppended = newstate with { Payloads = payloads };

            // if the batch is not full, wait for more events
            if (stateAppended.Payloads.Count() < 6)
            {
                context.ContinueAsNew(stateAppended, preserveUnprocessedEvents: true);
                return null;
            }

            //TODO : we now have a full batch of work to send


            // notify the WebhookWorkflows of the results
            string handled = string.Empty;
            foreach (var webhook in stateAppended.Payloads){
                var success = await context.CallActivityAsync<bool>(nameof(SubmitWebhookResponseActivity), new WebhookResponse(webhook.Key, "200"));
                handled += $"{webhook.Key}: {success}, ";
                context.SetCustomStatus(handled);
            }

            // exit successfully

            return stateAppended;
        }
    }
}
