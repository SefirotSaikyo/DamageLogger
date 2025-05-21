using System;
using System.IO;
using Exiled.API.Features;
using PlayerEvent = Exiled.Events.Handlers.Player;

namespace DamageLoggerPlugin
{
    public sealed class DamageLoggerPlugin : Plugin<Config>
    {
        public override string Author => "Эзер";
        public override string Name => "Damage Logger";
        public override Version Version => new(1, 1, 0);
        public override Version RequiredExiledVersion => new(9, 6, 0);

        internal static DamageLoggerPlugin Instance { get; private set; }

        public override void OnEnabled()
        {
            Instance = this;

            // Load localization
            var locPath = Path.Combine(Paths.Configs, Config.LocalizationFileName);
            Localization.Load(locPath, Config.Debug);

            // Initialize Discord bot
            DiscordService.Initialize(Config.BotToken, Config.ChannelId, Config.Debug);

            // Register events
            EventHandlers.Register();

            Log.Info($"[{Name}] v{Version} enabled. Localization: {locPath}");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            EventHandlers.Unregister();
            DiscordService.Dispose();
            Instance = null;
            base.OnDisabled();
        }
    }
}
