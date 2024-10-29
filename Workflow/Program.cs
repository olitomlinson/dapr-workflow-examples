using Dapr.Workflow;
using Dapr;
using Dapr.Client;
using WorkflowConsoleApp.Activities;
using WorkflowConsoleApp.Workflows;
using workflow;
using System.Text.Json;
using WorkflowConsoleApp;


var builder = WebApplication.CreateBuilder(args);
bool registerWorkflows = Convert.ToBoolean(Environment.GetEnvironmentVariable("REGISTER_WORKFLOWS"));
bool registerActivities = Convert.ToBoolean(Environment.GetEnvironmentVariable("REGISTER_ACTIVITIES"));

builder.Services.AddHttpClient();
builder.Services.AddDaprClient();
builder.Services.AddDaprWorkflow(options =>
    {
        if (registerWorkflows)
        {
            options.RegisterWorkflow<MonitorWorkflow>();
            options.RegisterWorkflow<FanOutWorkflow>();
            options.RegisterWorkflow<ExternalSystemWorkflow>();
            options.RegisterWorkflow<SagaWorkflow>();
            options.RegisterWorkflow<ThrottleWorkflow>();
            options.RegisterWorkflow<ConstrainedWorkflow>();
        }

        if (registerActivities)
        {
            options.RegisterActivity<FastActivity>();
            options.RegisterActivity<SlowActivity>();
            options.RegisterActivity<VerySlowActivity>();
            options.RegisterActivity<AlwaysFailActivity>();
            options.RegisterActivity<NotifyCompensateActivity>();
            options.RegisterActivity<NoOpActivity>();
            options.RegisterActivity<RaiseProceedEventActivity>();
            options.RegisterActivity<RaiseSignalEventActivity>();
            options.RegisterActivity<RaiseWaitEventActivity>();
        }
    });

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<DaprJobsService>(
    client =>
    {
        client.BaseAddress = new Uri($"http://localhost:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")}/v1.0-alpha1/jobs/");
    });

var app = builder.Build();

//app.UseCloudEvents();
app.MapSubscribeHandler();

