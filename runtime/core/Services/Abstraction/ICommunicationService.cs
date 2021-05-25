using CivicCommunicator.DataAccess.DataModel.Models;
using Microsoft.Bot.Schema;
using System.Collections.Generic;

namespace CivicCommunicator.Services.Abstraction
{
    public interface ICommunicationService
    {
        void SendMessageToUserAsync(User user, string text, List<Attachment> attachments = null);
    }
}
