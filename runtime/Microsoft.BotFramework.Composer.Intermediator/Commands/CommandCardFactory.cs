using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
using Microsoft.BotFramework.Composer.Intermediator.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.Models;

namespace Microsoft.BotFramework.Composer.Intermediator
{
    public class CommandCardFactory
    {
        /// <summary>
        /// Creates a large connection request card.
        /// </summary>
        /// <param name="connectionRequest">The connection request.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A newly created request card.</returns>
        public static HeroCard CreateConnectionRequestCard(
            ConnectionRequest connectionRequest, string botName = null, User userProfile = null)
        {

            if (connectionRequest == null || connectionRequest.Requestor == null)
            {
                throw new ArgumentNullException("The connection request or the conversation reference of the requestor is null");
            }

            ChannelAccount requestorChannelAccount =
                RoutingDataManager.GetChannelAccount(connectionRequest.Requestor);

            if (requestorChannelAccount == null)
            {
                throw new ArgumentNullException("The channel account of the requestor is null");
            }

            string requestorChannelAccountName = string.IsNullOrEmpty(requestorChannelAccount.Name)
                ? StringConstants.NoUserNamePlaceholder : requestorChannelAccount.Name;
            string requestorChannelId =
                CultureInfo.CurrentCulture.TextInfo.ToTitleCase(connectionRequest.Requestor.ChannelId);

            var acceptValue = $"Request Accepted From User With Dynamic Id : {connectionRequest.Requestor.Conversation?.Id} @ {requestorChannelAccount?.Id}";
            var rejectValue = $"Request Rejected From User With Dynamic Id : {connectionRequest.Requestor.Conversation?.Id} @ {requestorChannelAccount?.Id}";

            HeroCard card = new HeroCard()
            {
                Title = Strings.ConnectionRequestTitle,
                Subtitle = string.Format(Strings.RequestorDetailsTitle, userProfile?.Name),
                // Text = string.Format(Strings.AcceptRejectConnectionHint, acceptValue, rejectValue),

                Buttons = new List<CardAction>()
                {
                    new CardAction()
                    {
                        Title = Strings.AcceptRequestButtonLabel,
                        Type = ActionTypes.ImBack,
                        Value = acceptValue
                    },
                    new CardAction()
                    {
                        Title = Strings.RejectRequestButtonLabel,
                        Type = ActionTypes.ImBack,
                        Value = rejectValue
                    }
                }
            };

            return card;
        }

        /// <summary>
        /// Creates multiple large connection request cards.
        /// </summary>
        /// <param name="connectionRequests">The connection requests.</param>
        /// <param name="botName">The name of the bot (optional).</param>
        /// <returns>A list of request cards as attachments.</returns>
        public static IList<Attachment> CreateMultipleConnectionRequestCards(
            IList<ConnectionRequest> connectionRequests, IUserService userService, string botName = null)
        {
            IList<Attachment> attachments = new List<Attachment>();

            foreach (ConnectionRequest connectionRequest in connectionRequests)
            {
                var user = userService.GetUserModelFromChatId(connectionRequest.Requestor.User.Id);
                attachments.Add(CreateConnectionRequestCard(connectionRequest, botName, user).ToAttachment());
            }

            return attachments;
        }
    }
}
