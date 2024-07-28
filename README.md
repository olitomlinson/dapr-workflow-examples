# dapr-workflow-examples

This repo is for load testing Dapr Workflows via Docker Compose. 

In the past, this has flushed out many concurrency issues in the underlying durabletask-go library, which have been subsequently addressed in newer versions of the Dapr runtime. `main` currently targets `1.14.0-rc6` of the runtime which is the first vresion to address the horizontal scaling constraints by inclusion of the new Actor Reminder sub-system.

### Run with 2 instances of the Workflow App

1. `docker compose -f compose-2-instances.yml build`
2. `docker compose -f compose-2-instances.yml up`

### Run with 3 instances of the Workflow App

1. `docker compose -f compose-3-instances.yml build`
2. `docker compose -f compose-3-instances.yml up`

*Important*: When swapping between 2 and 3 instances, it is important to delete the existing topics that are created in Kafka, so that they are recreated with the correct number of partitions. This is easy to do via Kafka UI that is running on `localhost:8080`

### Run a simple workflow

This will create many workflow instances randomly distributed across the Workflow App instances.

Run a simple workflow by making a POST request to 

```http://localhost:5112/start/monitor-workflow?runId={runId}&count=1000&async=true```

- Where `{runId}` is a unique value i.e. UUID/GUID.
- Increase/decrease the amount of workflows created by changing the `count` property

#### What does the workflow do?

Go to `workflow/workflows/MonitorWorkflow.cs` to see the code of the workflow. 

In short summary, this workflow will Call an `Activity` with a very small payload (`Activities/SlowActivity.cs`) and the Activity will wait for 3 seconds and then return a very small payload in response. The payload is not used for any purpose other than logging out it's progress. Once the Activity completes and control is returned to the Workflow, the workflow will go to sleep for 3 seconds. After which the workflow will enter into the eternal workflow pattern, and the above process will repeat. The process will run 10 times. After which the Workflow will exit successfully. 
