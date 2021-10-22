using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.BotFramework.Composer.DAL.Implementation;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
using Microsoft.BotFramework.Composer.Intermediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.Models;

namespace Microsoft.BotFramework.Composer.CustomAction.Action
{
    public class GetRequestsDialog : Dialog
    {
        private readonly MessageRouter _messageRouter;
        private readonly ILogger<GetRequestsDialog> _logger;
        private readonly IUserService userService;

        [JsonConstructor]
        public GetRequestsDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            this.userService = Configuration.ServiceProvider.GetRequiredService<IUserService>();

            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);

            // load dependency service
            _messageRouter = Configuration.MessageRouter;
            _logger = Configuration.LoggerFactory.CreateLogger<GetRequestsDialog>();
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(GetRequestsDialog);

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
            Activity replyActivity = null;
            try
            {
                var activity = dc.Context.Activity;

                IList<ConnectionRequest> connectionRequests =
                            _messageRouter.RoutingDataManager.GetConnectionRequests();

                replyActivity = activity.CreateReply();

                if (connectionRequests.Count == 0)
                {
                    replyActivity.Text = "No pending requests";
                }
                else
                {
                    replyActivity.Attachments = CommandCardFactory.CreateMultipleConnectionRequestCards(
                        connectionRequests, userService, activity.Recipient?.Name);
                }
                List<ConversationReference> cr = new List<ConversationReference>();
                foreach (var item in connectionRequests)
                {
                    item.Requestor.User.Id = null;
                    item.Requestor.Conversation.Id = null;
                    item.Requestor.ServiceUrl = null;
                    item.ConnectionRequestTime = DateTime.Now;
                }
                //replyActivity.ChannelData = dc.Context.Activity.GetChannelData<TeamsChannelData>();
                replyActivity.ChannelData = JsonConvert.SerializeObject(connectionRequests, new NoColonIsoDateTimeConverter());

                replyActivity.Text = "CD | " + replyActivity.ChannelData;
            }
            catch (Exception ex)
            {
                replyActivity.Text = ex.Message + " | " + ex.StackTrace;
            }

            await dc.Context.SendActivityAsync(replyActivity, cancellationToken);
            return await dc.EndDialogAsync(null, cancellationToken);
        }
    }

    public class NoColonIsoDateTimeConverter : IsoDateTimeConverter
    {
        public NoColonIsoDateTimeConverter()
        {
            DateTimeFormat = "ABYYYY";
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime)
            {
                var dateTime = (DateTime)value;
                var text = dateTime.ToString(DateTimeFormat);
                text = text.Remove(text.Length - 3, 1);
                writer.WriteValue(text);
            }
            else
            {
                throw new JsonSerializationException("Unexpected value when converting date. Expected DateTime");
            }
        }
    }
}
