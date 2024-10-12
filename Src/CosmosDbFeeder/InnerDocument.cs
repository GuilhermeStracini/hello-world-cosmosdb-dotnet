namespace CosmosDbFeeder;

public class InnerDocument
{
    public Guid Id { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public bool TestFlag { get; set; }
}
