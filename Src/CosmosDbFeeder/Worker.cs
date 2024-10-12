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

    /// <summary>
    /// Initiates a series of cycles, executing a specified task for each cycle.
    /// </summary>
    /// <remarks>
    /// This asynchronous method runs a loop for a defined number of cycles, indicated by the variable <paramref name="Cycles"/>.
    /// For each cycle, it outputs the current cycle number to the console and then calls the <see cref="Run(int)"/> method, 
    /// passing the current cycle index as an argument. The <see cref="Run(int)"/> method is expected to perform some 
    /// asynchronous operation related to the cycle. This method allows for concurrent execution of tasks, making it suitable 
    /// for scenarios where operations can be performed in parallel without blocking the main thread.
    /// </remarks>
    public async Task Start()
    {
        for (var i = 0; i < Cycles; i++)
        {
            Console.WriteLine($"Circle {i}");
            await Run(i);
        }
    }

    /// <summary>
    /// Asynchronously runs a cycle to create and upsert items into a Cosmos DB container.
    /// </summary>
    /// <param name="cycle">The cycle number for logging purposes.</param>
    /// <remarks>
    /// This method initializes a CosmosClient using the provided configuration settings, and attempts to create a database if it does not already exist.
    /// It retrieves a list of items to insert and upserts each item into the specified container in parallel.
    /// The method logs the status of each upsert operation, including any exceptions that may occur during the process.
    /// It measures the time taken to complete the insertion of items and logs this information upon completion.
    /// If any exceptions are encountered during the upsert operations, they are caught and logged to the console.
    /// </remarks>
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

    /// <summary>
    /// Generates a collection of <see cref="RootDocument"/> items to be inserted.
    /// </summary>
    /// <returns>A read-only collection of <see cref="RootDocument"/> instances.</returns>
    /// <remarks>
    /// This method utilizes the Bogus library to create fake data for testing or seeding purposes.
    /// It first sets a fixed seed for the random number generator to ensure consistent results across calls.
    /// 
    /// The method creates a faker for the <see cref="InnerDocument"/> type, defining rules for generating its properties, 
    /// including a GUID for the Id, a future date for DateCreated, and a boolean for TestFlag with a 30% chance of being true.
    /// 
    /// Subsequently, it creates a faker for the <see cref="RootDocument"/> type, defining rules for its properties such as:
    /// - PartitionKey as a string representation of the global index.
    /// - GuidProperty as a randomly generated GUID.
    /// - BoolProperty as a randomly generated boolean value.
    /// - StringProperty as a random string of length 10.
    /// - IntProperty as a random integer between 0 and 1,000,000.
    /// - DateOnlyProperty as a past date only.
    /// - DecimalProperty as a random decimal value between 0 and 1,000,000.
    /// - Id as a string formatted with a prefix and a random number between 1 and 10.
    /// - DictionaryProperty as a dictionary containing three key-value pairs with random decimal values.
    /// - InnerItems as an array of randomly generated <see cref="InnerDocument"/> instances, with a count between 1 and 15.
    /// 
    /// Finally, the method generates and returns the specified number of <see cref="RootDocument"/> instances defined by the AmountToInsert variable.
    /// </remarks>
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
