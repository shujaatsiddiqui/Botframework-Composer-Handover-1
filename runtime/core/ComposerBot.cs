// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using CivicCommunicator.DataAccess.DataModel;
using CivicCommunicator.DataAccess.DataModel.Models;
using CivicCommunicator.DataAccess.Repository.Implementation;
using CivicCommunicator.Services.Abstraction;
using CivicCommunicator.Services.Implementation;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.Intermediator;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.Results;

namespace Microsoft.BotFramework.Composer.Core
{
    public class ComposerBot : ActivityHandler
    {
        private readonly ResourceExplorer resourceExplorer;
        private readonly UserState userState;
        private DialogManager dialogManager;
        private readonly IUserService userService;
        private readonly ConversationState conversationState;
        private readonly IStatePropertyAccessor<DialogState> dialogState;
        private readonly string rootDialogFile;
        private readonly IBotTelemetryClient telemetryClient;
        private readonly string defaultLocale;
        private readonly bool removeRecipientMention;
        private readonly MessageRouter messageRouter;
        private readonly MessageRouterResultHandler messageRouterResultHandler;

        public ComposerBot(IUserService userService, ConversationState conversationState, UserState userState, ResourceExplorer resourceExplorer, BotFrameworkClient skillClient, SkillConversationIdFactoryBase conversationIdFactory, MessageRouter messageRouter, MessageRouterResultHandler messageRouterResultHandler, IBotTelemetryClient telemetryClient, string rootDialog, string defaultLocale, bool removeRecipientMention = false)
        {
            this.userService = userService;
            this.conversationState = conversationState;
            this.userState = userState;
            this.dialogState = conversationState.CreateProperty<DialogState>("DialogState");
            this.resourceExplorer = resourceExplorer;
            this.rootDialogFile = rootDialog;
            this.defaultLocale = defaultLocale;
            this.telemetryClient = telemetryClient;
            this.removeRecipientMention = removeRecipientMention;
            this.messageRouter = messageRouter;
            this.messageRouterResultHandler = messageRouterResultHandler;

            LoadRootDialogAsync();
            this.dialogManager.InitialTurnState.Set(skillClient);
            this.dialogManager.InitialTurnState.Set(conversationIdFactory);
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.removeRecipientMention && turnContext?.Activity?.Type == "message")
            {
                turnContext.Activity.RemoveRecipientMention();
            }

            messageRouter.StoreConversationReferences(turnContext?.Activity);


            await this.dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
            await base.OnTurnAsync(turnContext, cancellationToken);
            await this.conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await this.userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private void LoadRootDialogAsync()
        {
            var rootFile = resourceExplorer.GetResource(rootDialogFile);
            var rootDialog = resourceExplorer.LoadType<Dialog>(rootFile);
            this.dialogManager = new DialogManager(rootDialog)
                                .UseResourceExplorer(resourceExplorer)
                                .UseLanguageGeneration()
                                .UseLanguagePolicy(new LanguagePolicy(defaultLocale));

            if (telemetryClient != null)
            {
                dialogManager.UseTelemetry(this.telemetryClient);
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> context, CancellationToken cancellationToken)
        {
            var user = this.userService.GetUserModel(context);
            if (user == null)
            {
                user = this.userService.RegisterUser(context);
            }
            else
            {
                this.userService.TryUpdate(user, context);
            }


            new MessageService().StoreTheMessage(user, context.Activity.Text);

            //var aboutCivic = this.actionsService.HandleAction(context);
            //if (aboutCivic != null)
            //{
            //    await context.SendActivityAsync(aboutCivic);
            //    return;
            //}

            //var commandResult = this.commandHandlingService.HandleCommand(context, cancellationToken);
            //if (commandResult != null)
            //{
            //    this.StoreBotReply(commandResult, user.UserId);
            //    await context.SendActivityAsync(commandResult);
            //    return;
            //}

            //if (this.RunHandOff(user, context))
            //{
            //    return;
            //}


            //var qnaResult = this.ProcessWithQna(context);
            //if (qnaResult != null)
            //{
            //    this.StoreBotReply(qnaResult, user.UserId);
            //    await context.SendActivityAsync(qnaResult);
            //    return;
            //}

            //var message = Activity.CreateMessageActivity();
            //message.Text = "Sorry, I did not understand your question. Consider rephrasing your question or if you would like to contact a customer agent, just ask!";
            //this.StoreBotReply(message, user.UserId);
            //await context.SendActivityAsync("");
        }

        

        //protected override async Task OnConversationUpdateActivityAsync
        //    (ITurnContext<IConversationUpdateActivity> turnContext,
        //    CancellationToken cancellationToken)
        //{

        //    if (turnContext.Activity.From.Role != null || turnContext.Activity.From.Name == "Customer")
        //    {
        //        var user = this.userService.GetUserModel(turnContext);
        //        if (user == null)
        //        {
        //            this.userService.RegisterUser(turnContext);
        //        }
        //        else
        //        {
        //            this.userService.TryUpdate(user, turnContext);
        //        }
        //        return;
        //    }

        //    var replyActivity = Activity.CreateMessageActivity();

        //    replyActivity.Attachments = new List<Attachment>() { this.cardService.CreateAdaptiveCardAttachment() };
        //    await turnContext.SendActivityAsync(replyActivity);
        //}
    }
}
