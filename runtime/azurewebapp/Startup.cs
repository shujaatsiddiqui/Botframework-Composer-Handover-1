// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Bot.Builder.Community.Storage.EntityFramework;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Azure.Blobs;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotFramework.Composer.Core;
using Microsoft.BotFramework.Composer.Core.Settings;

using Microsoft.BotFramework.Composer.CustomAction;
using Microsoft.BotFramework.Composer.CustomAction.Action;
using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Abstraction;
using Microsoft.BotFramework.Composer.DAL.DataAccess.Repository.Implementation;
using Microsoft.BotFramework.Composer.DAL.Implementation;
using Microsoft.BotFramework.Composer.DAL.Services.Abstraction;
using Microsoft.BotFramework.Composer.Intermediator;
using Microsoft.BotFramework.Composer.WebAppTemplates.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;

namespace Microsoft.BotFramework.Composer.WebAppTemplates
{
    public class Startup
    {
        private const string KeyAzureTableStorageConnectionString = "AzureTableStorageConnectionString";

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.HostingEnvironment = env;
            this.Configuration = configuration;
        }

        public IWebHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        public void ConfigureTranscriptLoggerMiddleware(BotFrameworkHttpAdapter adapter, BotSettings settings)
        {
            if (ConfigSectionValid(settings?.BlobStorage?.ConnectionString) && ConfigSectionValid(settings?.BlobStorage?.Container))
            {
                adapter.Use(new TranscriptLoggerMiddleware(new BlobsTranscriptStore(settings?.BlobStorage?.ConnectionString, settings?.BlobStorage?.Container)));
            }
        }

        public void ConfigureShowTypingMiddleWare(BotFrameworkAdapter adapter, BotSettings settings)
        {
            if (settings?.Feature?.UseShowTypingMiddleware == true)
            {
                adapter.Use(new ShowTypingMiddleware());
            }
        }

        public void ConfigureInspectionMiddleWare(BotFrameworkAdapter adapter, BotSettings settings, IStorage storage)
        {
            if (settings?.Feature?.UseInspectionMiddleware == true)
            {
                adapter.Use(new InspectionMiddleware(new InspectionState(storage)));
            }
        }

        public void ConfigureSetSpeakMiddleWare(BotFrameworkAdapter adapter, BotSettings settings)
        {
            if (settings?.Feature?.UseSetSpeakMiddleware == true && settings.Speech != null)
            {
                adapter.Use(new SetSpeakMiddleware(settings.Speech.VoiceFontName, settings.Speech.FallbackToTextForSpeechIfEmpty));
            }
        }

        public void ConfigureHandOff(IServiceCollection services, BotSettings settings)
        {
            services.AddSingleton<IRoutingDataStore>((sp) =>
            {
                string connectionString = settings.AzureTableStorageConnectionString ?? string.Empty;
                IRoutingDataStore routingDataStore = null;
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(nameof(Startup));

                if (string.IsNullOrEmpty(connectionString))
                {
                    logger.LogDebug($"WARNING!!! No connection string found - using {nameof(Underscore.Bot.MessageRouting.DataStore.Local.InMemoryRoutingDataStore)}");
                    routingDataStore = new Underscore.Bot.MessageRouting.DataStore.Local.InMemoryRoutingDataStore();
                }
                else
                {
                    logger.LogDebug($"Found a connection string - using {nameof(Underscore.Bot.MessageRouting.DataStore.Azure.AzureTableRoutingDataStore)}");
                    routingDataStore = new Underscore.Bot.MessageRouting.DataStore.Azure.AzureTableRoutingDataStore(
                        connectionString,
                        new Underscore.Bot.MessageRouting.Logging.ConsoleLogger(loggerFactory.CreateLogger<Underscore.Bot.MessageRouting.DataStore.Azure.AzureTableRoutingDataStore>()));
                }

                return routingDataStore;
            });

            services.AddSingleton<MessageRouter>((sp) =>
            {
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var routingDataStore = sp.GetService<IRoutingDataStore>();

                var messageRouter = new MessageRouter(
                    routingDataStore,
                    new MicrosoftAppCredentials(settings.MicrosoftAppId, settings.MicrosoftAppPassword),
                    logger: new Underscore.Bot.MessageRouting.Logging.ConsoleLogger(loggerFactory.CreateLogger<MessageRouter>()));

                Microsoft.BotFramework.Composer.CustomAction.Configuration.MessageRouter = messageRouter;
                return messageRouter;
            });

            services.AddSingleton<MessageRouterResultHandler>((sp) =>
            {
                var messageRouter = sp.GetService<MessageRouter>();
                var loggerFactory = sp.GetService<ILoggerFactory>();

                var handler = new MessageRouterResultHandler(messageRouter, loggerFactory.CreateLogger<MessageRouterResultHandler>());

                CustomAction.Configuration.MessageRouterResultHandler = handler;
                return handler;
            });

            Microsoft.BotFramework.Composer.CustomAction.Configuration.Settings = Configuration;
        }

