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
            get { return "96da3395-a60e-4460-a185-cb60508712ba"; }
        }

        public string MicrosoftAppPassword
        {
            get { return "h1Zd0LY~P--0c4CU-6_3Sc9W0yTKidGlY9"; }
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
            get { return @"Server=tcp:chatbotserverdemo.database.windows.net,1433;Initial Catalog=chatbotdemo;Persist Security Info=False;User ID=serveradmin;Password=admin123..;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"; }
        }
    }
}
