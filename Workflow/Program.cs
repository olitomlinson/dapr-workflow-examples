using Dapr.Workflow;
using Dapr;
using Dapr.Client;
using WorkflowConsoleApp.Activities;
using WorkflowConsoleApp.Workflows;
using workflow;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddDaprWorkflow(options =>
    {
        options.RegisterWorkflow<MonitorWorkflow>();
        options.RegisterWorkflow<FanOutWorkflow>();
        options.RegisterWorkflow<ExternalSystemWorkflow>();
        options.RegisterWorkflow<SagaWorkflow>();

        options.RegisterActivity<FastActivity>();
        options.RegisterActivity<SlowActivity>();
        options.RegisterActivity<AlwaysFailActivity>();
        options.RegisterActivity<NotifyCompensateActivity>();
    });

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//app.UseCloudEvents();
//app.MapSubscribeHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

TimingMetadata timings = new TimingMetadata();

app.MapGet("/dapr/subscribe", async () => {

    app.Logger.LogInformation("dapr/subscribe");

    return new object[] {
        new {
            pubsubname = "rabbit-pubsub",
            topic = "workflowTopic2",
            route = "/start",
            metadata = new { 
                routingKey = Environment.GetEnvironmentVariable("ROUTING_KEY"),
                queueName = "queue-" + Environment.GetEnvironmentVariable("ROUTING_KEY")
            }
        }
    };
});

app.MapPost("/health", async () => {

    app.Logger.LogInformation("Hello from Workflow!");

    return "Hello from Workflow!";

});

app.MapGet("/timings", () => {
    return timings;
    // var groups = timings.Splits.Durations.GroupBy(x =>
    // {
    //     var stamp = x.Timestamp;
    //     stamp = stamp.AddMinutes(-(stamp.Minute % 5));
    //     stamp = stamp.AddMilliseconds(-stamp.Millisecond - 1000 * stamp.Second);
    //     return stamp;
    // })
    // .Select(g => new { TimeStamp = g.Key, Value = g.Average(s => s.Duration) })
    // .ToList();

    // timings.SplitsGrouped = new List<TimingItem>();
    // foreach(var a in groups){
    //     timings.SplitsGrouped.Add(new TimingItem() {
    //         Timestamp = a.TimeStamp,
    //         Duration = a.Value
    //     })
        
    // }      
}).Produces<TimingMetadata>();

app.MapPost("/start", [Topic("rabbit-pubsub", "workflowTopic")] async ( DaprClient daprClient, DaprWorkflowClient workflowClient, CloudEvent2<StartWorklowRequest>? ce) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }
    app.Logger.LogInformation($"App : {Environment.GetEnvironmentVariable("ROUTING_KEY")}");
    app.Logger.LogInformation("ce fields : id {2}, type {0}, source {1}, specversion {2}, my-custom-property {3}", ce.Id, ce.Type, ce.Source, ce.Specversion, ce.MyCustomProperty);

    if (ce.Data.Sleep == 666)
    {
        throw new Exception("666");
    }

    if (ce.Data.Sleep > 0)
    {
        app.Logger.LogInformation("sleeping for {0} ...", ce.Data.Sleep);
        await Task.Delay(TimeSpan.FromSeconds(ce.Data.Sleep));
        app.Logger.LogInformation("Awake!");
    }

    if (!string.IsNullOrEmpty(ce.Data.AbortHint))
    {
        return new StartWorkflowResponse(){
            status = ce.Data.AbortHint
        };
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = ce.Data?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant(), 10, Enumerable.Range(0, 1).Select(_ => Guid.NewGuid()).ToArray());

    string result = string.Empty;
    Stopwatch stopwatch = new Stopwatch();
    stopwatch.Start();
    try
    {
        result = await workflowClient.ScheduleNewWorkflowAsync(
            name: nameof(MonitorWorkflow),
            instanceId: workflowId,
            input: orderInfo);
    }
    catch(Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse(){
            Id = workflowId + " error"
        };
    }
    stopwatch.Stop();
    //app.Logger.LogInformation($"duration : {stopwatch.ElapsedMilliseconds}");
    timings.Splits.Durations.Add(new TimingItem() { Timestamp = DateTime.UtcNow, Duration = (int)stopwatch.ElapsedMilliseconds});

    return new StartWorkflowResponse(){
        Id = result
    };   
}).Produces<StartWorkflowResponse>();

app.MapPost("/start-raise-event-workflow", [Topic("kafka-pubsub", "start-raise-event-workflow")] async ( DaprClient daprClient, DaprWorkflowClient workflowClient, CloudEvent2<StartWorklowRequest>? ce) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    if (ce.Data.Sleep == 666)
    {
        throw new Exception("666");
    }

    if (ce.Data.Sleep > 0)
    {
        app.Logger.LogInformation("sleeping for {0} ...", ce.Data.Sleep);
        await Task.Delay(TimeSpan.FromSeconds(ce.Data.Sleep));
        app.Logger.LogInformation("Awake!");
    }

    if (!string.IsNullOrEmpty(ce.Data.AbortHint))
    {
        return new StartWorkflowResponse(){
            status = ce.Data.AbortHint
        };
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = ce.Data?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new ExternalSystemWorkflowPayload(ce.Data?.FailOnTimeout ?? false);

    try
    {
        await workflowClient.ScheduleNewWorkflowAsync(nameof(ExternalSystemWorkflow), workflowId, orderInfo);
     
        await daprClient.RaiseWorkflowEventAsync(workflowId, "dapr", "wait-event", Guid.NewGuid().ToString());
        await daprClient.RaiseWorkflowEventAsync(workflowId, "dapr", "wait-event", Guid.NewGuid().ToString());
        await daprClient.RaiseWorkflowEventAsync(workflowId, "dapr", "wait-event", Guid.NewGuid().ToString());
        await daprClient.RaiseWorkflowEventAsync(workflowId, "dapr", "wait-event", Guid.NewGuid().ToString());
        await daprClient.RaiseWorkflowEventAsync(workflowId, "dapr", "wait-event", Guid.NewGuid().ToString());
    }
    catch(Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse(){
            Id = workflowId + " error"
        };
    }

    return new StartWorkflowResponse(){
        Id = workflowId
    };   
}).Produces<StartWorkflowResponse>();

