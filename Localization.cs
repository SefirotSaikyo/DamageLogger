using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Exiled.API.Features;

namespace DamageLoggerPlugin
{
    internal static class Localization
    {
        private static readonly Dictionary<string, string> Messages = new();
        private static bool _debug;

        internal static void Load(string path, bool debug)
        {
            _debug = debug;

            if (!File.Exists(path))
            {
                Log.Error($"[Localization] File not found: {path}");
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                Messages.Clear();

                if (data != null)
                {
                    foreach (var kvp in data)
                        Messages[kvp.Key] = kvp.Value;
                }

                if (_debug) Log.Debug($"[Localization] Loaded {Messages.Count} keys");
            }
            catch (Exception ex)
            {
                Log.Error($"[Localization] Failed to parse file: {ex.Message}");
            }
        }

        /// <summary>Подставляет переменные в шаблон.</summary>
        internal static string Format(string key, IDictionary<string, string> args)
        {
            if (!Messages.TryGetValue(key, out var template))
            {
                if (_debug) Log.Debug($"[Localization] Missing key: {key}");
                template = key; // fallback
            }

            foreach (var kvp in args)
                template = template.Replace($"{{{kvp.Key}}}", kvp.Value);

            return template;
        }
    }
}