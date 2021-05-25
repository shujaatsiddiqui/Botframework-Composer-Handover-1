using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.Services.Abstraction;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace CivicCommunicator.Services.Implementation
{
    public class CommunicationService : ICommunicationService
    {
        private IConfiguration configuration;

        private MicrosoftAppCredentials GetMicrosoftAppCredential 
            => new MicrosoftAppCredentials(configuration["MicrosoftAppId"], configuration["MicrosoftAppPassword"]);

        public CommunicationService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void SendMessageToUserAsync(User user, string text, List<Attachment> attachments = null)
        {
            var userAccount = new ChannelAccount(id: user.ChatId, name: user.Name);
            var botAccount = new ChannelAccount(id: user.BotChannelId);

            var connector = new ConnectorClient(new Uri(user.ServiceUrl), this.GetMicrosoftAppCredential);
           
            var message = Activity.CreateMessageActivity();
            message.From = botAccount;
            message.ChannelId = user.ChannelId;
            message.Recipient = userAccount;
            message.Conversation = new ConversationAccount(id: user.ConversationId, isGroup: null);
            message.Text = text;
            message.Attachments = attachments;
            message.Locale = "en-US";
            message.ServiceUrl = user.ServiceUrl;
            message.LocalTimestamp = DateTime.Now;

            connector.Conversations.SendToConversation((Activity)message);
        }
    }
}
