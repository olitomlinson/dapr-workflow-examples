# dapr-workflow-examples

The purpose of this sample application is to simply demonstrate the capabilities of [Dapr Workflows](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-overview/) as they are enhanced over time by the Dapr Team and Open Source community

As it stands Dapr Workflows can 
- Call [Activities](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/#workflow-activities) to perform work
- Can [perform an eternal loop](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/#infinite-loops-and-eternal-workflows) which exits on condition being met.

In this example, when the `/start` endpoint is called [here](https://github.com/olitomlinson/dapr-workflow-examples/blob/0a0d54687aa43c32e630e6fd0def0ce49d933dbe/Client/Program.cs#L23), a new Workflow instance is scheduled to run. The workflow will call the same Activity 11 times via an eternal loop [here](https://github.com/olitomlinson/dapr-workflow-examples/blob/0a0d54687aa43c32e630e6fd0def0ce49d933dbe/Workflow/Workflows/ContinueAsNewWorkflow.cs#L12), at which point the Workflow exits successfuly and is marked as completed [here](https://github.com/olitomlinson/dapr-workflow-examples/blob/0a0d54687aa43c32e630e6fd0def0ce49d933dbe/Workflow/Workflows/ContinueAsNewWorkflow.cs#L17).

At any point in time (while the Workflow is running, or after it has completed) you can check the status of that Workflow instance.

This sample application utilises 2 services (`Client` and `Workflow`) this is purely to demonstrate Dapr Service to Service Invocation and Dapr PubSub capabilities, as well as Dapr Workflows. If desired, you can simply call the `/start` endpoint on the `Workflow` service directly.

The recommended way to run this sample application is via Docker Compose due to its ease of use.

The diagram below shows at high-level the various components and services that are deployed via Docker Compose.

![image](https://user-images.githubusercontent.com/4224880/232878508-bcbfa5ce-6c0b-4b97-b573-bb489197005c.png)

## To run via Docker Compose

Build the workflow image : 

`% docker compose build`

Deploy the workflow image via Docker Compose :

`% docker compose up`

### Start a workflow

POST `http://localhost:5112/start`

> This will respond with a workflow Id i.e. `123456` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow

GET `http://localhost:3500/v1.0-alpha1/workflows/dapr/_/123456`

---

## To debug within container via Docker Compose (VS Code)

- Install docker VS Code extension `https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker`
- Install c# VS Code extension `https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp`

Build the image first (if you've changed any of the source, of course)

`% docker compose build`

Then deploy via Docker Compose

`% docker compose up`

Debug via the Task `Docker .NET Attach (Preview)`

<img width="394" alt="image" src="https://user-images.githubusercontent.com/4224880/226457356-00d13f1d-d16a-481c-a126-663a84af7970.png">

Then chose the following when prompted

`dapr-workflow-examples` then...
`dapr-workflow-examples-client-app` or `dapr-workflow-examples-workflow-app`

choose `Yes` at the prompt

<img width="346" alt="image" src="https://user-images.githubusercontent.com/4224880/226458631-13daa7e2-5013-4637-a4b8-acf2f8aded22.png">

Debug points in the code can now be reached

<img width="1019" alt="image" src="https://user-images.githubusercontent.com/4224880/226459366-458408a8-017c-4a37-b89e-681f8668014e.png">


---

## To run locally

- Dapr installed locally for stand-alone mode (`dapr run`) - _tested with dapr 1.10.4_
- A suitable Dapr State Store component installed in the kubernetes cluster. Must be a State Store component which is compatible with Dapr Actors i.e. [Redis](https://docs.dapr.io/getting-started/tutorials/configure-state-pubsub/#step-1-create-a-redis-store)

*This has only been tested on MacOS with VS Code*

`dotnet build`

`dapr run dotnet run`

### Start a workflow

POST `http://localhost:{dapr-app-port}/start`

> This will respond with a workflow Id i.e. `12345678` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/_/123456`

---


## To run in Kubernetes (via Docker Desktop)

- Dapr installed in a kubernetes cluster -  _tested with dapr 1.10.4_
- A suitable Dapr State Store component installed in the kubernetes cluster. Must be a State Store component which is compatible with Dapr Actors i.e. [Redis](https://docs.dapr.io/getting-started/tutorials/configure-state-pubsub/#step-1-create-a-redis-store)

Build the workflow :

`% docker build -f WorkflowApi/Dockerfile -t workflowtest .`

Deploy the workflow image to the cluster via `kubectl` :

`% kubectl apply -f ./deploy.yaml`

> **Note** Remember to install a suitable Dapr State Store component in the kubernetes cluster. It must be a State Store component which is compatible with Dapr Actors i.e. Redis.


### Start a workflow

POST `http://localhost:{app-port}/v1.0/invoke/workflow/method/start` 

This will respond with a workflow Id i.e. `123456` 

### Check the status of the workflow

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/_/123456`



