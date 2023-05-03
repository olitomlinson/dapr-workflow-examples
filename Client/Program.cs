using Dapr.Workflow;
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/start", async (DaprClient daprClient) =>
{
    var response = await daprClient.InvokeMethodAsync<StartWorkflowResponse>("workflow", "start");
    app.Logger.LogInformation("start Id: {0}", response.Id);
    return response;
}).Produces<StartWorkflowResponse>();

app.MapPost("/startasync", async (DaprClient daprClient) =>
{
    var o = new StartWorkflowRequest(){
        Id = Guid.NewGuid().ToString()
    };
    
    await daprClient.PublishEventAsync<StartWorkflowRequest>("mypubsub", "workflowTopic", o);
    app.Logger.LogInformation("Start Async Id: {0}", o.Id);
    return new StartWorkflowResponse{
        Id = o.Id
    };
}).Produces<StartWorkflowResponse>();

app.Run();

public class StartWorkflowRequest
{
    public string Id {get; set;}
}

public class StartWorkflowResponse
{
    public string Id { get; set; }
}