using CivicCommunicator.DataAccess.DataModel;
using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.DataAccess.Repository.Abstraction;
using CivicCommunicator.DataAccess.Repository.Implementation;
using CivicCommunicator.Services.Abstraction;
using System;

namespace CivicCommunicator.Services.Implementation
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
