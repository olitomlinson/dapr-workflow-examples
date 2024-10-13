# dapr-workflow-examples

This repo is for load testing Dapr Workflows via Docker Compose. 

In the past, this has flushed out many concurrency issues in the underlying durabletask-go library, which have been subsequently addressed in newer versions of the Dapr runtime. `main` currently targets `1.14.x` of the runtime which is the first version to address the horizontal scaling constraints by inclusion of the new Actor Reminder sub-system.

### Run with 5 instances of the Workflow App, and 3 instances of the scheduler service

1. `docker compose -f compose-5-instances-3-schedulers.yml build`
2. `docker compose -f compose-5-instances-3-schedulers.yml up`


### Run a simple monitor pattern workflow

This will create many workflow instances randomly distributed across the Workflow App instances.

Run the workflows by making a POST request to :

```http://localhost:5112/start/monitor-workflow?runId={runId}&count=1000&async=true```

- Where `{runId}` is a unique value i.e. UUID/GUID.
- Increase/decrease the amount of workflows created by changing the `count` property

### Run a simple fan-out & fan-in pattern workflow

This will create many workflow instances randomly distributed across the Workflow App instances.

Run the workflows by making a POST request to :

```http://localhost:5112/start/fanout-workflow?runId={runId}&count=1000&async=true```

- Where `{runId}` is a unique value i.e. UUID/GUID.
- Increase/decrease the amount of workflows created by changing the `count` property
