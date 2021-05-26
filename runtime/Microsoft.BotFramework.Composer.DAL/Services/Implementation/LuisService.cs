using CivicCommunicator.Services.Abstraction;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using System;
using System.Linq;
using System.Threading;

namespace CivicCommunicator.Services.Implementation
{
    public class LuisService : ILuisService
    {
        private readonly LuisRecognizer luisRecognizer;

        public LuisService(LuisRecognizer luisRecognizer)
        {
            this.luisRecognizer = luisRecognizer;
        }

        public bool WantToTalkWithHuman(ITurnContext context, CancellationToken cancellationToken)
        {
            var result = this.luisRecognizer.RecognizeAsync<CivicIntentModel>(context, cancellationToken).Result;
            return result.Intents.Any(x => x.Key == CivicIntentModel.Intent.TalkToHuman_Intent && x.Value.Score > 0.9);
        }
    }
}
