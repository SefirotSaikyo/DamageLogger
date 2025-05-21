using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Warhead;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Reflection;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;
using WarheadEvents = Exiled.Events.Handlers.Warhead;

namespace DamageLoggerPlugin
{
    internal static class EventHandlers
    {
        // ---------- Cached reflection ----------
        private static readonly string[] WarheadTimeProps = { "TimeUntilDetonation", "DetonationTimer", "TimeToDetonation", "FuseTime" };
        private static readonly PropertyInfo KillerProp;
        private static readonly PropertyInfo AttackerProp;
        private static readonly PropertyInfo HurtingDHProp;
        private static readonly PropertyInfo DyingDHProp;
        private static readonly PropertyInfo DamageTypeProp;

        static EventHandlers()
        {
            var ht = typeof(HurtingEventArgs);
            HurtingDHProp = ht.GetProperty("DamageHandler") ?? ht.GetProperty("Handler");

            var dt = typeof(DyingEventArgs);
            DyingDHProp = dt.GetProperty("DamageHandler") ?? dt.GetProperty("Handler");
            KillerProp = dt.GetProperty("Killer");
            AttackerProp = dt.GetProperty("Attacker");

            if (HurtingDHProp != null)
                DamageTypeProp = HurtingDHProp.PropertyType.GetProperty("Type");
        }

        // -------------- Register / Unregister --------------
        internal static void Register()
        {
            PlayerEvents.Hurting += OnHurting_Safe;
            PlayerEvents.Dying += OnDying_Safe;
            ServerEvents.RoundStarted += OnRoundStarted_Safe;
            WarheadEvents.Starting += OnWarheadStarting_Safe;
            WarheadEvents.Stopping += OnWarheadStopping_Safe;
            WarheadEvents.Detonated += OnWarheadDetonated_Safe;
        }
        internal static void Unregister()
        {
            PlayerEvents.Hurting -= OnHurting_Safe;
            PlayerEvents.Dying -= OnDying_Safe;
            ServerEvents.RoundStarted -= OnRoundStarted_Safe;
            WarheadEvents.Starting -= OnWarheadStarting_Safe;
            WarheadEvents.Stopping -= OnWarheadStopping_Safe;
            WarheadEvents.Detonated -= OnWarheadDetonated_Safe;
        }

        // -------------- Safe wrappers ----------------------
        private static void OnHurting_Safe(HurtingEventArgs e) { Try(() => OnHurting(e), "Hurting"); }
        private static void OnDying_Safe(DyingEventArgs e) { Try(() => OnDying(e), "Dying"); }
        private static void OnRoundStarted_Safe() { Try(OnRoundStarted, "RoundStarted"); }
        private static void OnWarheadStarting_Safe(StartingEventArgs e) { Try(() => OnWarheadStarting(e), "WarheadStart"); }
        private static void OnWarheadStopping_Safe(StoppingEventArgs e) { Try(() => OnWarheadStopping(e), "WarheadStop"); }
        private static void OnWarheadDetonated_Safe() { Try(OnWarheadDetonated, "WarheadDetonate"); }

        private static void Try(Action act, string tag)
        {
            try { act(); }
            catch (Exception ex) { Log.Error($"[DamageLogger] {tag} exception: {ex}"); }
        }

        // -------------- Helpers ----------------------------
        private static string GetDamageType(object dh)
        {
            if (DamageTypeProp == null || dh == null) return "Unknown";
            try { return DamageTypeProp.GetValue(dh)?.ToString() ?? "Unknown"; } catch { return "Unknown"; }
        }

        // -------------- Round start ------------------------
        private static void OnRoundStarted()
        {
            foreach (var pl in Player.List)
            {
                if (!pl.IsVerified || pl.Role.Type == RoleTypeId.None) continue;
                var data = new Dictionary<string, string>
                {
                    {"playerNickname", pl.Nickname},
                    {"playerSteamId",  pl.UserId},
                    {"playerRole",     pl.Role.Type.ToString()}
                };
                DiscordService.Enqueue(Localization.Format("round_role", data));
            }
        }