app.Logger.LogInformation("REGISTER_WORKFLOWS: " + registerWorkflows);
app.Logger.LogInformation("REGISTER_ACTIVITIES: " + registerActivities);
app.Logger.LogInformation("DAPR_HTTP_PORT: " + Environment.GetEnvironmentVariable("DAPR_HTTP_PORT"));
app.Logger.LogInformation("DAPR_GRPC_PORT: " + Environment.GetEnvironmentVariable("DAPR_GRPC_PORT"));
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.Use(async (httpContext, next) =>
// {
//     try
//     {
//         httpContext.Request.EnableBuffering();
//         string requestBody = await new StreamReader(httpContext.Request.Body, System.Text.Encoding.UTF8).ReadToEndAsync();
//         httpContext.Request.Body.Position = 0;

//         Console.WriteLine($"Request path: {httpContext.Request.Path}");
//         Console.WriteLine($"Request Content-Type: {httpContext.Request.ContentType}");
//         Console.WriteLine($"Request body: {requestBody}");
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Exception reading request: {ex.Message}");
//     }

//     Stream originalBody = httpContext.Response.Body;
//     try
//     {
//         using var memStream = new MemoryStream();
//         httpContext.Response.Body = memStream;

//         // call to the following middleware 
//         // response should be produced by one of the following middlewares
//         await next(httpContext);

//         memStream.Position = 0;
//         string responseBody = new StreamReader(memStream).ReadToEnd();

//         memStream.Position = 0;
//         await memStream.CopyToAsync(originalBody);
//         Console.WriteLine(responseBody);
//     }
//     finally
//     {
//         httpContext.Response.Body = originalBody;
//     }
// });


app.MapGet("/health", async (DaprJobsService jobsService) =>
{
    await jobsService.EnsureThrottleJobIsRunning();
    app.Logger.LogInformation($"Health is good");
});

app.MapPost("/job/ensurethrottle", async (DaprWorkflowClient workflowClient) =>
{
    var state = await workflowClient.GetWorkflowStateAsync("throttle", false);
    if (state.Exists && state.IsWorkflowRunning)
        return;

    app.Logger.LogWarning($"throttle workflow does not exist, attempting to schedule it");
    await workflowClient.ScheduleNewWorkflowAsync(nameof(ThrottleWorkflow), "throttle", new ThrottleState { MaxConcurrency = 5 });
});

// app.MapGet("/health/toggle", async () =>
// {
//     appHealth = !appHealth;
// });

// app.MapPost("/start", [Topic("kafka-pubsub", "workflowTopic")] async ( [FromHeader(Name = "__partition")] string partition, [FromHeader(Name = "my-custom-property")] string customHeader, DaprClient daprClient, DaprWorkflowClient workflowClient, CustomCloudEvent<StartWorklowRequest>? ce) => {
app.MapPost("/monitor-workflow", [Topic("kafka-pubsub", "monitor-workflow")] async (DaprClient daprClient, DaprWorkflowClient workflowClient, CustomCloudEvent<StartWorklowRequest>? ce) =>
{
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    // app.Logger.LogInformation("ce.id {0}, ce.type {1}, ce.source {2}, ce.specversion {3}, ce.my-custom-property {4}, kafka-partition {5}, customHeader {6}", ce.Id, ce.Type, ce.Source, ce.Specversion, ce.MyCustomProperty, partition, customHeader);

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
        return new StartWorkflowResponse()
        {
            status = ce.Data.AbortHint
        };
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = ce.Data?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new WorkflowPayload(randomData.ToLowerInvariant(), 10, Enumerable.Range(0, 1).Select(_ => Guid.NewGuid()).ToArray());

    string result = string.Empty;

    try
    {
        result = await workflowClient.ScheduleNewWorkflowAsync(
            name: nameof(MonitorWorkflow),
            instanceId: workflowId,
            input: orderInfo);
    }
    catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse()
        {
            Id = workflowId + " error"
        };
    }

    return new StartWorkflowResponse()
    {
        Id = result
    };
}).Produces<StartWorkflowResponse>();

app.MapPost("/start-raise-event-workflow", [Topic("kafka-pubsub", "start-raise-event-workflow")] async (DaprClient daprClient, DaprWorkflowClient workflowClient, CustomCloudEvent<StartWorklowRequest>? ce) =>
{
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
        return new StartWorkflowResponse()
        {
            status = ce.Data.AbortHint
        };
    }

    string randomData = Guid.NewGuid().ToString();
    string workflowId = ce.Data?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";
    var orderInfo = new ExternalSystemWorkflowPayload(ce.Data?.FailOnTimeout ?? false);

    try
    {
        await workflowClient.ScheduleNewWorkflowAsync(nameof(ExternalSystemWorkflow), workflowId, orderInfo);

        var cts = new CancellationTokenSource();
        var options = new ParallelOptions() { MaxDegreeOfParallelism = 50, CancellationToken = cts.Token };
        await Parallel.ForEachAsync(Enumerable.Range(0, 1000), options, async (index, token) =>
        {
            await workflowClient.RaiseEventAsync(workflowId, "event-name", $"{index}-{Guid.NewGuid()}");
        });
    }
    catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse()
        {
            Id = workflowId + " error"
        };
    }

    return new StartWorkflowResponse()
    {
        Id = workflowId
    };
}).Produces<StartWorkflowResponse>();


app.MapGet("/status-batch", async (DaprClient daprClient, DaprWorkflowClient workflowClient, string runId, int? count) =>
{
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
    Dictionary<string, WorkflowState> Running = new Dictionary<string, WorkflowState>();

    foreach (var i in Enumerable.Range(0, count.Value))
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

        if (state.RuntimeStatus == WorkflowRuntimeStatus.Running)
            Running.Add(instanceId, state);
    }

    var response = $"Completed : {complete}, Failed : {failed}, Running : {running}, Pending : {pending}, Terminated : {terminated}, Suspended : {suspended}, Unknown : {unknown} ";

    foreach (var instance in Running)
    {
        response += "workflow instance id : " + instance.Key + " " + JsonSerializer.Serialize(instance.Value) + ", ";
    }
    return response;

}).Produces<string>();


