using Dapr.Workflow;
using Dapr;
using Dapr.Client;
using WorkflowConsoleApp.Activities;
using WorkflowConsoleApp.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddDaprWorkflow(options =>
    {
        options.RegisterWorkflow<ContinueAsNewWorkflow>();
        options.RegisterWorkflow<MaxConcurrentActivityWorkflow>();
        options.RegisterActivity<NotifyActivity>();
        options.RegisterActivity<DelayActivity>();
    });

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/start", [Topic("mypubsub", "workflowTopic")] async ( DaprClient daprClient, DaprWorkflowClient workflowClient, StartWorklowRequest? o) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = o?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant());

    var result = await workflowClient.ScheduleNewWorkflowAsync(
        name: nameof(ContinueAsNewWorkflow),
        instanceId: workflowId,
        input: orderInfo);

    return new StartWorkflowResponse(){
        Id = result
    };   
}).Produces<StartWorkflowResponse>();

app.MapPost("/startdelay", [Topic("mypubsub", "workflowDelayTopic")] async ( DaprClient daprClient, DaprWorkflowClient workflowClient, StartWorklowRequest? o) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = o?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant());

    var result = await workflowClient.ScheduleNewWorkflowAsync(
        name: nameof(MaxConcurrentActivityWorkflow),
        instanceId: workflowId,
        input: orderInfo);

    return new StartWorkflowResponse(){
        Id = result
    };   
}).Produces<StartWorkflowResponse>();

app.Run();

public record WorkflowPayload(string RandomData, int Count = 0);

public class StartWorklowRequest
{
    public string Id { get; set; }
}

public class StartWorkflowResponse
{
    public string Id {get; set;}
}