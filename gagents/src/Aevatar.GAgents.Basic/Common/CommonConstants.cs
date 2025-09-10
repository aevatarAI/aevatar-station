using System;

namespace Aevatar.GAgents.Basic;

public static class CommonConstants
{
    public const string PubSubName = "pubsub";
    public const string TestGroup = "TestGroup";
    public const string TwitterTopic = "Twitter";
    public const string TelegramTopic = "Telegram";
    public const string GptTopic = "Gpt";
    
    public const string CommonTopic = "CommonTopic";
    
    public const string StreamNamespace = "AINamespace";
    public const string StreamProvider = "AISmart";
    public static Guid StreamGuid = Guid.NewGuid();

}