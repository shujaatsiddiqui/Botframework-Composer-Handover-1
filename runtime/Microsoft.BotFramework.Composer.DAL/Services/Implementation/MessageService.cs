using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Abstraction;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Implementation;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
using System;

namespace Microsoft.BotFramework.Composer.DAL.Implementation
{
    public class MessageService : IMessageService
    {
        private readonly IRepository<Message> repository;

        public MessageService()
        {
            this.repository = new Repository<Message>(new BotDbContext());
        }

        public void StoreTheMessage(User user, string message)
        {
            this.repository.Add(new Message
            {
                FromId = user.UserId,
                Text = message,
                CreationDate = DateTime.Now
            });
        }
    }
}
