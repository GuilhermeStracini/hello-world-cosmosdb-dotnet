namespace CosmosDbFeeder;

public class CosmosConfiguration
{
    public const string SectionName = "Cosmos";

    public string EndpointUrl { get; set; } = null!;

    public string AuthorizationKey { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string ContainerName { get; set; } = null!;
}
