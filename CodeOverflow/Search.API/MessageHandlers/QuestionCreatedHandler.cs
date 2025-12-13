using Contracts;
using Search.API.Models;
using System.Text.RegularExpressions;
using Typesense;

namespace Search.API.MessageHandlers;

public class QuestionCreatedHandler(ITypesenseClient client)
{
    public async Task Handle(QuestionCreated message)
    {
        var created = new DateTimeOffset(message.Created).ToUnixTimeSeconds();

        var document = new SearchQuestion
        {
            Id = message.QuestionId,
            Title = message.Title,
            Content = StripHtml(message.Content),
            CreatedAt = created,
            Tags = message.Tags.ToArray(),
        };
        await client.CreateDocument("questions", document);

        Console.WriteLine($"Created question with id {message.QuestionId}");
    }
    private static string StripHtml(string content)
    {
        return Regex.Replace(content, "<.*?>", string.Empty);
    }
}

