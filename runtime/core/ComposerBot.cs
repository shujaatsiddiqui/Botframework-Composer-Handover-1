// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFramework.Composer.Core.Settings;
using Microsoft.BotFramework.Composer.DAL;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Abstraction;
using Microsoft.BotFramework.Composer.DAL.Implementation;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
using Microsoft.BotFramework.Composer.Intermediator;
using Microsoft.Extensions.Configuration;
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
        private readonly IMessageService messageService;
        private readonly ConversationState conversationState;
        private readonly IStatePropertyAccessor<DialogState> dialogState;
        private readonly string rootDialogFile;
        private readonly IBotTelemetryClient telemetryClient;
        private readonly string defaultLocale;
        private readonly bool removeRecipientMention;
        private readonly MessageRouter messageRouter;
        private readonly MessageRouterResultHandler messageRouterResultHandler;

        public ComposerBot(IUserService userService,
            IMessageService messageService,
            ConversationState conversationState, UserState userState, ResourceExplorer resourceExplorer, BotFrameworkClient skillClient, SkillConversationIdFactoryBase conversationIdFactory, MessageRouter messageRouter, MessageRouterResultHandler messageRouterResultHandler, IBotTelemetryClient telemetryClient, string rootDialog, string defaultLocale, bool removeRecipientMention = false)
        {
            this.userService = userService;
            this.messageService = messageService;
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
            var user = this.userService.GetUserModel(turnContext);
            if (user == null)
            {
                user = this.userService.RegisterUser(turnContext);
            }
            else
            {
                this.userService.TryUpdate(user, turnContext);
            }

            var conversationStateAccessors = conversationState.CreateProperty<ConversationFlow>(nameof(ConversationFlow));
            var flow = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationFlow(), cancellationToken);

            var userStateAccessors = userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var profile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile(), cancellationToken);

            if (this.removeRecipientMention && turnContext?.Activity?.Type == "message")
            {
                turnContext.Activity.RemoveRecipientMention();
            }

            messageRouter.StoreConversationReferences(turnContext?.Activity);
            if (flow.LastQuestionAsked == ConversationFlow.Question.Stop)
                await this.dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
            await FillOutUserProfileAsync(user, flow, profile, turnContext, cancellationToken);
            //await base.OnTurnAsync(turnContext, cancellationToken);
            await this.conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await this.userState.SaveChangesAsync(turnContext, false, cancellationToken);
            if (turnContext.Activity.Type.Equals("Message", StringComparison.InvariantCultureIgnoreCase))
                messageService.StoreTheMessage(user, turnContext.Activity.Text);
        }

        private async Task FillOutUserProfileAsync(User userObj, ConversationFlow flow, UserProfile profile, ITurnContext turnContext, CancellationToken cancellationToken)
        {

            var input = turnContext.Activity.Text?.Trim();
            string message;

            switch (flow.LastQuestionAsked)
            {
                case ConversationFlow.Question.Start:
                    await turnContext.SendActivityAsync("Demo 002 Hi my name is BOT. I am here for your assistance. May I have your name?", null, null, cancellationToken);
                    flow.LastQuestionAsked = ConversationFlow.Question.Name;
                    break;
                case ConversationFlow.Question.Name:
                    if (ValidateName(input, out var name, out message))
                    {
                        profile.Name = name;
                        userObj.Name = name;
                        await turnContext.SendActivityAsync($"Hi {profile.Name}. How may I help you today?", null, null, cancellationToken);

                        this.userService.TryUpdate(userObj, turnContext);
                        flow.LastQuestionAsked = ConversationFlow.Question.Stop;
                        break;
                    }
                    else
                    {
                        await turnContext.SendActivityAsync(message ?? "I'm sorry, I didn't understand that.", null, null, cancellationToken);
                        break;
                    }
            }
        }

        private static bool ValidateName(string input, out string name, out string message)
        {
            name = null;
            message = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                message = "Please enter a name that contains at least one character.";
            }
            else
            {
                name = input.Trim();
            }

            return message is null;
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

            //if (string.IsNullOrEmpty(user.Name))
            //{
            //    // First time around this is set to false, so we will prompt user for name.
            //    if (!string.IsNullOrEmpty(context.Activity.Text?.Trim()))
            //    {
            //        // Set the name to what the user provided.
            //        user.Name = context.Activity.Text?.Trim();

            //        // Acknowledge that we got their name.
            //        await context.SendActivityAsync($"Hi {user.Name}. How may I help you today?");
            //        this.userService.TryUpdate(user, context);
            //    }
            //    else
            //    {
            //        // Prompt the user for their name.
            //        await context.SendActivityAsync($"What is your name?");
            //    }
            //}

            this.messageService.StoreTheMessage(user, context.Activity.Text);

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

        private string GetRootDialog(string folderPath)
        {
            var dir = new DirectoryInfo(folderPath);
            foreach (var f in dir.GetFiles())
            {
                if (f.Extension == ".dialog")
                {
                    return f.Name;
                }
            }

            throw new Exception($"Can't locate root dialog in {dir.FullName}");
        }
    }
}
