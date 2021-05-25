using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.DataAccess.Repository.Abstraction;
using CivicCommunicator.Services.Abstraction;
using System;

namespace CivicCommunicator.Services.Implementation
{
    public class MessageService : IMessageService
    {
        private readonly IRepository<Message> repository;

        public MessageService(IRepository<Message> repository)
        {
            this.repository = repository;
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
