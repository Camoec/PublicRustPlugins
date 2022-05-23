using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("HeliScrap", "Camoec", 1.0)]
    [Description("Call heli with scrap")]

    public class HeliScrap : RustPlugin
    {
        private PluginConfig _config; 
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "ChatPrefix")]
            public string ChatPrefix = "<color=#eb4213>HeliScrap</color>:";

            [JsonProperty(PropertyName = "Command")]
            public string Command = "CallHeli";

            [JsonProperty(PropertyName = "Scrap Amount")]
            public int ScrapAmount = 100;

            [JsonProperty(PropertyName = "UsePermission")]
            public bool UsePermission = true;

            [JsonProperty(PropertyName = "MaxSpawnedHelis")]
            public int MaxSpawnedHelis = 1;
        }

        private const string UsePerm = "heliscrap.use";
        private const string HELI_PREFAB = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
        private HashSet<BaseHelicopter> activeHelis = new HashSet<BaseHelicopter>();


        #region Config Setup

        protected override void SaveConfig() => Config.WriteObject(_config, true);
        protected override void LoadDefaultConfig()
        {
            //base.LoadDefaultConfig();
            _config = new PluginConfig();
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
                ["Success"] = "Heli called successfuly",
                ["MaxSpawnedHelis"] = "Max heli in bound reached",
                ["NoRequiredScrap"] = "You don't have {0} of scrap in your inventory",
                ["NoPermission"] = "You don't have required permission to use this command"
            }, this);
        }

        private string Lang(string key) => lang.GetMessage(key, this, null);

        #endregion

        private void Init()
        {
            cmd.AddChatCommand(_config.Command, this, "CallCommand");
            permission.RegisterPermission(UsePerm, this);
        }

        private void CallCommand(BasePlayer player)
        {
            if(permission.UserHasPermission(player.UserIDString, UsePerm) || !_config.UsePermission)
            {
                if(CanRemoveItem(player, -932201673, _config.ScrapAmount)) 
                {
                    CheckHelis();
                    if(activeHelis.Count < _config.MaxSpawnedHelis)
                    {
                        RemoveItemsFromInventory(player, -932201673, _config.ScrapAmount);
                        // call heli
                        callHeli();



                        PrintToChat(player, $"{_config.ChatPrefix} {Lang(key: "Success")}");
                    }
                    else
                    {
                        PrintToChat(player, $"{_config.ChatPrefix} {Lang(key: "MaxSpawnedHelis")}");
                    }
                }
                else
                {
                    PrintToChat(player, $"{_config.ChatPrefix} {string.Format(Lang(key: "NoRequiredScrap"), _config.ScrapAmount)}");
                }
            }
            else
            {
                PrintToChat(player, $"{_config.ChatPrefix} {Lang("NoPermission")}");
            }
        }

        
        

        private void OnServerInitialized()
        {
            
            foreach(var entity in BaseNetworkable.serverEntities)
            {
                if (entity == null || (entity as BaseHelicopter) == null)
                    continue;

                activeHelis.Add(entity as BaseHelicopter);
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity as BaseHelicopter != null)
                activeHelis.Add(entity as BaseHelicopter);
        }

        

        #region Misc

        private void CheckHelis()
        {
            activeHelis.RemoveWhere(heli => heli?.IsDestroyed ?? true);
        }

        private bool CanRemoveItem(BasePlayer player, int itemid, int amount)
        {
            var foundAmount = 0;
            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item != null && item.info.itemid == itemid)
                {
                    foundAmount = foundAmount + item.amount;
                }
            }

            if (foundAmount >= amount)
                return true;
            return false;
        }

        public void RemoveItemsFromInventory(BasePlayer player, int itemid, int amount)
        {
            var items = player.inventory.containerMain.itemList;

            int removeAmount = 0;
            int amountRemaining = amount;

            for(int i = 0; i < items.Count; i++ )
            {
                var item = items[i];
                if (item == null || item.info.itemid != itemid)
                    continue;

                removeAmount = amountRemaining;
                if (item.amount < removeAmount)
                    removeAmount = item.amount;

                if (item.amount > removeAmount)
                    item.SplitItem(removeAmount);
                else
                    item.UseItem(removeAmount);
                amountRemaining = amountRemaining - removeAmount;

                if (amountRemaining <= 0)
                    break;
            }
        }
        private BaseHelicopter callHeli(Vector3 coordinates = new Vector3(), bool setPositionAfterSpawn = true)
        {
            var heli = (BaseHelicopter)GameManager.server.CreateEntity(HELI_PREFAB, new Vector3(), new Quaternion(), true);
            if (heli == null)
            {
                PrintWarning("Failed to create heli prefab on " + nameof(callHeli));
                return null;
            }

            var heliAI = heli?.GetComponent<PatrolHelicopterAI>() ?? null;
            if (heliAI == null)
            {
                PrintWarning("Failed to get helicopter AI on " + nameof(callHeli));
                return null;
            }
            if (coordinates != Vector3.zero)
            {
                if (coordinates.y < 225)
                    coordinates.y = 225;
                heliAI.SetInitialDestination(coordinates, 0.25f);
                if (setPositionAfterSpawn)
                    heli.transform.position = heliAI.transform.position = coordinates;
            }
            heli.Spawn();
            
            return heli;
        }

        #endregion

    }
}