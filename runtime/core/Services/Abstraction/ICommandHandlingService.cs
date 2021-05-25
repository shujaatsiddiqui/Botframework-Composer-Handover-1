using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading;

namespace CivicCommunicator.Services.Abstraction
{
    public interface ICommandHandlingService
    {
        IActivity HandleCommand(ITurnContext context, CancellationToken cancellationToken);
    }
}
