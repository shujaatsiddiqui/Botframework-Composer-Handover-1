using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using System.Collections.Generic;

namespace Microsoft.BotFramework.Composer.DAL.Services.Abstraction
{
    public interface ICommunicationService
    {
        void SendMessageToUserAsync(User user, string text, List<Attachment> attachments = null);
    }
}
