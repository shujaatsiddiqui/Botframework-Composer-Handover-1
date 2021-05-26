
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;

namespace Microsoft.BotFramework.Composer.DAL.Services.Abstraction
{
    public interface IMessageService
    {
        void StoreTheMessage(User user, string message);
    }
}
