var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.GenesisFEPortalWebApp_ApiService>("apiservice");

builder.AddProject<Projects.GenesisFEPortalWebApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
