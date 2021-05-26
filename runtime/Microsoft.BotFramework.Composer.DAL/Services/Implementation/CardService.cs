using CivicCommunicator.Commands;
using CivicCommunicator.Services.Abstraction;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace CivicCommunicator.Services.Implementation
{
    public class CardService : ICardService
    {
        public Attachment CreateAdaptiveCardAttachment()
        {
            var cardResourcePath = "CivicCommunicator.Cards.welcomeCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }

        public Attachment CreateVideoCard()
        {
            var videoCard = new VideoCard
            {
                Title = "About Civic",
                Media = new List<MediaUrl>
                {
                    new MediaUrl()
                    {
                        Url = "https://www.youtube.com/watch?v=MlJtRMZQdz0",
                    },
                },
            };

            return videoCard.ToAttachment();
        }

        public Attachment CreateAcceptOrOfflineCard()
        {
            var cardResourcePath = "CivicCommunicator.Cards.acceptOrOffline.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
    }
}
