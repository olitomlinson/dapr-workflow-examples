using Dapr.Workflow;
using Dapr.Client;
using WorkflowConsoleApp.Activities;
using WorkflowConsoleApp.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddDaprWorkflow(options =>
    {
        options.RegisterWorkflow<ContinueAsNewWorkflow>();
        options.RegisterActivity<NotifyActivity>();
    });

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapPost("/start", async (DaprClient daprClient, WorkflowEngineClient workflowClient) =>
{
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant());

    // Start the workflow using the order ID as the workflow ID
    var result = await workflowClient.ScheduleNewWorkflowAsync(
        name: nameof(ContinueAsNewWorkflow),
        instanceId: workflowId,
        input: orderInfo);

    return new CreateWorkflowResponse(){
        Id = result
    };   
}).Produces<CreateWorkflowResponse>();

app.Run();

public class CreateWorkflowResponse
{
    public string Id { get; set; }
}

public record WorkflowPayload(string RandomData, int Count = 0);
