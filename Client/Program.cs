
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

app.MapPost("/start", async (DaprClient daprClient, int? count, bool? async) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{Guid.NewGuid().ToString()[..8]}" };
        
        if (async.HasValue && async.Value == true)
            await daprClient.PublishEventAsync<StartWorkflowRequest>("mypubsub", "workflowTopic", request);
        else
            await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow", "start", request);
        
        app.Logger.LogInformation("start Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();

app.MapPost("/startdelay", async (DaprClient daprClient, int? count, bool? async) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{Guid.NewGuid().ToString()[..8]}" };
        
        if (async.HasValue && async.Value == true)
            await daprClient.PublishEventAsync<StartWorkflowRequest>("mypubsub", "workflowDelayTopic", request);
        else
            await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow", "startdelay", request);
        
        app.Logger.LogInformation("start Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();

app.Run();

public class StartWorkflowRequest
{
    public string Id {get; set;}
}

public class StartWorkflowResponse
{
    public int Index { get; set; }
    public string Id { get; set; }
}