# dapr-workflow-examples

## Pre-requisites

- Dapr installed locally for stand-alone mode (`dapr run`)
- Dapr installed in a kubernetes cluster
- A suitable Dapr State Store component installed in the kubernetes cluster. Must be a State Store component which is compatible with Dapr Actors i.e. [Redis](https://docs.dapr.io/getting-started/tutorials/configure-state-pubsub/#step-1-create-a-redis-store)

## To run locally

*This has only been tested on MacOS with VS Code*

`dotnet build`

`dapr run dotnet run`

### Start a workflow locally

POST `http://localhost:{app-port}/Workflow`

> This will respond with a workflow Id i.e. `12345678` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow locally

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-you-put-here/12345678`

---


## To run in Kubernetes (via Docker Desktop)

`dapr-workflow-examples % docker build -f WorkflowApi/Dockerfile -t workflowtest .`

`dapr-workflow-examples % kubectl apply -f ./deploy.yaml`

---

### Start a workflow in Kubernetes

POST `http://localhost:{dapr-http-port}/v1.0/invoke/workflow/method/Workflow` 

This will respond with a workflow Id i.e. `12345678` 

### Check the status of the workflow in kubernetes

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-you-put-here/12345678`