        public IStorage ConfigureStorage(BotSettings settings)
        {
            return new EntityFrameworkStorage(settings.ConnectionString);

            //if (string.IsNullOrEmpty(settings?.CosmosDb?.ContainerId))
            //{
            //    if (!string.IsNullOrEmpty(this.Configuration["cosmosdb:collectionId"]))
            //    {
            //        settings.CosmosDb.ContainerId = this.Configuration["cosmosdb:collectionId"];
            //    }
            //}

            //IStorage storage;
            //if (ConfigSectionValid(settings?.CosmosDb?.AuthKey))
            //{
            //    storage = new CosmosDbPartitionedStorage(settings?.CosmosDb);
            //}
            //else
            //{
            //    storage = new MemoryStorage();
            //}

            //return storage;
        }

        public bool IsSkill(BotSettings settings)
        {
            return settings?.SkillConfiguration?.IsSkill == true;
        }

        public BotFrameworkHttpAdapter GetBotAdapter(IStorage storage, BotSettings settings, UserState userState, ConversationState conversationState, IServiceProvider s)
        {
            var adapter = IsSkill(settings)
                ? new BotFrameworkHttpAdapter(new ConfigurationCredentialProvider(this.Configuration), s.GetService<AuthenticationConfiguration>())
                : new BotFrameworkHttpAdapter(new ConfigurationCredentialProvider(this.Configuration));

            adapter
              .UseStorage(storage)
              .UseBotState(userState, conversationState)
              .Use(new RegisterClassMiddleware<IConfiguration>(Configuration))
              .Use(s.GetService<TelemetryInitializerMiddleware>());

            // Configure Middlewares
            ConfigureTranscriptLoggerMiddleware(adapter, settings);
            ConfigureInspectionMiddleWare(adapter, settings, storage);
            ConfigureShowTypingMiddleWare(adapter, settings);
            ConfigureSetSpeakMiddleWare(adapter, settings);

            adapter.OnTurnError = async (turnContext, exception) =>
            {
                await turnContext.SendActivityAsync(exception.Message).ConfigureAwait(false);
                await conversationState.ClearStateAsync(turnContext).ConfigureAwait(false);
                await conversationState.SaveChangesAsync(turnContext).ConfigureAwait(false);
            };
            return adapter;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton<IConfiguration>(this.Configuration);

            // Load settings
            var settings = new BotSettings();
            Configuration.Bind(settings);
            Microsoft.BotFramework.Composer.DAL.DALConfiguration.ConnectionString = settings.ConnectionString;

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            services.AddTransient<BotAdapter>(sp => (BotFrameworkHttpAdapter)sp.GetService<IBotFrameworkHttpAdapter>());

            // Register AuthConfiguration to enable custom claim validation for skills.
            services.AddSingleton(sp => new AuthenticationConfiguration { ClaimsValidator = new AllowedCallersClaimsValidator(settings.SkillConfiguration) });

            // register components.
            ComponentRegistration.Add(new DialogsComponentRegistration());
            ComponentRegistration.Add(new DeclarativeComponentRegistration());
            ComponentRegistration.Add(new AdaptiveComponentRegistration());
            ComponentRegistration.Add(new LanguageGenerationComponentRegistration());
            ComponentRegistration.Add(new QnAMakerComponentRegistration());
            ComponentRegistration.Add(new LuisComponentRegistration());

            // register Handoff 
            ConfigureHandOff(services, settings);

            // This is for custom action component registration.
            ComponentRegistration.Add(new CustomActionComponentRegistration());

            // Register the skills client and skills request handler.
            services.AddTransient<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
            services.AddHttpClient<BotFrameworkClient, SkillHttpClient>();
            services.AddTransient<ChannelServiceHandler, SkillHandler>();

            // Register telemetry client, initializers and middleware
            services.AddApplicationInsightsTelemetry(settings?.ApplicationInsights?.InstrumentationKey ?? string.Empty);

            services.AddTransient<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();
            services.AddTransient<ITelemetryInitializer, TelemetryBotIdInitializer>();
            services.AddTransient<IBotTelemetryClient, BotTelemetryClient>();
            services.AddTransient<TelemetryLoggerMiddleware>(sp =>
            {
                var telemetryClient = sp.GetService<IBotTelemetryClient>();
                return new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: settings?.Telemetry?.LogPersonalInformation ?? false);
            });
            services.AddTransient<TelemetryInitializerMiddleware>(sp =>
            {
                var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
                var telemetryLoggerMiddleware = sp.GetService<TelemetryLoggerMiddleware>();
                return new TelemetryInitializerMiddleware(httpContextAccessor, telemetryLoggerMiddleware, settings?.Telemetry?.LogActivities ?? false);
            });

