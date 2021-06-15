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

        public string MicrosoftAppId  = "41cad738-7fa2-48dd-99b8-2d6ae8b97791";

        public string MicrosoftAppPassword  = "dTjFhtU55-9wr12l4yblUn.-.PNi~Jaq_5";

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
    }
}
