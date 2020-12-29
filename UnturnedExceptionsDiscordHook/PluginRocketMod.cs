#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

#endregion

namespace RG.UnturnedExceptionsDiscordHook
{
    [UsedImplicitly]
    public class PluginRocketMod : RocketPlugin<ConfigurationRocketMod>
    {
        private const string c_BaseHookUrl = "https://discord.com/api/webhooks/";

        private static readonly Harmony s_HarmonyObject = new Harmony("RG.UnturnedExceptionsDiscordHook");

        private static readonly MethodInfo s_LogErrorOriginalMethod =
            typeof(UnturnedLog).GetMethod(nameof(UnturnedLog.error), new[] {typeof(string)});

        private static readonly HarmonyMethod s_LogErrorRedirectMethod =
            new HarmonyMethod(typeof(PluginRocketMod).GetMethod(nameof(LogErrorPatch),
                BindingFlags.Static | BindingFlags.NonPublic));

        private static PluginRocketMod s_Instance;
        private readonly HashSet<string> m_DiscordHooks = new HashSet<string>();

        private CancellationTokenSource m_CancellationToken;

        protected override void Load()
        {
            s_Instance = this;
            m_DiscordHooks.Clear();
            foreach (var discordHook in Configuration.Instance.DiscordHooks)
            {
                if (!discordHook.StartsWith(c_BaseHookUrl, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogWarning($"Invalid DiscordHook '{discordHook}'");
                    continue;
                }

                m_DiscordHooks.Add(discordHook);
            }

            if (m_DiscordHooks.Count == 0)
                return;

            m_CancellationToken = new CancellationTokenSource();
            s_HarmonyObject.Patch(s_LogErrorOriginalMethod, postfix: s_LogErrorRedirectMethod);
        }

        protected override void Unload()
        {
            s_HarmonyObject.UnpatchAll();
            m_DiscordHooks.Clear();
            m_CancellationToken?.Cancel();

            if (s_Instance == this)
                s_Instance = null;
        }

        private static void LogErrorPatch(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var token = s_Instance.m_CancellationToken.Token;
            Utils.TaskRun(() => s_Instance.SendError(message, token), token);
        }

        private void SendError(string message, CancellationToken token)
        {
            var webMessage = new WWWForm();
            webMessage.AddField("avatar_url", Provider.configData.Browser.Icon);
            webMessage.AddField("username", Provider.serverName);

            if (message.Length > 2000)
                webMessage.AddBinaryData("Exception", Encoding.UTF8.GetBytes(message), "Exception.txt");
            else
                webMessage.AddField("content", message);

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = token
            };
            Parallel.ForEach(m_DiscordHooks, parallelOptions,
                async url => await Utils.WrapTryCatchAction(() => DiscordPost(url, webMessage, token)));
        }

        public async Task DiscordPost(string url, WWWForm webMessage, CancellationToken token)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);

            var data = webMessage.data;
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = webMessage.headers["Content-Type"];
            request.ContentLength = data.Length;

            using (var stream = await request.GetRequestStreamAsync())
            {
                await stream.WriteAsync(data, offset: 0, data.Length, token);
            }

            // ReSharper disable AssignNullToNotNullAttribute
            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Logger.Log($"DiscordHook response: {await reader.ReadToEndAsync()}");
            }
            // ReSharper restore AssignNullToNotNullAttribute
        }
    }
}