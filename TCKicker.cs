
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("TCKicker", "Camoec", 1.1)]
    [Description("When a tc is destroyed all owners are kicket")]

    public class TCKicker : RustPlugin
    {
        private const string CupBoardPrefab = "assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab";

        private const string KickeablePerm = "tckicker.kickeable";
        private const string BypassKickPerm = "tckicker.bypasskick";
        private const string DestroyBypassPerm = "tckicker.bypassdestroy";

        private void Init()
        {
            permission.RegisterPermission(KickeablePerm, this);
            permission.RegisterPermission(BypassKickPerm, this);
            permission.RegisterPermission(DestroyBypassPerm, this);
        }

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "AllowSamePlayers (if enabled when a cumboard is destroyed by a authed player all authed players and he got kicked)")]
            public bool AllowSamePlayers = false;

            [JsonProperty(PropertyName = "Use Ban")]
            public bool UseBan = false;
        }

        private PluginConfig _config;

        protected override void SaveConfig() => Config.WriteObject(_config, true);
        protected override void LoadDefaultConfig()
        {
            //base.LoadDefaultConfig();
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

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["KickMessage"] = "Your tc has been destroyed"
            }, this);
        }

        private string Lang(string key) => lang.GetMessage(key, this, null);


        void OnEntityDeath(BuildingPrivlidge entity, HitInfo info)
        {
            if (entity == null || entity.PrefabName != CupBoardPrefab)
                return;

            if (info != null && info.InitiatorPlayer != null && permission.UserHasPermission(info.InitiatorPlayer.UserIDString, DestroyBypassPerm))
                return;

            if (info != null && !_config.AllowSamePlayers && entity.IsAuthed(info.InitiatorPlayer))
                return;

            foreach (var _player in entity.authorizedPlayers)
            {
                var player = BasePlayer.FindByID(_player.userid);
                if (permission.UserHasPermission(player.UserIDString, KickeablePerm) && !permission.UserHasPermission(player.UserIDString, BypassKickPerm))
                {
                    if (_config.UseBan)
                    {
                        ServerUsers.Set(player.userID, ServerUsers.UserGroup.Banned, string.Empty, string.Empty);
                    }
                    player.Kick(Lang("KickMessage"));
                }
            }
        }
    }
}