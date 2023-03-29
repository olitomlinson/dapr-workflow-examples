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

POST `http://localhost:{dapr-app-port}/Workflow`

> This will respond with a workflow Id i.e. `12345678` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-you-put-here/12345678`

---


## To run in Kubernetes (via Docker Desktop)

Build the workflow :

`dapr-workflow-examples % docker build -f WorkflowApi/Dockerfile -t workflowtest .`

Deploy the workflow image to the cluster via `kubectl` :

`dapr-workflow-examples % kubectl apply -f ./deploy.yaml`

> **Note** Remember to install a suitable Dapr State Store component in the kubernetes cluster. It must be a State Store component which is compatible with Dapr Actors i.e. Redis.


### Start a workflow

POST `http://localhost:{app-port}/v1.0/invoke/workflow/method/Workflow` 

This will respond with a workflow Id i.e. `12345678` 

### Check the status of the workflow

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-you-put-here/12345678`

---

## To run via Docker Compose

Build the workflow image : 

`dapr-workflow-examples % docker build -f WorkflowApi/Dockerfile -t workflowtest .`

Deploy the workflow image via Docker Compose :

`dapr-workflow-examples % docker compose up`

### Start a workflow

POST `http://localhost:5111/Workflow`

> This will respond with a workflow Id i.e. `12345678` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow

GET `http://localhost:3500/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-put-here/12345678`

---

## To debug within container via Docker Compose (VS Code)

Install docker VS Code extension `https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker`
Install c# VS Code extension `https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp`

Build the image first (if you've changed any of the source, of course)

`dapr-workflow-examples % docker compose build`

Then deploy via Docker Compose

`dapr-workflow-examples % docker compose up`

Debug via the Task `Docker .NET Attach (Preview)`

<img width="394" alt="image" src="https://user-images.githubusercontent.com/4224880/226457356-00d13f1d-d16a-481c-a126-663a84af7970.png">

Then chose the following when prompted

`dapr-workflow-examples`
`dapr-workflow-examples-workflowapp`

choose `Yes` at the prompt

<img width="346" alt="image" src="https://user-images.githubusercontent.com/4224880/226458631-13daa7e2-5013-4637-a4b8-acf2f8aded22.png">

Debug points in the code can now be reached

<img width="1019" alt="image" src="https://user-images.githubusercontent.com/4224880/226459366-458408a8-017c-4a37-b89e-681f8668014e.png">




