using System;
using System.Collections.Generic;


namespace Oxide.Plugins
{
    [Info("Custom Craft Times", "Camoec", 1.0)]
    [Description("Allows you to change the crafting times")]

    public class CustomCraftTimes : RustPlugin
    {
        Dictionary<int, BPItem> _restore = new Dictionary<int, BPItem>();
        private PluginConfig _config;

        private class BPItem
        {
            public string shortname;
            public float time;
        }        
        private class PluginConfig
        {
            public Dictionary<int,BPItem> itemdefinitions = new Dictionary<int, BPItem>();
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);
        private void _LoadDefaultConfig()
        {
            Puts("Creating new config file");
            _config = new PluginConfig();
            foreach(var bp in ItemManager.bpList)
            {
                _config.itemdefinitions.Add(bp.targetItem.itemid,new BPItem()
                {
                    shortname = bp.targetItem.shortname,
                    time = bp.time
                });
            }
            SaveConfig();
        }
        private void _LoadConfig()
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
                PrintError("Loaded default config");

                _LoadDefaultConfig();
            }
            
        }


        void OnServerInitialized(bool initial)
        {
            _LoadConfig();
            Puts("Loading new times");
            
            foreach (var bp in ItemManager.bpList)
            {
                _restore.Add(bp.targetItem.itemid, new BPItem() { time = bp.time, shortname = bp.name });
                bp.time = _config.itemdefinitions[bp.targetItem.itemid].time;
            }
        }

        void Unload()
        {
            if (ItemManager.bpList == null)
                return;
            foreach (var bp in ItemManager.bpList)
            {
                bp.time = _restore[bp.targetItem.itemid].time;
            }
        }
    }
}