var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data");

var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("postgres-data")
    .WithPgAdmin();

var questionDb = postgres.AddDatabase("questionDb");

var typesenseApiKey = builder.AddParameter("typesense-api-key", secret: true);

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
    .WithArgs("--data-dir", "/data", "--api-key", typesenseApiKey, "--enable-cors")
    .WithVolume("typesense-data", "/data")
    .WithHttpEndpoint(8108, 8108, name: "typesense");

var typeSenseContainer = typesense.GetEndpoint("typesense");

var questionService =  builder.AddProject<Projects.Question_API>("question-srv")
    .WithReference(keycloak)
    .WithReference(questionDb)
    .WaitFor(keycloak)
    .WaitFor(questionDb);

var searchService = builder.AddProject<Projects.Search_API>("search-svc")
    .WithEnvironment("typesense-api-key", typesenseApiKey)
    .WithReference(typeSenseContainer)
    .WaitFor(typesense);

builder.Build().Run();

