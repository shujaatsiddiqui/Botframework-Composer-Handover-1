using CivicCommunicator.DataAccess.DataModel;
using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.DataAccess.Repository.Implementation;
using CivicCommunicator.Services.Abstraction;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.BotFramework.Composer.Core
{
    public class Helper
    {
        public static void StoreBotReply(IUserService userService, IActivity context, DialogContext dc)
        {
            var user = userService.GetUserModel(dc.Context);
            if (user == null)
            {
                user = userService.RegisterUser(dc.Context);
            }
            else
            {
                userService.TryUpdate(user, dc.Context);
            }

            var mes = context.AsMessageActivity();
            if (mes == null)
                return;

            if (string.IsNullOrEmpty(mes.Text))
                return;

            new Repository<BotReply>(new BotDbContext()).Add(new BotReply
            {
                Text = mes.Text,
                CreationDate = DateTime.Now,
                ToId = user.UserId
            });
        }
    }
}
