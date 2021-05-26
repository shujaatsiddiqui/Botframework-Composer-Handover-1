using CivicCommunicator.Commands;
using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.DataAccess.Repository.Abstraction;
using CivicCommunicator.Helpers;
using CivicCommunicator.Services.Abstraction;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CivicCommunicator.Services.Implementation
{
    public class CommandHandlingService : ICommandHandlingService
    {
        private const string COMMAND_PREFIX = "/";

        private Dictionary<BotCommand, BotCommandLogic> ruleDictionary;

        private readonly IRepository<User> userRepository;
        private readonly IRepository<ConversationRequest> requestRepository;

        private readonly IUserService userService;
        private readonly ILuisService luisService;
        private readonly ICommunicationService communicationService;
        private readonly IOrchestrizationService orchestrizationService;

        private bool isCommandBySimpleRuling(string message, BotCommand command)
            => message.Contains($"{COMMAND_PREFIX}{command.ToString().ToLower()}");

        private void initializeRuleDictionary()
        {
            this.ruleDictionary = new Dictionary<BotCommand, BotCommandLogic>
            {
                { BotCommand.TalkToHuman,
                    new BotCommandLogic
                    {
                        IsCommand = (c, can) => this.luisService.WantToTalkWithHuman(c, can),
                        Validate = x => this.ValidateTalkToHumanCommand(x),
                        HandleCommand = (x, parameter) => this.HandleTalkToHumanCommand(x),
                        PostLogic = x => this.orchestrizationService.NavigateRequestToAgent()
                    }},
                { BotCommand.RegisterAsAgent,
                new BotCommandLogic{
                    IsCommand = (c, can) => this.isCommandBySimpleRuling(c.Activity.Text, BotCommand.RegisterAsAgent),
                    ParseParameters = x => this.ParseDomainParameter(x),
                    ValidateParameters = x => this.ValidateDomainParameter(x),
                    Validate = x => this.ValidateAgentRoleChange(x, becomeAgent:true),
                    HandleCommand = (x, parameter) => this.HandleAgentRoleChange(x, parameter, becomeAgent: true)
                }},
                { BotCommand.ChangeDomain,
                new BotCommandLogic{
                    IsCommand = (c, can) => this.isCommandBySimpleRuling(c.Activity.Text, BotCommand.ChangeDomain),
                    ParseParameters = x => this.ParseDomainParameter(x),
                    ValidateParameters = x => this.ValidateDomainParameter(x),
                    Validate = x => this.ValidateDomainChange(x),
                    HandleCommand = (x, parameter) => this.HandleDomainChange(x, parameter)
                }},
                { BotCommand.UnregisterAsAgent,
                new BotCommandLogic{
                    IsCommand = (c, can) => this.isCommandBySimpleRuling(c.Activity.Text, BotCommand.UnregisterAsAgent),
                    Validate = x => this.ValidateAgentRoleChange(x, becomeAgent:false),
                    HandleCommand = (x, parameter) => this.HandleAgentRoleChange(x, parameter, becomeAgent: false)
                }},
                { BotCommand.FinishConversation,
                new BotCommandLogic
                {
                    IsCommand = (c, can) => this.isCommandBySimpleRuling(c.Activity.Text, BotCommand.FinishConversation),
                    Validate = x => this.ValidateConversationFinish(x),
                    HandleCommand = (x, parameter) => this.HandleConversationFinish(x),
                    PostLogic = x => this.orchestrizationService.NavigateRequestToAgent()
                }},
                { BotCommand.GoOnline,
                new BotCommandLogic
                {
                    IsCommand = (c, can) => this.isCommandBySimpleRuling(c.Activity.Text, BotCommand.GoOnline),
                    Validate = x => this.ValidateGoOnline(x),
                    HandleCommand = (x, parameter) => this.HandleGoOnline(x),
                    PostLogic = x => this.orchestrizationService.NavigateRequestToAgent()
                }}
            };
        }

        public CommandHandlingService(
            IRepository<User> userRepository,
            IRepository<ConversationRequest> requestRepository,
            IUserService userService,
            ILuisService luisService,
            ICommunicationService communicationService,
            IOrchestrizationService orchestrizationService)
        {
            this.userRepository = userRepository;
            this.requestRepository = requestRepository;
            this.userService = userService;
            this.luisService = luisService;
            this.communicationService = communicationService;
            this.orchestrizationService = orchestrizationService;
            this.initializeRuleDictionary();
        }

        public IActivity HandleCommand(ITurnContext context, CancellationToken cancellationToken)
        {
            var commandLogic = this.ruleDictionary.FirstOrDefault(x => x.Value.IsCommand(context, cancellationToken)).Value;
            var validationResult = commandLogic?.Validate(context);
            if(validationResult != null)
            {
                return validationResult;
            }
            var parameters = commandLogic?.ParseParameters?.Invoke(context);
            var parametersValidationResult = commandLogic?.ValidateParameters?.Invoke(parameters);
            if (parametersValidationResult != null)
            {
                return parametersValidationResult;
            }
            var result = commandLogic?.HandleCommand(context, parameters);
            commandLogic?.PostLogic?.Invoke(context);
            return result;
        }

        private IActivity ValidateTalkToHumanCommand(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);

            if (user.IsAgent)
            {
                var returnMessage = "Agents cannot create requests";
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = returnMessage;

                return returnActivity;
            }

            if(!this.userRepository.AsQueryable().Any(x => x.IsOnline && x.HandlingDomain == user.SiteDomain))
            {
                var returnMessage = "The request cannot be created as there are no customer agents online.";
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = returnMessage;

                return returnActivity;
            }

            if (this.requestRepository
                .AsQueryable()
                .Any(x => x.RequesterId == user.UserId && (x.State == RequestState.InProgress || x.State == RequestState.Pending)))
            {
                var returnMessage = "The request cannot be created, because there are unfinished requests on your side";
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = returnMessage;

                return returnActivity;
            }

            return null;
        }

        private IActivity ValidateAgentRoleChange(ITurnContext turnContext, bool becomeAgent)
        {
            var user = this.userService.GetUserModel(turnContext);
            
            if (user.IsAgent == becomeAgent)
            {
               var message = "Your status is the same.";
               var returnActivity = Activity.CreateMessageActivity();
               returnActivity.Text = message;

               return returnActivity;
            }

            return null;
        }

        private IActivity ValidateDomainChange(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);

            if (!user.IsAgent)
            {
                var message = "You are not authorized";
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = message;

                return returnActivity;
            }

            return null;
        }

        private IActivity ValidateUserRequestToAgent(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);

            if (!user.IsAgent)
            {
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = $"You are not authorized for the command";

                return returnActivity;
            }

            var request = this.requestRepository.AsQueryable()
                .Where(x => x.State == RequestState.Pending)
                .OrderByDescending(x => x.CreationDate)
                .FirstOrDefault();

            if (request == null)
            {
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = $"There is no available request.";

                return returnActivity;
            }

            return null;
        }

        private IActivity ValidateConversationFinish(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);

            var request = this.requestRepository.AsQueryable()
               .Where(x => x.State == RequestState.InProgress)
               .OrderByDescending(x => x.CreationDate)
               .FirstOrDefault();

            if(request == null)
            {
                var message = "You have no ongoing conversation.";
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = message;
                return returnActivity;
            }

            return null;
        }

        private IActivity ValidateGoOnline(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);

            if (!user.IsAgent)
            {
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = $"You are not authorized. Please register yourself as agent first.";

                return returnActivity;
            }

            if (user.IsOnline)
            {
                var returnActivity = Activity.CreateMessageActivity();
                returnActivity.Text = $"You are already online";

                return returnActivity;
            }

            return null;
        }

        private object ParseDomainParameter(ITurnContext turnContext)
        {
            var text = turnContext.Activity.Text.Trim(' ', '\r', '\n');
            var splitted = text.Split(' ');
            return splitted.Length < 2 ? null : splitted[1];
        }

        private IActivity ValidateDomainParameter(object parameters)
        {
            if(parameters == null)
            {
                return null;
            }

            if (UrlHelper.IsUrl((string)parameters))
            {
                return null;
            }

            var returnActivity = Activity.CreateMessageActivity();
            returnActivity.Text = "Invalid parameters";

            return returnActivity;
        }

        private IActivity HandleTalkToHumanCommand(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);

            var returnMessage = "The request has been created. You will be notified when the customer service agent accepts your request.";

            this.requestRepository.Add(new ConversationRequest
            {
                CreationDate = DateTime.Now,
                RequesterId = user.UserId,
                Requester = user,
                State = RequestState.Pending
            });

            var returnActivity = Activity.CreateMessageActivity();
            returnActivity.Text = returnMessage;

            return returnActivity;
        }

        private IActivity HandleDomainChange(ITurnContext turnContext, object parameter)
        {
            var user = this.userService.GetUserModel(turnContext);
            
            user.HandlingDomain = (string)parameter;
            
            this.userRepository.Update(user);

            var returnActivity = Activity.CreateMessageActivity();
            var message = "The domain has been changed";
            returnActivity.Text = message;

            return returnActivity;
        }

        private IActivity HandleAgentRoleChange(ITurnContext turnContext, object domainParameter, bool becomeAgent)
        {
            var user = this.userService.GetUserModel(turnContext);

            user.IsAgent = becomeAgent;
            if (becomeAgent && domainParameter != null)
            {
                user.HandlingDomain = (string)domainParameter;
            }

            if (!becomeAgent)
            {
                user.IsOnline = false;
                user.HandlingDomain = null;
            }

            this.userRepository.Update(user);

            var returnActivity = Activity.CreateMessageActivity();
            var message = "Your status is modified";
            returnActivity.Text = message;

            return returnActivity;
        }

        private IActivity HandleUserRequestToAgent(ITurnContext turnContext)
        {
            var agent = this.userService.GetUserModel(turnContext);

            var request = this.requestRepository.AsQueryable()
                .Where(x => x.State == RequestState.Pending)
                .OrderByDescending(x => x.CreationDate)
                .FirstOrDefault();

            request.AgentId = agent.UserId;
            request.Agent = agent;

            request.State = RequestState.InProgress;

            this.requestRepository.Update(request);

            var requestor = this.userRepository.Get(request.RequesterId);
            this.communicationService.SendMessageToUserAsync(requestor, "Your request was accepted");

            var returnActivity = Activity.CreateMessageActivity();
            returnActivity.Text = $"You are now messaging with {requestor.Name}";

            return returnActivity;
        }

        private IActivity HandleConversationFinish(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);
            var conversationEndedMessage = "Conversation is finished";

            var request = this.requestRepository.AsQueryable()
               .Where(x => x.State == RequestState.InProgress)
               .FirstOrDefault(x => x.AgentId == user.UserId || x.RequesterId == user.UserId);

            request.State = RequestState.Finished;

            this.requestRepository.Update(request);

            var toSendId = request.RequesterId;
            if(toSendId == user.UserId)
            {
                toSendId = request.AgentId.Value;
            }

            this.communicationService.SendMessageToUserAsync(this.userRepository.Get(toSendId), conversationEndedMessage);

            var returnActivity = Activity.CreateMessageActivity();
            returnActivity.Text = conversationEndedMessage;

            return returnActivity;
        }

        private IActivity HandleGoOnline(ITurnContext turnContext)
        {
            var user = this.userService.GetUserModel(turnContext);

            user.IsOnline = true;
            this.userRepository.Update(user);

            var returnActivity = Activity.CreateMessageActivity();
            returnActivity.Text = "You are online";

            return returnActivity;
        }
    }
}
