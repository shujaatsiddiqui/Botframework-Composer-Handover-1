using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace CivicCommunicator.Services.Abstraction
{
    public interface IActionsService
    {
        IActivity HandleAction(ITurnContext context);
    }
}