app.MapPost("/fanout-workflow", [Topic("kafka-pubsub", "fanout-workflow")] async (DaprClient daprClient, DaprWorkflowClient workflowClient, CustomCloudEvent<StartWorklowRequest>? ce) =>
{
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
        return new StartWorkflowResponse()
        {
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
    catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse()
        {
            Id = workflowId + " error"
        };
    }

    return new StartWorkflowResponse()
    {
        Id = result
    };
}).Produces<StartWorkflowResponse>();


app.MapPost("/saga", [Topic("kafka-pubsub", "sagaTopic")] async (DaprClient daprClient, DaprWorkflowClient workflowClient, CustomCloudEvent<StartWorklowRequest>? ce) =>
{
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
        return new StartWorkflowResponse()
        {
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
    catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unknown && ex.Status.Detail.StartsWith("an active workflow with ID"))
    {
        app.Logger.LogError(ex, "Workflow already running : {workflowId}", workflowId);
        return new StartWorkflowResponse()
        {
            Id = workflowId + " error"
        };
    }

    return new StartWorkflowResponse()
    {
        Id = result
    };
}).Produces<StartWorkflowResponse>();


app.MapPost("/schedule-job", [Topic("kafka-pubsub", "schedule-job")] async (DaprClient daprClient, DaprWorkflowClient workflowClient, CustomCloudEvent<StartWorklowRequest>? ce) =>
{
    while (!await daprClient.CheckHealthAsync())
    {
        Thread.Sleep(TimeSpan.FromSeconds(5));
        app.Logger.LogInformation("waiting...");
    }

    app.Logger.LogInformation("ce.id {0}", ce.Id);

    string randomData = Guid.NewGuid().ToString();
    string jobId = ce.Data?.Id ?? $"{Guid.NewGuid().ToString()[..8]}";

    try
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri($"http://localhost:{Environment.GetEnvironmentVariable("DAPR_HTTP_PORT")}");

        using StringContent jsonContent = new(
            JsonSerializer.Serialize(new
            {
                data = new
                {
                    scheduled = DateTime.UtcNow
                },
                schedule = "@every 5s",
                repeats = 5
            }),
            System.Text.Encoding.UTF8,
            "application/json");

        var result = await httpClient.PostAsync($"v1.0-alpha1/jobs/{jobId}", jsonContent);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, $"Job scheduling error {jobId}");
        return new StartWorkflowResponse()
        {
            Id = jobId + " error"
        };
    }

    return new StartWorkflowResponse()
    {
        Id = jobId
    };
}).Produces<StartWorkflowResponse>();


// app.MapPost("/job/{jobId}", (string jobId, [FromBody] HelloWorld payload) =>
// {
//     var now = DateTime.UtcNow;
//     var timeSinceScheduled = now.Subtract(payload.scheduled);
//     app.Logger.LogInformation($"job triggered='{jobId}', timeSince='{timeSinceScheduled.TotalSeconds}', timeNow='{now:HH:mm:ss}',  scheduledAt='{payload.scheduled:HH:mm:ss}'");
//     return;
// });


app.Run();

public record WorkflowPayload(string RandomData, int Itterations = 1, Guid[]? Data = default);

public record ExternalSystemWorkflowPayload(bool failOnTimeout = false);

public class HelloWorld
{
    public DateTime scheduled { get; set; }
}

public class StartWorklowRequest
{
    public string Id { get; set; }
    public bool FailOnTimeout { get; set; }
    public int Sleep { get; set; }
    public string AbortHint { get; set; }
}

public class StartWorkflowResponse
{
    public string Id { get; set; }
    public string status { get; set; }
}

public class RaiseEvent<T>
{
    public string InstanceId { get; set; }
    public string EventName { get; set; }
    public T EventData { get; set; }
}