using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using AdaptiveExpressions.Properties;
using Microsoft.BotFramework.Composer.Intermediator;
using Microsoft.BotFramework.Composer.Intermediator.Resources;
using Microsoft.BotFramework.Composer.Core;
using System;
using Microsoft.BotFramework.Composer.DAL.Implementation;

namespace Microsoft.BotFramework.Composer.CustomAction.Action
{
    public class HumanDialog : Dialog
    {
        private readonly MessageRouter _messageRouter;
        private readonly MessageRouterResultHandler _messageRouterResultHandler;
        private readonly UserService userService;

        [JsonConstructor]
        public HumanDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
           : base()
        {
            this.userService = new UserService();
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);

            // load dependency service
            _messageRouter = Configuration.MessageRouter;
            _messageRouterResultHandler = Configuration.MessageRouterResultHandler;
        }


        [JsonProperty("$kind")]
        public const string Kind = nameof(HumanDialog);

        /// <summary>
        /// Gets or sets caller's memory path to store the result of this step in (ex: conversation.area).
        /// </summary>
        /// <value>
        /// Caller's memory path to store the result of this step in (ex: conversation.area).
        /// </value>
        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var user = this.userService.GetUserModel(dc.Context);
            var activity = dc.Context.Activity;

            var messageRouterResult = _messageRouter.CreateConnectionRequest(
                MessageRouter.CreateSenderConversationReference(activity));

            await _messageRouterResultHandler.HandleResultAsync(messageRouterResult, user, userService: userService);

            var replyActivity = activity.CreateReply(Strings.NotifyClientWaitForRequestHandling);
            await dc.Context.SendActivityAsync(replyActivity, cancellationToken);

            Helper.StoreBotReply(this.userService, replyActivity, dc);
            //new Repository<ConversationRequest>(new BotDbContext())
            //    .Add(new ConversationRequest
            //    {
            //        CreationDate = DateTime.Now,
            //        RequesterId = user.UserId,
            //        Requester = user
            //    });
            //return EndOfTurn;
            return await dc.EndDialogAsync(messageRouterResult, cancellationToken);
        }
    }
}
