namespace WorkflowConsoleApp
{
    public sealed class DaprJobsService : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<DaprJobsService> logger;

        public DaprJobsService(HttpClient httpClient, ILogger<DaprJobsService> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }
        public async Task EnsureThrottleJobIsRunning()
        {
            var result = await httpClient.GetAsync("ensurethrottle");
            logger.LogDebug($"GET job `ensure-throttle` result : {result.StatusCode.ToString()}");
            if (!result.IsSuccessStatusCode)
            {
                var createResult = await httpClient.PostAsJsonAsync("ensurethrottle", new
                {
                    data = new
                    {
                        scheduled = DateTime.UtcNow
                    },
                    schedule = "@every 5s"
                });
                logger.LogInformation($"CREATE job `ensure-throttle` result : {createResult.StatusCode.ToString()}");
                createResult.EnsureSuccessStatusCode();
            }
        }

        public void Dispose() => httpClient?.Dispose();
    }

}