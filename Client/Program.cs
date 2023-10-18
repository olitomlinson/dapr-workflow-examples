
using System.Reflection.Metadata;
using System.Text.Json;
using Dapr;
using Dapr.Client;
using Microsoft.VisualBasic;
using Workflow;

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

app.MapPost("/start", async (DaprClient daprClient, string runId, int? count, bool? async) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{runId}" };
        
        var metadata = new Dictionary<string, string>();
        metadata.Add("cloudevent.id", "123");
        metadata.Add("cloudevent.type", "456");
        metadata.Add("cloudevent.source", "567");
        
        // var ce = new CloudEvent2<StartWorkflowRequest>(request) { 
        //     Id = "wf-" + Guid.NewGuid().ToString(),
        //     Source = new Uri("/cloudevents/spec/pull/123"), 
        //     Type = "my-type" };

        //var content = JsonSerializer.SerializeToUtf8Bytes(ce);

        if (async.HasValue && async.Value == true)
        {
            //await daprClient.PublishByteEventAsync("redis-pubsub", "workflowTopic", content.AsMemory(), "application/cloudevents+json", null, cts.Token);

            await daprClient.PublishEventAsync<StartWorkflowRequest>("kafka-pubsub", "workflowTopic", request, metadata, cts.Token);
        }
        else
            await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow-a", "start", request, cts.Token);
        
        app.Logger.LogInformation("start Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();

app.MapPost("/start-distribute", async (DaprClient daprClient, string runId, int? count, bool? async) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{runId}" };
        
        if (async.HasValue && async.Value == true)
            await daprClient.PublishEventAsync<StartWorkflowRequest>("kafka-pubsub", "workflowTopic", request, cts.Token);
        else
        {
            if (index % 2 == 0)
                await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow-a", "start", request, cts.Token);
            else
                await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow-b", "start", request, cts.Token);
        }
        
        app.Logger.LogInformation("start Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();


app.MapPost("/start-raise-event-workflow", async (DaprClient daprClient, string runId, int? count, bool? failOnTimeout) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    if (!failOnTimeout.HasValue)
        failOnTimeout = false;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ 
            Id = $"{index}-{runId}",
            FailOnTimeout = failOnTimeout.Value };
        
        await daprClient.PublishEventAsync<StartWorkflowRequest>("kafka-pubsub", "start-raise-event-workflow", request, cts.Token);

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
            EventData = Guid.NewGuid().ToString()
        };
        
        await daprClient.InvokeMethodAsync<RaiseEvent<string>>("workflow-a", "start-raise-event-workflow-event", payload, cts.Token);

        app.Logger.LogInformation("event raised: {0}", payload.InstanceId);
    });
});

app.MapPost("/start-fanout-workflow", async (DaprClient daprClient, string runId, int? count, bool? async) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{runId}" };

        if (async.HasValue && async.Value == true)
            await daprClient.PublishEventAsync<StartWorkflowRequest>("kafka-pubsub", "FanoutWorkflowTopic", request, cts.Token );
        else
            await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow-a", "start-fanout-workflow", request, cts.Token);
        
        app.Logger.LogInformation("start Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();


app.MapPost("/start-webhook-workflow", async (DaprClient daprClient, string runId, int? count, bool? async) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{runId}" };

        if (async.HasValue && async.Value == true)
            await daprClient.PublishEventAsync<StartWorkflowRequest>("kafka-pubsub", "WebhookWorkflowTopic", request, cts.Token );
        else
            await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow-a", "start-webhook-workflow", request, cts.Token);
        
        app.Logger.LogInformation("start Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();

app.MapPost("/saga", async (DaprClient daprClient, string runId, int? count, bool? async) =>
{
    if (!count.HasValue || count.Value < 1 )
        count = 1;

    var results = new List<StartWorkflowResponse>();

    var cts = new CancellationTokenSource();

    var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };

    await Parallel.ForEachAsync(Enumerable.Range(0, count.Value),options,async(index, token) => {
        var request = new StartWorkflowRequest{ Id = $"{index}-{runId}" };
        
        if (async.HasValue && async.Value == true)
            await daprClient.PublishEventAsync<StartWorkflowRequest>("kafka-pubsub", "sagaTopic", request, cts.Token);
        else
            await daprClient.InvokeMethodAsync<StartWorkflowRequest,StartWorkflowResponse>("workflow-a", "saga", request, cts.Token);
        
        app.Logger.LogInformation("start Id: {0}", request.Id);
        
        results.Add(new StartWorkflowResponse { Index = index, Id = request.Id });
    });
    return results;
}).Produces<List<StartWorkflowResponse>>();



app.Run();

public class StartWorkflowRequest
{
    public string Id {get; set;}
    public bool FailOnTimeout { get; set; }
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
