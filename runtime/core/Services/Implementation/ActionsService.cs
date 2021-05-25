using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.DataAccess.Repository.Abstraction;
using CivicCommunicator.Services.Abstraction;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CivicCommunicator.Services.Implementation
{
    public class ActionsService : IActionsService
    {
        private readonly ICardService cardService;
        private readonly IUserService userService;
        private readonly IRepository<ConversationRequest> requestRepository;
        private readonly ICommunicationService communicationService;
        private readonly IRepository<User> userRepository;
        private Dictionary<string, Func<ITurnContext, IActivity>> actionHandlingDictionary;

        private void initializeDictionary()
        {
            this.actionHandlingDictionary = new Dictionary<string, Func<ITurnContext, IActivity>>
            {
                { "aboutCivic", x => this.ResolveAboutCivic(x) },
                { "acceptRequest", x => this.ResolveAcceptRequest(x) },
                { "goOffline", x => this.ResolveGoOffline(x) }
            };
        }

        private void checkIfLastOnline()
        {
            if(this.userRepository.AsQueryable().Any(x => x.IsOnline))
                return;

            var toClose = this.requestRepository
                .AsQueryable()
                .Include(x => x.Requester)
                .Where(x => x.State == RequestState.Pending).ToList();

            foreach(var request in toClose)
            {
                request.State = RequestState.Finished;
                this.requestRepository.Update(request);
                this.communicationService.SendMessageToUserAsync(request.Requester, "Sorry, but all the agents are offline. Try to request conversation later");
            }
        }

        private IActivity ResolveGoOffline(ITurnContext context)
        {
            var returnActivity = Activity.CreateMessageActivity();

            var user = this.userService.GetUserModel(context);

            if (!user.IsOnline || !user.IsAgent)
            {
                returnActivity.Text = $"You are not online or not an agent";

                return returnActivity;
            }

            var requests = this.requestRepository
                .AsQueryable()
                .Include(x => x.Requester)
                .Where(x => x.AgentId == user.UserId);

            if (requests.Any(x => x.State == RequestState.InProgress))
            {
                returnActivity.Text = $"You have a request in progres. Please, complete the request first.";

                return returnActivity;
            }

            var toAccept = requests.FirstOrDefault(x => x.State == RequestState.WaitingForAgentReponse);

            if (toAccept != null)
            {
                toAccept.State = RequestState.Pending;
                toAccept.AgentId = null;
                toAccept.Agent = null;

                this.requestRepository.Update(toAccept);
            }

            user.IsOnline = false;
            this.userRepository.Update(user);
            this.checkIfLastOnline();

            returnActivity.Text = $"You're now offline. Type '/goonline' to receive more requests";

            return returnActivity;
        }

        private IActivity ResolveAcceptRequest(ITurnContext context)
        {
            var returnActivity = Activity.CreateMessageActivity();

            var user = this.userService.GetUserModel(context);

            if (!user.IsOnline || !user.IsAgent)
            {
                returnActivity.Text = $"You are not online or not an agent";

                return returnActivity;
            }

            var requests = this.requestRepository
                .AsQueryable()
                .Include(x => x.Requester)
                .Where(x => x.AgentId == user.UserId);

            if (requests.Any(x => x.State == RequestState.InProgress))
            {
                returnActivity.Text = $"You have unfinished requests";

                return returnActivity;
            }

            var toAccept = requests.FirstOrDefault(x => x.State == RequestState.WaitingForAgentReponse);

            if(toAccept == null)
            {
                returnActivity.Text = $"No requests to accept";

                return returnActivity;
            }

            toAccept.State = RequestState.InProgress;

            this.requestRepository.Update(toAccept);

            this.communicationService.SendMessageToUserAsync(toAccept.Requester, "Your request was accepted");

            returnActivity.Text = $"You're now messaging with {toAccept.Requester.Name}";

            return returnActivity;
        }

        private IActivity ResolveAboutCivic(ITurnContext x)
        {
            var replyActivity = Activity.CreateMessageActivity();
            replyActivity.Attachments = new List<Attachment>() { this.cardService.CreateVideoCard() };

            return replyActivity;
        }

        public ActionsService(ICardService cardService, 
            IUserService userService, 
            IRepository<ConversationRequest> requestRepository,
            ICommunicationService communicationService,
            IRepository<User> userRepository)
        {
            this.cardService = cardService;
            this.userService = userService;
            this.requestRepository = requestRepository;
            this.communicationService = communicationService;
            this.userRepository = userRepository;
            this.initializeDictionary();
        }


        public IActivity HandleAction(ITurnContext context)
        {
            if (context.Activity.Value != null)
            {
                var valueToken = JObject.Parse(context.Activity.Value.ToString());
                var actionValue = valueToken.SelectToken("action")?.ToString();
                if (string.IsNullOrEmpty(actionValue))
                {
                    return null;
                }
                return this.actionHandlingDictionary[actionValue]?.Invoke(context);
            }

            return null;
        }
    }
}
