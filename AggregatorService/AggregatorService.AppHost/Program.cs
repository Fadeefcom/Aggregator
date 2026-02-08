var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .AddDatabase("tradingdb");

var loadGenerator = builder.AddProject<Projects.LoadGenerator>("loadgenerator");

builder.AddProject<Projects.AggregatorService_ApiService>("apiservice")
    .WithReference(postgres)
    .WithReference(loadGenerator)
    .WaitFor(loadGenerator);

builder.Build().Run();