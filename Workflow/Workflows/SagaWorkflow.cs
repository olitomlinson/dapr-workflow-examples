// using Dapr.Workflow;
// using WorkflowConsoleApp.Activities;

// namespace WorkflowConsoleApp.Workflows
// {
//     public class SagaWorkflow : Workflow<WorkflowPayload, List<string>>
//     {
//         public override async Task<List<string>> RunAsync(WorkflowContext context, WorkflowPayload payload)
//         {
//             var log = new List<string>();
//             var saga = new Saga<WorkflowContext>(context, log);
//             var workflowId = context.InstanceId;

//             saga.OnCompensationComplete(async (log) =>
//             {
//                 /* Send "we're sorry, but.." email to customer... */
//                 log.Add("Done. Compensation complete!");
//             });

//             saga.OnCompensationError(async (log) =>
//             {
//                 /* Send emails to internal supporting teams */
//                 log.Add("Done. Compensation unsuccessful... Manual intervention required!");
//             });

//             try
//             {
//                 saga.AddCompensation((context) => context.CallActivityAsync(nameof(NotifyCompensateActivity), new Notification($"{workflowId}: some data 1")));
//                 await context.CallActivityAsync(nameof(NotifyActivity), new Notification($"{workflowId}: some data 1"));

//                 saga.AddCompensation((context) => context.CallActivityAsync(nameof(NotifyCompensateActivity), new Notification($"{workflowId}: some data 2")));
//                 await context.CallActivityAsync(nameof(NotifyActivity), new Notification($"{workflowId}: some data 2"));

//                 saga.AddCompensation((context) => context.CallActivityAsync(nameof(NotifyCompensateActivity), new Notification($"{workflowId}: some data 3")));
//                 await context.CallActivityAsync(nameof(AlwaysFailActivity), new Notification($"{workflowId}: some data 3"));
//             }
//             catch(Exception ex)
//             {
//                 await saga.CompensateAsync();
//             }

//             return log;
//         }
//     }
// }

using Dapr.Workflow;
using WorkflowConsoleApp.Activities;

namespace WorkflowConsoleApp.Workflows
{
    public class SagaWorkflow : Workflow<WorkflowPayload, List<string>>
    {
        public override async Task<List<string>> RunAsync(WorkflowContext context, WorkflowPayload payload)
        {
            var log = new List<string>();
            var saga = new Saga2(context, log);
            var workflowId = context.InstanceId;

            saga.OnCompensationComplete(async (log) =>
            {
                /* Send "we're sorry, but.." email to customer... */
                log.Add("Done. Compensation complete!");
            });

            saga.OnCompensationError(async (log) =>
            {
                /* Send emails to internal supporting teams */
                log.Add("Done. Compensation unsuccessful... Manual intervention required!");
            });


            var result1 = await saga.CallActivityAsync<string, string>(
                name: "HelloActivity",
                input: "New York", 
                compensation: (context,input,_) => context.CallActivityAsync(nameof(NotifyCompensateActivity), new Notification(input))
            );

            var result2 = await saga.CallActivityAsync<string, string>(
                name: "HelloActivity",
                input: "Seattle", 
                compensation: (context,input,_) => context.CallActivityAsync(nameof(NotifyCompensateActivity), new Notification(input))
            );

            var result3 = await saga.CallActivityAsync<string, Notification>(
                name: "AlwaysFailActivity",
                input: new Notification("London"), 
                compensation: (context,input,_) => context.CallActivityAsync(nameof(NotifyCompensateActivity), new Notification(input.Message))
            );

            return log;
        }
    }
}
