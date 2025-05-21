# DamageLoggerPlugin

**DamageLoggerPlugin** is an EXILED plugin for *SCP: Secret Laboratory* that streams granular, real-time server events to a Discord channel through a self-hosted bot.

---
## ✨ Features

| Category      | What gets logged                                                                                           |
|---------------|-------------------------------------------------------------------------------------------------------------|
| **Damage**    | • PvP damage<br>• Friendly-fire damage (highlighted)<br>• Exact damage amount & cause (firearm / explosion / SCP / etc.) |
| **Deaths**    | • Victim & killer (nick, SteamID, role)<br>• Damage cause                                                  |
| **Warhead**   | • Activation (who, remaining time)<br>• Cancellation (who)<br>• Detonation                                  |
| **Round start** | • Every player’s spawn class at the beginning of the round                                                |

All messages are fully localisable (text, emojis, placeholders).

---
## 📋 Requirements

* **SCP:SL Dedicated Server** 14.x  
* **EXILED** 9.6 or newer  
* .NET 4.8 **or** `.NET SDK` (for building from source)  
* A Discord bot token & channel ID  

---
## 🚀 Installation

1. **Download** the latest release `DamageLoggerPlugin.dll` (or build it yourself — see below).  
2. Copy the DLL to  
   ```
   ~/.config/EXILED/Plugins/
3. Copy the example damage_logger_localization.json to your server’s Configs folder and edit to taste
4. Create (or edit) DamageLoggerPlugin.yml in Configs and set:
```
BotToken: "Bot YOUR_DISCORD_BOT_TOKEN"
ChannelId: 123456789012345678
LocalizationFileName: "damage_logger_localization.json"
Debug: false        # true → extra console output
```
5. Restart the server. First launch will create the config file if it doesn’t exist.
6. Enjoy real-time logs in Discord!

---
## 🛠 Configuration reference (Config.cs)

| Key                    | Default                           | Description                                     |
| ---------------------- | --------------------------------- | ----------------------------------------------- |
| `IsEnabled`            | `true`                            | Master on/off switch for the plugin             |
| `Debug`                | `false`                           | Print verbose diagnostics to the server console |
| `BotToken`             | `"Bot ..."`                       | Discord bot token (*keep it secret!*)           |
| `ChannelId`            | `0`                               | Numeric ID of the target text channel           |
| `LocalizationFileName` | `damage_logger_localization.json` | Relative path in the `Configs` folder           |

Changes take effect after a server restart.

---
## 🌐 Localisation
The plugin replaces placeholders in the JSON file with runtime values:
| Placeholder          | Meaning                         |
| -------------------- | ------------------------------- |
| `{attackerNickname}` | Attacker’s in-game nickname     |
| `{attackerSteamId}`  | Attacker’s SteamID64            |
| `{attackerRole}`     | Spawn role (e.g. `NtfSergeant`) |
| …                    | *(see source for full list)*    |

Add emojis, change wording, or translate freely; just don’t remove the curly-brace tags.

---
## 🏗 Building from source
```
# clone repository
$ git clone https://github.com/yourname/DamageLoggerPlugin.git && cd DamageLoggerPlugin

# restore & build
$ dotnet restore
$ dotnet build -c Release

# resulting DLL
$ cp bin/Release/netstandard2.0/DamageLoggerPlugin.dll <SCP-SL>/~/.config/EXILED/Plugins/
```
Dependencies are pulled from NuGet:
```
<PackageReference Include="Exiled.API" Version="9.6.0" />
<PackageReference Include="System.Text.Json" Version="8.*" />
```
## ❓ Support / Issues
Open an issue or pull request on the repository, or ping **@sefirot_saikyo** in the EXILED Discord.

## 📝 License
MIT — do whatever you want, just don’t blame me if something breaks your server.