        // -------------- Hurting -----------------------------
        private static void OnHurting(HurtingEventArgs ev)
        {
            if (ev.Attacker == null || ev.Attacker == ev.Player) return;
            bool friendly = ev.Attacker.Role.Team == ev.Player.Role.Team;
            string cause = GetDamageType(HurtingDHProp?.GetValue(ev));
            var data = new Dictionary<string, string>
            {
                {"attackerNickname", ev.Attacker.Nickname},
                {"attackerSteamId",  ev.Attacker.UserId},
                {"attackerRole",     ev.Attacker.Role.Type.ToString()},
                {"victimNickname",   ev.Player.Nickname},
                {"victimSteamId",    ev.Player.UserId},
                {"victimRole",       ev.Player.Role.Type.ToString()},
                {"damage",           ev.Amount.ToString("F1")},
                {"cause",            cause}
            };
            DiscordService.Enqueue(Localization.Format(friendly ? "friendly_damage" : "pvp_damage", data));
        }

        // -------------- Dying -------------------------------
        private static void OnDying(DyingEventArgs ev)
        {
            Player killer = KillerProp?.GetValue(ev) as Player ?? AttackerProp?.GetValue(ev) as Player;
            string cause = GetDamageType(DyingDHProp?.GetValue(ev));
            var data = new Dictionary<string, string>
            {
                {"victimNickname", ev.Player.Nickname},
                {"victimSteamId",  ev.Player.UserId},
                {"victimRole",     ev.Player.Role.Type.ToString()},
                {"cause",          cause},
                {"killerNickname", killer?.Nickname ?? "Environment"},
                {"killerSteamId",  killer?.UserId ?? "N/A"},
                {"killerRole",     killer?.Role.Type.ToString() ?? "N/A"}
            };
            DiscordService.Enqueue(Localization.Format("death", data));
        }

        // -------------- Warhead -----------------------------
        private static void OnWarheadStarting(StartingEventArgs ev)
        {
            double sec = 0;
            foreach (var p in new[] { "TimeToDetonation", "TimeUntilDetonation", "DetonationTimer", "Delay" })
            {
                var prop = ev.GetType().GetProperty(p);
                if (prop == null) continue;
                sec = Convert.ToDouble(prop.GetValue(ev));
                if (sec > 0) break;
            }
            if (sec <= 0)
                foreach (var p in WarheadTimeProps)
                {
                    var pi = typeof(Warhead).GetProperty(p);
                    if (pi != null && pi.PropertyType == typeof(double)) { sec = (double)pi.GetValue(null); if (sec > 0) break; }
                }
            if (sec <= 0) sec = 90;
            var data = new Dictionary<string, string>
            {
                {"playerNickname", ev.Player?.Nickname ?? "AUTO"},
                {"playerSteamId",  ev.Player?.UserId  ?? "AUTO"},
                {"playerRole",     ev.Player?.Role.Type.ToString() ?? "Server"},
                {"timeLeft",       Math.Ceiling(sec).ToString()}
            };
            DiscordService.Enqueue(Localization.Format("warhead_start", data));
        }

        private static void OnWarheadStopping(StoppingEventArgs ev)
        {
            var data = new Dictionary<string, string>
            {
                {"playerNickname", ev.Player?.Nickname ?? "Unknown"},
                {"playerSteamId",  ev.Player?.UserId  ?? "Unknown"},
                {"playerRole",     ev.Player?.Role.Type.ToString() ?? "Server"}
            };
            DiscordService.Enqueue(Localization.Format("warhead_stop", data));
        }

        private static void OnWarheadDetonated() => DiscordService.Enqueue(Localization.Format("warhead_explode", new Dictionary<string, string>()));
    }
}
