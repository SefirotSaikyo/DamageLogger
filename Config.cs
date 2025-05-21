using System.ComponentModel;
using Exiled.API.Interfaces;

namespace DamageLoggerPlugin
{
    public sealed class Config : IConfig
    {
        [Description("Enable / disable the plugin")]
        public bool IsEnabled { get; set; } = true;

        [Description("Verbose debug output")]
        public bool Debug { get; set; } = true;

        [Description("Discord bot token")]
        public string BotToken { get; set; } = "Bot_YOUR_TOKEN_HERE";

        [Description("Target channel ID")]
        public ulong ChannelId { get; set; } = 0;

        [Description("Localization file name (inside /Configs)")]
        public string LocalizationFileName { get; set; } = "damage_logger_localization.json";
    }
}
