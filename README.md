# dapr-workflow-examples

## To run locally

*This has only been tested on MacOS with VS Code*

`dotnet build`

`dapr run dotnet run`


### Start a workflow 

POST `http://localhost:{app-port}/Workflow` 

This will respond with a workflow Id i.e. `12345678` (Note that this is not going through the dapr sidecar, but targeting your web server directly)

### Check the status of the workflow 

GET `http://localhost:{dapr-http-port}/v1.0-alpha1/workflows/dapr/it-doesnt-matter-what-you-put-here/12345678`

## To run in Kubernetes (via Docker Desktop)

`dapr-workflow-examples % docker build -f WorkflowApi/Dockerfile -t workflowtest .`

`dapr-workflow-examples % kubectl apply -f ./deploy.yaml`
