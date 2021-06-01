﻿using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Abstraction;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Implementation;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.BotFramework.Composer.Core
{
    public class Helper
    {
        public static void StoreBotReply(IRepository<BotReply> botReplyRepo, IUserService userService, IActivity context, DialogContext dc)
        {
            var user = userService.GetUserModel(dc.Context);
            if (user != null && context != null)
            {
                var mes = context.AsMessageActivity();
                if (mes == null)
                    return;

                if (string.IsNullOrEmpty(mes.Text))
                    return;

                botReplyRepo.Add(new BotReply
                {
                    Text = mes.Text,
                    CreationDate = DateTime.Now,
                    ToId = user.UserId
                });
            }
        }
    }
}
