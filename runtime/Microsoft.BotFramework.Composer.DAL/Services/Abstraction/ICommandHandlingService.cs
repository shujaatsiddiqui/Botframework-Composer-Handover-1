using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading;

namespace Microsoft.BotFramework.Composer.DAL.Services.Abstraction
{
    public interface ICommandHandlingService
    {
        IActivity HandleCommand(ITurnContext context, CancellationToken cancellationToken);
    }
}
