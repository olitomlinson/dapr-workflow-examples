
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


app.MapPost("/start-raise-event-workflow", async (DaprClient daprClient, string runId, int? count) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{runId}" };
        
        await daprClient.PublishEventAsync<StartWorkflowRequest>("mypubsub", "start-raise-event-workflow", request);

        app.Logger.LogInformation("start-raise-event-workflow Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();

app.MapPost("/start-raise-event-workflow-event", async (DaprClient daprClient, string runId, int? count) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
   
        var payload = new RaiseEvent<string>(){
            InstanceId = $"{index}-{runId}",
            EventName = "wait-event",
            EventData = "OK"
        };
        
        await daprClient.InvokeMethodAsync<RaiseEvent<string>>("workflow", "start-raise-event-workflow-event", payload);

        app.Logger.LogInformation("event raised: {0}", payload.InstanceId);
    });
});

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

public class RaiseEvent<T>
{
    public string InstanceId {get; set;}
    public string EventName {get; set;}
    public T EventData { get; set; }
}
