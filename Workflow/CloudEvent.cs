using System.Text.Json.Serialization;
using System.Net.Mime;

namespace workflow;

public class CloudEvent2<TData> : Dapr.CloudEvent<TData>
{
    public CloudEvent2(TData data) : base(data)
    {
        
    }

    [JsonPropertyName("id")]
    public string Id { get; init; }

    [JsonPropertyName("specversion")]
    public string Specversion { get; init; }

    [JsonPropertyName("my-custom-property")]
    public string MyCustomProperty { get ;init; }
}