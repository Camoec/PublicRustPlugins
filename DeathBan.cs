using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Death Ban", "Camoec", 1.0)]
    [Description("Temporal and perma bans on death")]

    public class DeathBan : RustPlugin
    {
        private const string UsePerm = "deathban.use";
        private const string BypassPerm = "deathban.bypass";


        private StoredData _data = new StoredData();
        private class StoredData
        {
            public Dictionary<string, DateTime> activeBans = new Dictionary<string, DateTime>();
        }

        private PluginConfig _config;
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "TemporalBan")]
            public bool TemporalBan = true;

            [JsonProperty(PropertyName = "Ban Time (In Seconds)")]
            public int BanTime = 30;
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);
        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            SaveConfig();
        }
        protected override void LoadConfig()
        { 
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                    throw new Exception();

                SaveConfig(); // override posible obsolet / outdated config
            }
            catch (Exception)
            {
                PrintError("Loaded default config.");

                LoadDefaultConfig();
            }
        }

        private void Init()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("DeathBan"))
            {
                SaveData();
            }
            else
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("DeathBan");
            }

            //SaveData(); // Fix posible miss match

            permission.RegisterPermission(UsePerm, this);
            permission.RegisterPermission(BypassPerm, this);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject<StoredData>( "DeathBan", _data);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Reject"] = "You still have {0} seconds left of ban",
                ["TempBan"] = "You have been banned for {0} seconds",
                ["PermBan"] = "You have been permanently banned"
            }, this);
        }

        private string Lang(string key, string userid) => lang.GetMessage(key, this, userid);


        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null)
                return null;

            if(permission.UserHasPermission(player.UserIDString, UsePerm) && !permission.UserHasPermission(player.UserIDString, BypassPerm))
            {
                if (_config.TemporalBan)
                {
                    Puts($"Banning player '{player}' for {_config.BanTime} seconds");
                    if (!_data.activeBans.ContainsKey(player.UserIDString))
                        _data.activeBans.Add(player.UserIDString, DateTime.Now + TimeSpan.FromSeconds(_config.BanTime));
                    else
                        _data.activeBans[player.UserIDString] = DateTime.Now + TimeSpan.FromSeconds(_config.BanTime);

                    player.Kick(string.Format(Lang("TempBan", player.UserIDString), _config.BanTime));
                }
                else
                {
                    Puts($"Permanent ban for player '{player}'");
                    ServerUsers.Set(player.userID, ServerUsers.UserGroup.Banned, string.Empty, string.Empty);
                    player.Kick(Lang("PermBan", player.UserIDString));
                }

                SaveData();
            }

            

            return null;
        }

        object CanUserLogin(string name, string id, string ipAddress)
        {
            if(_data.activeBans.ContainsKey(id))
            {
                if (_data.activeBans[id] < DateTime.Now)
                {
                    _data.activeBans.Remove(id);
                    SaveData();
                }
                else
                {
                    //Puts($"'{name}' tried to connect but still has ban until {_data.activeBans[id]}");
                    return string.Format(Lang("Reject", id), (int)(_data.activeBans[id] - DateTime.Now).TotalSeconds);
                }
            }
            return true;
        }

    }
}