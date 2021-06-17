// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.Azure;

namespace Microsoft.BotFramework.Composer.Core.Settings
{
    public class BotSettings
    {
        public BotFeatureSettings Feature { get; set; }

        public BlobStorageConfiguration BlobStorage { get; set; }

        public string MicrosoftAppId
        {
            get { return "a0312873-2617-4daa-9092-250fa79a04fd"; }
        }

        public string MicrosoftAppPassword
        {
            get { return "7yzQc-~R158~zAGxblrBf3hGMEX_j9h-~o"; }
        }

        public CosmosDbPartitionedStorageOptions CosmosDb { get; set; }

        public TelemetryConfiguration ApplicationInsights { get; set; }

        public AdditionalTelemetryConfiguration Telemetry { get; set; }

        public string Bot { get; set; }

        public BotSkillConfiguration SkillConfiguration { get; set; }

        public SpeechConfiguration Speech { get; set; }

        public string AzureTableStorageConnectionString { get; set; }

        public class BlobStorageConfiguration
        {
            public string ConnectionString { get; set; }

            public string Container { get; set; }
        }

        public class AdditionalTelemetryConfiguration
        {
            public bool LogPersonalInformation { get; set; }

            public bool LogActivities { get; set; }
        }

        public class SpeechConfiguration
        {
            public string VoiceFontName { get; set; }

            public bool FallbackToTextForSpeechIfEmpty { get; set; }
        }

        public string ConnectionString
        {
            get { return @"Server=tcp:wcubot.database.windows.net,1433;Initial Catalog=wcufabot;Persist Security Info=False;User ID=serveradmin;Password=Wcu@admin1!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; }
        }
    }
}
