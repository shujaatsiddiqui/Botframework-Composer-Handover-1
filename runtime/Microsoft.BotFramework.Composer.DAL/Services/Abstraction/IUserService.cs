using Microsoft.Bot.Builder;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;

namespace Microsoft.BotFramework.Composer.DAL.Services.Abstraction
{
    public interface IUserService
    {
        User GetUserModel(ITurnContext turnContext);
        User TryUpdate(User user, ITurnContext context);
        User GetUserModel(int id);
        User RegisterUser(ITurnContext turnContext);
        void MarkUserAsAgent(ITurnContext turnContext);
    }
}
