using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.Twitter.Dto;
using Orleans;

namespace Aevatar.GAgents.Twitter.Grains;

public interface ITwitterGrain : IGrainWithStringKey
{
    public Task CreateTweetAsync(string consumerKey, string consumerSecret, string text, string token,
        string tokenSecret);

    public Task ReplyTweetAsync(string consumerKey, string consumerSecret, string text, string tweetId, string token,
        string tokenSecret);

    public Task<List<Tweet>> GetRecentMentionAsync(string userName, string bearerToken, int replyLimit);
}