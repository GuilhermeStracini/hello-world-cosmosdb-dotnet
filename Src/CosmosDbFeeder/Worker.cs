using Bogus;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;

namespace CosmosDbFeeder;

internal class Worker
{
    private const int AmountToInsert = 1_000;
    private const int Cycles = 150_000;

    private readonly CosmosConfiguration _configuration;

    public Worker(CosmosConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task Start()
    {
        for (var i = 0; i < Cycles; i++)
        {
            Console.WriteLine($"Circle {i}");
            await Run(i);
        }
    }

    private async Task Run(int cycle)
    {
        var cosmosClient = new CosmosClient(
            _configuration.EndpointUrl,
            _configuration.AuthorizationKey,
            new CosmosClientOptions { AllowBulkExecution = true }
        );

        Microsoft.Azure.Cosmos.Database database =
            await cosmosClient.CreateDatabaseIfNotExistsAsync(_configuration.DatabaseName);

        try
        {
            Console.WriteLine("Creating items...");
            var itemsToInsert = GetItemsToInsert();

            Console.WriteLine("Starting...");
            var stopwatch = Stopwatch.StartNew();
            var container = database.GetContainer(_configuration.ContainerName);
            var tasks = new List<Task>(AmountToInsert);
            foreach (var item in itemsToInsert)
            {
                tasks.Add(
                    container
                        .UpsertItemAsync(item, new PartitionKey(item.PartitionKey))
                        .ContinueWith(itemResponse =>
                        {
                            Console.WriteLine(
                                $"Cycle: {cycle} - {itemResponse.Result.Resource.PartitionKey}"
                            );
                            if (!itemResponse.IsCompletedSuccessfully)
                            {
                                AggregateException innerExceptions =
                                    itemResponse.Exception?.Flatten();
                                if (
                                    innerExceptions?.InnerExceptions.FirstOrDefault(
                                        innerEx => innerEx is CosmosException
                                    )
                                    is CosmosException cosmosException
                                )
                                {
                                    Console.WriteLine(
                                        $"Received {cosmosException.StatusCode} ({cosmosException.Message})."
                                    );
                                }
                                else
                                {
                                    Console.WriteLine(
                                        $"Exception {innerExceptions?.InnerExceptions.FirstOrDefault()}."
                                    );
                                }
                            }
                        })
                );
            }

            await Task.WhenAll(tasks);
            tasks.Clear();

            stopwatch.Stop();

            await Task.Delay(1000);

            Console.WriteLine(
                $"Finished in writing {AmountToInsert} items in {stopwatch.Elapsed}."
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private static IReadOnlyCollection<RootDocument> GetItemsToInsert()
    {
        Randomizer.Seed = new Random(8675309);

        var itemFaker = new Faker<InnerDocument>()
            .StrictMode(true)
            .RuleFor(o => o.Id, f => f.Random.Guid())
            .RuleFor(o => o.DateCreated, f => f.Date.FutureOffset())
            .RuleFor(o => o.TestFlag, f => f.Random.Bool(.3f));
        return new Faker<RootDocument>()
            .StrictMode(true)
            .RuleFor(o => o.PartitionKey, f => f.IndexGlobal.ToString())
            .RuleFor(o => o.GuidProperty, f => f.Random.Guid())
            .RuleFor(o => o.BoolProperty, f => f.Random.Bool())
            .RuleFor(o => o.StringProperty, f => f.Random.String2(10))
            .RuleFor(o => o.IntProperty, f => f.Random.Int(0, 1000000))
            .RuleFor(o => o.DateOnlyProperty, f => f.Date.PastDateOnly())
            .RuleFor(o => o.DecimalProperty, f => f.Random.Decimal(0, 1000000))
            .RuleFor(o => o.Id, (f, o) => $"SomeId-{f.Random.Number(1, 10)}")
            .RuleFor(
                o => o.DictionaryProperty,
                (f, o) =>
                    new Dictionary<string, decimal>
                    {
                        { "ABC", f.Random.Decimal(0, 1000000) },
                        { "DEF", f.Random.Decimal(0, 1000000) },
                        { "GHI", f.Random.Decimal(0, 1000000) }
                    }
            )
            .RuleFor(o => o.InnerItems, (f, o) => itemFaker.GenerateBetween(1, 15).ToArray())
            .Generate(AmountToInsert);
    }
}
