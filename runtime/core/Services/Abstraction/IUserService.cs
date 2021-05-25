using CivicCommunicator.DataAccess.DataModel.Models;
using Microsoft.Bot.Builder;

namespace CivicCommunicator.Services.Abstraction
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
