using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.DataAccess.Repository.Abstraction;
using CivicCommunicator.Services.Abstraction;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CivicCommunicator.Services.Implementation
{
    public class OrchestrizationService : IOrchestrizationService
    {
        private static object locker = new object();
        private readonly ICommunicationService communicationService;
        private readonly ICardService cardService;
        private readonly IRepository<User> userRepository;
        private readonly IRepository<ConversationRequest> requestRepository;

        public OrchestrizationService(ICommunicationService communicationService, 
            ICardService cardService,
            IRepository<User> userRepository,
            IRepository<ConversationRequest> requestRepository)
        {
            this.communicationService = communicationService;
            this.cardService = cardService;
            this.userRepository = userRepository;
            this.requestRepository = requestRepository;
        }

        public void NavigateRequestToAgent()
        {
            lock (locker)
            {

                var requestToHandle = this.requestRepository
                    .AsQueryable()
                    .Include(x => x.Requester)
                    .Where(x => x.State == RequestState.Pending)
                    .OrderBy(x => x.CreationDate)
                    .FirstOrDefault();

                if (requestToHandle == null)
                {
                    return;
                }

                var availableAgent = this.userRepository
                    .AsQueryable()
                    .Include(x => x.Handled)
                    .Where(x => x.IsAgent
                        && x.IsOnline
                        && x.HandlingDomain == requestToHandle.Requester.SiteDomain
                        && !x.Handled.Any(y => y.State == RequestState.InProgress || y.State == RequestState.WaitingForAgentReponse))
                    .FirstOrDefault();

                if (availableAgent == null)
                {
                    return;
                }

                requestToHandle.AgentId = availableAgent.UserId;
                requestToHandle.Agent = availableAgent;
                requestToHandle.State = RequestState.WaitingForAgentReponse;

                this.requestRepository.Update(requestToHandle);

                this.communicationService.SendMessageToUserAsync(availableAgent, 
                    $"New Request from {requestToHandle.Requester.ChannelId}. Created on {requestToHandle.CreationDate.ToString()}", 
                    new List<Attachment> { this.cardService.CreateAcceptOrOfflineCard() });
            }
        }
    }
}
