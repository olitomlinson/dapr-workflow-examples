# dapr-workflow-examples

## Pre-requisites

- Dapr installed locally for stand-alone mode (`dapr run`)
- Dapr installed in a kubernetes cluster
- A suitable Dapr State Store component installed in the kubernetes cluster. Must be a State Store component which is compatible with Dapr Actors i.e. [Redis](https://docs.dapr.io/getting-started/tutorials/configure-state-pubsub/#step-1-create-a-redis-store)

## To run locally

*This has only been tested on MacOS with VS Code*

`dotnet build`

`dapr run dotnet run`

### Start a workflow

POST `http://localhost:{app-port}/Workflow`

> This will respond with a workflow Id i.e. `12345678` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-you-put-here/12345678`

---


## To run in Kubernetes (via Docker Desktop)

`dapr-workflow-examples % docker build -f WorkflowApi/Dockerfile -t workflowtest .`

`dapr-workflow-examples % kubectl apply -f ./deploy.yaml`


### Start a workflow

POST `http://localhost:{dapr-http-port}/v1.0/invoke/workflow/method/Workflow` 

This will respond with a workflow Id i.e. `12345678` 

### Check the status of the workflow

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-you-put-here/12345678`

---

## To run via Docker Compose

Build the image first 

`dapr-workflow-examples % docker build -f WorkflowApi/Dockerfile -t workflowtest .`

Then you can deploy via Docker Compose

`dapr-workflow-examples % docker-compose up`

### Start a workflow

POST `http://localhost:5111/Workflow`

> This will respond with a workflow Id i.e. `12345678` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow

GET `http://localhost:3500/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-put-here/12345678`