using System;

namespace CivicCommunicator.DataAccess.DataModel.Models
{
    public class BotReply
    {
        public int BotReplyId { get; set; }
        public string Text { get; set; }
        public int ToId { get; set; }
        public User To { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
