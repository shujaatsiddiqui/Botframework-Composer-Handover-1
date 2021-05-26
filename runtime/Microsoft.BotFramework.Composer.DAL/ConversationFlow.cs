// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.BotFramework.Composer.Core
{
    public class ConversationFlow
    {
        public enum Question
        {
            Name,
            Start,
            Stop
        }

        // The last question asked.
        public Question LastQuestionAsked { get; set; } = Question.Start;
    }
}