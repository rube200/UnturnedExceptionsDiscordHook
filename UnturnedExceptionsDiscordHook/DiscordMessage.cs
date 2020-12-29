#region

using Newtonsoft.Json;

#endregion

namespace RG.UnturnedExceptionsDiscordHook
{
    public class DiscordMessage
    {
        [JsonProperty("avatar_url")] public string AvatarIcon { get; set; }
        [JsonProperty("content")] public string Content { get; set; }
        [JsonProperty("file")] public byte[] File { get; set; }
        [JsonProperty("username")] public string Username { get; set; }
    }
}