            var storage = ConfigureStorage(settings);

            services.AddSingleton(storage);
            var userState = new UserState(storage);
            var conversationState = new ConversationState(storage);
            services.AddSingleton(userState);
            services.AddSingleton(conversationState);

            //// Configure bot loading path
            var botDir = settings.Bot;
            var resourceExplorer = new ResourceExplorer().AddFolder(botDir);

            //var defaultLocale = Configuration.GetValue<string>("defaultLanguage") ?? "en-us";
            //var rootDialog = GetRootDialog(botDir);

            services.AddSingleton(resourceExplorer);

            resourceExplorer.RegisterType<OnQnAMatch>("Microsoft.OnQnAMatch");

            services.AddTransient<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>(s =>
                GetBotAdapter(storage, settings, userState, conversationState, s));

            var removeRecipientMention = settings?.Feature?.RemoveRecipientMention ?? false;

            services.AddDbContext<BotDbContext>(
                options =>
            options.UseSqlServer(settings.ConnectionString),
                ServiceLifetime.Transient);

            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient<IMessageService, MessageService>();
            services.AddTransient<IUserService, UserService>();

            //services.AddSingleton<IBot, ComposerBot>();
            services.AddTransient<IBot, ComposerBot>();

            services.AddTransient<Dialog, WatchDialog>();

            //services.AddSingleton<IBot>(s =>
            //    new ComposerBot(
            //        s.GetService<UserService>(),
            //        s.GetService<ConversationState>(),
            //        s.GetService<UserState>(),
            //        s.GetService<ResourceExplorer>(),
            //        s.GetService<BotFrameworkClient>(),
            //        s.GetService<SkillConversationIdFactoryBase>(),
            //        s.GetService<MessageRouter>(),
            //        s.GetService<MessageRouterResultHandler>(),
            //        s.GetService<IBotTelemetryClient>(),
            //        rootDialog,
            //        defaultLocale,
            //        removeRecipientMention));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            CustomAction.Configuration.ServiceProvider = serviceProvider;
            CustomAction.Configuration.LoggerFactory = loggerFactory;

            app.UseDefaultFiles();
            app.UseStaticFiles();

            //app.UseNamedPipes(System.Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME") + ".directline");
            app.UseWebSockets();
            app.UseRouting()
               .UseEndpoints(endpoints =>
               {
                   endpoints.MapControllers();
               });
        }

        private static bool ConfigSectionValid(string val)
        {
            return !string.IsNullOrEmpty(val) && !val.StartsWith('<');
        }

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
