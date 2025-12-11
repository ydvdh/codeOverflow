using Search.API.Data;
using Search.API.Models;
using System.Text.RegularExpressions;
using Typesense;
using Typesense.Setup;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.AddServiceDefaults();

var typesenseUri = builder.Configuration["services:typesense:typesense:0"];
if (string.IsNullOrWhiteSpace(typesenseUri))
    throw new InvalidOperationException("Typesense URI not found in configuration");

var typesenseApiKey = builder.Configuration["typesense-api-key"];
if (string.IsNullOrWhiteSpace(typesenseApiKey))
    throw new InvalidOperationException("Typesense API key not found in configuration");

var uri = new Uri(typesenseUri);
builder.Services.AddTypesenseClient(config =>
{
    config.ApiKey = typesenseApiKey;
    config.Nodes = new List<Node>
    {
        new(uri.Host, uri.Port.ToString(), uri.Scheme)
    };
});


var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/search", async (string query, ITypesenseClient tsClient) =>
{
    // Extract [tag] from the query (e.g., [aspire])
    string? tag = null;
    var tagMatch = Regex.Match(query, @"\[(.*?)\]");
    
    if (tagMatch.Success)
    {
        tag = tagMatch.Groups[1].Value;
        query = Regex.Replace(query, @"\[(.*?)\]", "").Trim();
    }

    var searchParameters = new SearchParameters(query,  "title,content");

    if (!string.IsNullOrEmpty(tag))
    {
        searchParameters.FilterBy = $"tags:=[{tag}]";
    }

    try
    {
        var result = await tsClient.Search<SearchQuestion>("questions", searchParameters);
        return Results.Ok(result.Hits.Select(hit=>hit.Document));
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error during Typesense search: {ex.Message}");
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();
var client = scope.ServiceProvider.GetRequiredService<ITypesenseClient>();
await SearchInitializer.EnsureIndexExists(client);

app.Run();

