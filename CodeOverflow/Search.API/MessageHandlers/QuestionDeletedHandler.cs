using Contracts;
using Search.API.Models;
using Typesense;

namespace Search.API.MessageHandlers;

public class QuestionDeletedHandler(ITypesenseClient tsClient)
{
    public async Task Handle(QuestionDeleted message)
    {
        await tsClient.DeleteDocument<SearchQuestion>("questions", message.QuestionId);
    }
}
