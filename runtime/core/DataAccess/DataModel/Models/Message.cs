using System;

namespace CivicCommunicator.DataAccess.DataModel.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public string Text { get; set; }
        public int FromId { get; set; }
        public User From { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
