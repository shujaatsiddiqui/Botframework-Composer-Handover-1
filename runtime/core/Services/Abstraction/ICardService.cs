using Microsoft.Bot.Schema;

namespace CivicCommunicator.Services.Abstraction
{
    public interface ICardService
    {
        Attachment CreateAdaptiveCardAttachment();

        Attachment CreateVideoCard();

        Attachment CreateAcceptOrOfflineCard();
    }
}
