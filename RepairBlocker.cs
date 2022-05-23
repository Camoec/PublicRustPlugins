using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace Oxide.Plugins
{
    [Info("RepairBlocker", "Camoec", 1.0)]
    [Description("Prevents certain objects from being repaired")]

    public class RepairBlocker : RustPlugin
    {
        private const string BypassPerm = "repairblocker.bypass";
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "BlackList")]
            public List<int> BlackList = new List<int>();
        }

        private PluginConfig _config;

        protected override void SaveConfig() => Config.WriteObject(_config, true);
        protected override void LoadDefaultConfig()
        {
            //base.LoadDefaultConfig();
            _config = new PluginConfig();
            _config.BlackList.Add(-1812555177); // rifle.lr300
            _config.BlackList.Add(1545779598); // rifle.ak
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
            permission.RegisterPermission(BypassPerm, this);
        }
        object OnItemRepair(BasePlayer player, Item item)
        {
            if (_config.BlackList.Contains(item.info.itemid) && !permission.UserHasPermission(player.UserIDString, BypassPerm))
                return false;
            return null;
        }
    }
}