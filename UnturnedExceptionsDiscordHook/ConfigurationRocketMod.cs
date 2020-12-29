#region

using System.Collections.Generic;
using Rocket.API;

#endregion

namespace RG.UnturnedExceptionsDiscordHook
{
    public class ConfigurationRocketMod : IRocketPluginConfiguration
    {
        public HashSet<string> DiscordHooks;

        public ConfigurationRocketMod()
        {
            DiscordHooks = new HashSet<string>();
        }

        public void LoadDefaults()
        {
            DiscordHooks = new HashSet<string>
            {
                "Your url here"
            };
        }
    }
}