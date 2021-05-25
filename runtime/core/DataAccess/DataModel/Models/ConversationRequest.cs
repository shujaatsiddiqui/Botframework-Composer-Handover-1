using System;

namespace CivicCommunicator.DataAccess.DataModel.Models
{
    public class ConversationRequest
    {
        public int ConversationRequestId { get; set; }
        public DateTime CreationDate{ get; set; }
        public int RequesterId { get; set; }
        public User Requester { get; set; }
        public RequestState State { get; set; }
        public int? AgentId { get; set; }
        public User Agent { get; set; }
    }
}
