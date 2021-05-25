using Microsoft.Bot.Builder;
using System.Threading;

namespace CivicCommunicator.Services.Abstraction
{
    public interface ILuisService
    {
        bool WantToTalkWithHuman(ITurnContext context, CancellationToken cancellationToken);
    }
}
