using Microsoft.AspNetCore.Mvc;
using Dapr.Workflow;
using WorkflowConsoleApp.Workflows;
using WorkflowConsoleApp.Models;
using Dapr.Client;

namespace WorkflowApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly ILogger<WorkflowController> _logger;
    private readonly WorkflowEngineClient _workflowClient;
    private readonly DaprClient _daprClient;

    public WorkflowController(ILogger<WorkflowController> logger, WorkflowEngineClient workflowClient, DaprClient daprClient)
    {
        _logger = logger;
        _workflowClient = workflowClient;
        _daprClient = daprClient;
    }

    [HttpPost(Name = "CreateWorkflow")]
    public async Task<string> Post()
    {
        // Wait for the sidecar to become available
        while (!await _daprClient.CheckHealthAsync())
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            _logger.LogInformation("waiting...");
        }

        string randomData = Guid.NewGuid().ToString();
        string workflowId = $"{Guid.NewGuid().ToString()[..8]}";
        var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant());

        // Start the workflow using the order ID as the workflow ID
        var result = await _workflowClient.ScheduleNewWorkflowAsync(
            name: nameof(ContinueAsNewWorkflow),
            instanceId: workflowId,
            input: orderInfo);

        return result;   
    }
}