app.MapPost("/start-raise-event-workflow-event", async ( DaprClient daprClient, DaprWorkflowClient workflowClient, RaiseEvent<string> o) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    app.Logger.LogInformation("raising event... : {InstanceId}, {EventName}, {EventData}", o.InstanceId, o.EventName, o.EventData);

    // This works as expected
    await daprClient.RaiseWorkflowEventAsync(o.InstanceId, "dapr", o.EventName, o.EventData);

    // This doesn't, eventPayload arrives in workflow as null 
    //await workflowClient.RaiseEventAsync(o.InstanceId, o.EventName, o.EventData);
});


app.MapGet("/status-batch", async ( DaprClient daprClient, DaprWorkflowClient workflowClient, string runId, int? count) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    var failed = 0;
    var complete = 0;
    var running = 0;
    var pending = 0;
    var terminated = 0;
    var suspended = 0;
    var unknown = 0;

    foreach(var i in Enumerable.Range(0, count.Value))
    {
        var instanceId = $"{i}-{runId}";
        var state = await workflowClient.GetWorkflowStateAsync(instanceId);

        if (state.RuntimeStatus == WorkflowRuntimeStatus.Completed)
            complete += 1;
        else if (state.RuntimeStatus == WorkflowRuntimeStatus.Running)
            running += 1;
        else if (state.RuntimeStatus == WorkflowRuntimeStatus.Failed)
            failed += 1;
        else if (state.RuntimeStatus == WorkflowRuntimeStatus.Pending)
            pending += 1;
        else if (state.RuntimeStatus == WorkflowRuntimeStatus.Terminated)
            terminated += 1;
        else if (state.RuntimeStatus == WorkflowRuntimeStatus.Suspended)
            suspended += 1;
        else if (state.RuntimeStatus == WorkflowRuntimeStatus.Unknown)
            unknown += 1;
    }
    
    var response = $"Completed : {complete}, Failed : {failed}, Running : {running}, Pending : {pending}, Terminated : {terminated}, Suspended : {suspended}, Unknown : {unknown} ";
    return response;

}).Produces<string>();


app.MapPost("/start-fanout-workflow", [Topic("kafka-pubsub", "FanoutWorkflowTopic")] async ( DaprClient daprClient, DaprWorkflowClient workflowClient, CloudEvent2<StartWorklowRequest>? ce) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    if (ce.Data.Sleep == 666)
    {
        throw new Exception("666");
    }

    if (ce.Data.Sleep > 0)
    {
        app.Logger.LogInformation("sleeping for {0} ...", ce.Data.Sleep);
        await Task.Delay(TimeSpan.FromSeconds(ce.Data.Sleep));
        app.Logger.LogInformation("Awake!");
    }

    if (!string.IsNullOrEmpty(ce.Data.AbortHint))
    {
        return new StartWorkflowResponse(){
            status = ce.Data.AbortHint
        };
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = ce.Data?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant(), 10);

    string result = string.Empty;
    try
    {
        result = await workflowClient.ScheduleNewWorkflowAsync(
            name: nameof(FanOutWorkflow),
            instanceId: workflowId,
            input: orderInfo);
    }
    catch(Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse(){
            Id = workflowId + " error"
        };
    }

    return new StartWorkflowResponse(){
        Id = result
    };   
}).Produces<StartWorkflowResponse>();


app.MapPost("/saga", [Topic("kafka-pubsub", "sagaTopic")] async ( DaprClient daprClient, DaprWorkflowClient workflowClient, CloudEvent2<StartWorklowRequest>? ce) => {
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    if (ce.Data.Sleep == 666)
    {
        throw new Exception("666");
    }

    if (ce.Data.Sleep > 0)
    {
        app.Logger.LogInformation("sleeping for {0} ...", ce.Data.Sleep);
        await Task.Delay(TimeSpan.FromSeconds(ce.Data.Sleep));
        app.Logger.LogInformation("Awake!");
    }

    if (!string.IsNullOrEmpty(ce.Data.AbortHint))
    {
        return new StartWorkflowResponse(){
            status = ce.Data.AbortHint
        };
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = ce.Data?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant());

    string result = string.Empty;
    try
    {
        result = await workflowClient.ScheduleNewWorkflowAsync(
            name: nameof(SagaWorkflow),
            instanceId: workflowId,
            input: orderInfo);
    }
    catch(Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse(){
            Id = workflowId + " error"
        };
    }

    return new StartWorkflowResponse(){
        Id = result
    };    
}).Produces<StartWorkflowResponse>();

app.Run();

public record WorkflowPayload(string RandomData, int Itterations = 1, Guid[]? Data = default);

public record ExternalSystemWorkflowPayload(bool failOnTimeout = false);

public class StartWorklowRequest
{
    public string Id { get; set; }
    public bool FailOnTimeout { get; set; }
    public int Sleep {get;set;}

    public string AbortHint { get; set; }
}

public class StartWorkflowResponse
{
    public string Id {get; set;}

    public string status { get; set; }

}

public class RaiseEvent<T>
{
    public string InstanceId {get; set;}
    public string EventName {get; set;}
    public T EventData { get; set; }
}