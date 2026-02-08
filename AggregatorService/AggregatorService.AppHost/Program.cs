var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .AddDatabase("tradingdb");

var apiService = builder.AddProject<Projects.AggregatorService_ApiService>("apiservice")
    .WithReference(postgres);

builder.AddProject<Projects.LoadGenerator>("loadgenerator")
       .WithExternalHttpEndpoints()
       .WithReference(apiService)
       .WaitFor(apiService);

builder.Build().Run();
