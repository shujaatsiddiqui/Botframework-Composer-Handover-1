using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Implementation;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
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
            if (user != null)
            {
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
}
