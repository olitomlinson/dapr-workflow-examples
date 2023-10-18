using System.Text.Json.Serialization;
using System.Net.Mime;

namespace Workflow;


public class CloudEvent2<TData> : Dapr.CloudEvent<TData>
{
    public CloudEvent2(TData data) : base(data)
    {
        
    }

    [JsonPropertyName("id")]
    public string Id { get; init; }

    [JsonPropertyName("specversion")]
    public string Specversion { get; init; }
}