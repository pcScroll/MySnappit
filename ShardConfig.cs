
//1.Shared Data Service
//This service holds and manages the shared data.

public class SharedDataService
{
    private readonly object _lock = new();

    public List<MyDataObject> SharedData { get; private set; } = new();

    public void UpdateData(List<MyDataObject> data)
    {
        lock (_lock)
        {
            SharedData = new List<MyDataObject>(data);
        }
    }
}


//2.Hosted Service
//The HostedService fetches data from the database during application startup.

using Microsoft.Extensions.Hosting;

public class DataFetchingHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly SharedDataService _sharedDataService;

    public DataFetchingHostedService(IServiceProvider services, SharedDataService sharedDataService)
    {
        _services = services;
        _sharedDataService = sharedDataService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        // Fetch data from the database
        var data = await dbContext.MyEntities
            .Select(e => new MyDataObject
            {
                Property1 = e.Property1,
                Property2 = e.Property2
            })
            .ToListAsync(cancellationToken);

        // Update shared data
        _sharedDataService.UpdateData(data);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}


//3.Refresh Data Method
//Add a method in SharedDataService to refresh the data on demand

public async Task RefreshDataAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    // Fetch data from the database
    var data = await dbContext.MyEntities
        .Select(e => new MyDataObject
        {
            Property1 = e.Property1,
            Property2 = e.Property2
        })
        .ToListAsync();

    // Update shared data
    UpdateData(data);
}


//4.Register Services in Program.cs
//Add the HostedService and SharedDataService to the service container.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<SharedDataService>();
builder.Services.AddHostedService<DataFetchingHostedService>();

var app = builder.Build();

// API endpoint to access shared data
app.MapGet("/data", (SharedDataService sharedDataService) =>
{
    return Results.Ok(sharedDataService.SharedData);
});

// API endpoint to refresh data on demand
app.MapPost("/refresh-data", async (SharedDataService sharedDataService, IServiceProvider services) =>
{
    await sharedDataService.RefreshDataAsync(services);
    return Results.Ok("Data refreshed successfully.");
});

app.Run();


// Benefits of This Approach

// Startup Initialization: Ensures the application has data ready when it starts.
// On-Demand Updates: Provides flexibility to refresh data when needed.
// Separation of Concerns: The HostedService handles startup tasks, while the API provides user-initiated control.
// This solution is both robust and easy to maintain!