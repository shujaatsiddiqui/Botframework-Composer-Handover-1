using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivicCommunicator.DataAccess.DataModel.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string ChatId { get; set; }
        public string ServiceUrl { get; set; }
        public bool IsAgent { get; set; }
        public bool IsOnline { get; set; }
        public string Name { get; set; }
        public string ConversationId { get; set; }
        public string ChannelId { get; set; }
        public string BotChannelId { get; set; }
        public string HandlingDomain { get; set; }
        public string SiteDomain { get; set; }

        public ICollection<ConversationRequest> Requested { get; set; }
        public ICollection<ConversationRequest> Handled { get; set; }
        public ICollection<Message> Messages { get; set; }

        public ICollection<BotReply> BotReplies { get; set; }
    }
}
