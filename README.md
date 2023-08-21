# dapr-workflow-examples

This is now a repo for helping reproduce and debug various Dapr Workflow issues.


### Run compose as normal

1. `docker compose build`
2. `docker compose up`

### Run compose, but with a local debug for the Workflow app

1. `docker compose build`
2. `docker compose -f compose.yml -f compose.debug-override.yml up`
3. Run the launch task `Debug workflow app` to debug the Workflow app

