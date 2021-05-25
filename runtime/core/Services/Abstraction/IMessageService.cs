using CivicCommunicator.DataAccess.DataModel.Models;

namespace CivicCommunicator.Services.Abstraction
{
    public interface IMessageService
    {
        void StoreTheMessage(User user, string message);
    }
}
