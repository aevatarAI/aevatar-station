using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.Twitter.Dto;
using Aevatar.GAgents.Twitter.Options;
using Aevatar.GAgents.Twitter.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Providers;

namespace Aevatar.GAgents.Twitter.Grains;

[StorageProvider(ProviderName = "PubSubStore")]
public class TwitterGrain : Grain, ITwitterGrain
{
    private readonly ITwitterProvider _twitterProvider;
    private ILogger<TwitterGrain> _logger;

    public TwitterGrain(ITwitterProvider twitterProvider,
        ILogger<TwitterGrain> logger)
    {
        _twitterProvider = twitterProvider;
        _logger = logger;
    }

    public async Task CreateTweetAsync(string consumerKey, string consumerSecret, string text, string token,
        string tokenSecret)
    {
        await _twitterProvider.PostTwitterAsync(consumerKey, consumerSecret, text, token, tokenSecret);
    }

    public async Task ReplyTweetAsync(string consumerKey, string consumerSecret, string text, string tweetId,
        string token, string tokenSecret)
    {
        await _twitterProvider.ReplyAsync(consumerKey, consumerSecret, text, tweetId, token, tokenSecret);
    }

    public async Task<List<Tweet>> GetRecentMentionAsync(string userName, string bearToken, int replyLimit)
    {
        try
        {
            _logger.LogError($"username-->{userName}, bearToken--->{bearToken}, replyLimit--{replyLimit}");
            var mentionList = await _twitterProvider.GetMentionsAsync(userName, bearToken);
            return mentionList.Take(replyLimit).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError($"[TwitterGrain] [GetRecentMentionAsync] handler error,ex:{e}");
        }

        return new List<Tweet>();
    }
}