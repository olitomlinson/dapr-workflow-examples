using System.Text.Json.Serialization;
using System.Net.Mime;

namespace workflow;

public class CustomCloudEvent<TData> : Dapr.CloudEvent<TData>
{
    public CustomCloudEvent(TData data) : base(data)
    {

    }

    [JsonPropertyName("id")]
    public string Id { get; init; }

    [JsonPropertyName("specversion")]
    public string Specversion { get; init; }

    [JsonPropertyName("my-custom-property")]
    public string MyCustomProperty { get; init; }
}