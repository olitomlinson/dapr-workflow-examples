# dapr-workflow-examples

This repo is for load testing Dapr Workflows via Docker Compose. 

In the past, this has flushed out many concurrency issues in the underlying durabletask-go library, which have been subsequently addressed in newer versions of the Dapr runtime. `main` currently targets `1.14.0-rc.2` of the runtime which is the first vresion to address the horizontal scaling constraints by inclusion of the new Actor Reminder sub-system.

### Run with 2 instances of the Workflow App

1. `docker compose build`
2. `docker compose -f compose-2-instances.yml up`

### Run with 3 instances of the Workflow App

1. `docker compose build`
2. `docker compose -f compose-3-instances.yml up`



### Run a simple workflow

This will create many workflow instances randomly distributed across the Workflow App instances.

Run a simple workflow by making a POST request to 

```http://localhost:5112/start?runId={runId}&count=1000&async=true```

- Where `{runId}` is a unique value i.e. UUID/GUID.
- Increase/decrease the amount of workflows created by changing the `count` property

#### What does the workflow do?

Go to `workflow/workflows/MonitorWorkflow.cs` to see the code of the workflow. 

In short summary, this workflow will Call an `Activity` with a very small payload (`Activities/FastActivity.cs`) and the Activity will return a very small payload in response. The payload is not used for any purpose other than logging out it's progress. Once the Activity completes and control is returned to the Workflow, the workflow will go to sleep for 3 seconds. After which the workflow will enter into the eternal workflow pattern, and the above process will repeat. The process will run 10 times. After which the Workflow will exit successfully. 

A typical E2E duration of the workflow is approx 30 seconds. However running more workflow instances concurrently will increase the E2E duration of each workflow, which is expected given the resource-bound environment of using docker compose. 

This would be addressed by horizontally scaling the Workflow App across more compute resources (i.e. in a real kubernetes environment) - this is something I aim to test in the near future and provide an example for.

