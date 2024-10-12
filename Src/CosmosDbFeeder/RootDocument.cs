using Newtonsoft.Json;

namespace CosmosDbFeeder;

public class RootDocument
{
    [JsonProperty("id")]
    public string Id { get; set; }

    public string PartitionKey { get; set; }

    public Guid GuidProperty { get; set; }

    public bool BoolProperty { get; set; }

    public string StringProperty { get; set; }

    public int IntProperty { get; set; }

    public DateOnly DateOnlyProperty { get; set; }

    public decimal DecimalProperty { get; set; }

    public Dictionary<string, decimal> DictionaryProperty { get; set; }

    public InnerDocument[] InnerItems { get; set; }
}
