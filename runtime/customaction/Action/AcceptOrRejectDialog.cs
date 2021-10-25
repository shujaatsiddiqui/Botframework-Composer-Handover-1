using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.Results;
using Microsoft.Extensions.Logging;
using Microsoft.BotFramework.Composer.Intermediator;
using Microsoft.BotFramework.Composer.Core;
using Microsoft.BotFramework.Composer.DAL.Implementation;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Implementation;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Abstraction;

namespace Microsoft.BotFramework.Composer.CustomAction.Action
{
    public class AcceptOrRejectDialog : Dialog
    {
        private readonly MessageRouter _messageRouter;
        private readonly ILogger<AcceptOrRejectDialog> _logger;
        private readonly MessageRouterResultHandler _messageRouterResultHandler;
        private readonly ConnectionRequestHandler _connectionRequestHandler;
        private readonly IUserService userService;
        private readonly IRepository<ConversationRequest> conversationStateRepo;
        private readonly IRepository<BotReply> botReplyRepo;

        [JsonConstructor]
        public AcceptOrRejectDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {

            this.userService = Configuration.ServiceProvider.GetRequiredService<IUserService>();
            conversationStateRepo = Configuration.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IRepository<ConversationRequest>>();
            this.botReplyRepo = Configuration.ServiceProvider.CreateScope().ServiceProvider.GetRequiredService<IRepository<BotReply>>();
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);

            // load dependency service
            _messageRouter = Configuration.MessageRouter;
            _logger = Configuration.LoggerFactory.CreateLogger<AcceptOrRejectDialog>();
            _messageRouterResultHandler = Configuration.MessageRouterResultHandler;
            _connectionRequestHandler = new ConnectionRequestHandler();
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(AcceptOrRejectDialog);


        [JsonProperty("conversationProperty")]
        public StringExpression ConversationProperty { get; set; }

        [JsonProperty("userIdProperty")]
        public StringExpression UserProperty { get; set; }

        [JsonProperty("acceptProperty")]
        public BoolExpression AcceptProperty { get; set; }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            try
            {

                var activity = dc.Context.Activity;
                bool success = false;

                string[] commands = dc.Context.Activity.Text.Split(':')[1].Split('@');

                var conversation = commands.Length >= 2 ? commands[0].Trim() : ""; //ConversationProperty.GetValue(dc.State);
                var userId = commands.Length >= 2 ? commands[1].Trim() : "";  // UserProperty.GetValue(dc.State);
                bool accept = AcceptProperty.GetValue(dc.State); //commands.Length >= 2 ? commands[1].Equals("\\Reject", StringComparison.InvariantCultureIgnoreCase) ? false : true : true; 

                Activity replyActivity = null;
                // sender conversation reference
                ConversationReference sender = MessageRouter.CreateSenderConversationReference(activity);

                if (_messageRouter.RoutingDataManager.IsAssociatedWithAggregation(sender))
                {
                    // The sender is associated with the aggregation and has the right to accept/reject
                    if (string.IsNullOrEmpty(conversation) && string.IsNullOrEmpty(userId))
                    {
                        replyActivity = activity.CreateReply();

                        var connectionRequests = _messageRouter.RoutingDataManager.GetConnectionRequests();

                        if (connectionRequests.Count == 0)
                        {
                            replyActivity.Text = "Strings.NoPendingRequests";
                        }
                        else
                        {
                            //replyActivity = CommandCardFactory.AddCardToActivity(
                            //    replyActivity, CommandCardFactory.CreateMultiConnectionRequestCard(
                            //        connectionRequests, doAccept, activity.Recipient?.Name));
                        }
                    }
                    else if (!string.IsNullOrEmpty(conversation) || !string.IsNullOrEmpty(userId))
                    {
                        // Try to accept the specified connection request
                        ChannelAccount requestorChannelAccount =
                            new ChannelAccount(userId);
                        ConversationAccount requestorConversationAccount =
                            new ConversationAccount(null, null, conversation);

                        _logger.LogDebug($"Accepting user {userId} @ {conversation}");
                        AbstractMessageRouterResult messageRouterResult =
                            await _connectionRequestHandler.AcceptOrRejectRequestAsync(
                                _messageRouter, _messageRouterResultHandler, sender, accept,
                                requestorChannelAccount, requestorConversationAccount);

                        User userCustomer = userService.GetUserModelFromChatId(requestorChannelAccount.Id);
                        User userAgent = userService.GetUserModelFromChatId(sender.User.Id);

                        this.conversationStateRepo.Add(new ConversationRequest
                        {
                            CreationDate = DateTime.Now,
                            RequesterId = (int)userCustomer?.UserId,
                            AgentId = (int)userAgent?.UserId,
                            State = RequestState.InProgress
                        });

                        await _messageRouterResultHandler.HandleResultAsync(messageRouterResult, userService: userService);
                        success = true;
                    }
                    else
                    {
                        replyActivity = activity.CreateReply(Intermediator.Resources.Strings.InvalidOrMissingCommandParameter);
                    }
                }

                if (!success)
                    await dc.Context.SendActivityAsync(replyActivity, cancellationToken);

                if (ResultProperty != null)
                    dc.State.SetValue(ResultProperty.GetValue(dc.State), success);

                Helper.StoreBotReply(this.botReplyRepo, this.userService, replyActivity, dc);
                return await dc.EndDialogAsync(success, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return await dc.EndDialogAsync(Intermediator.Resources.Strings.NotifyClientRequestRejected, cancellationToken);
            }
        }
    }
}
