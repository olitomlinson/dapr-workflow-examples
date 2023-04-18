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
    var response = await daprClient.InvokeMethodAsync<CreateWorkflowResponse>("workflow", "start");
    app.Logger.LogInformation("id: {0}", response.Id);
    return response.Id;
}).Produces<string>();

app.Run();

public class CreateWorkflowResponse
{
    public string Id { get; set; }
}