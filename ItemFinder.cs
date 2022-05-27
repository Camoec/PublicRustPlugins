using Oxide.Core.Libraries.Covalence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Item Finder", "Camoec", 1.1)]
    [Description("Get count of specific item in the server")]

    public class ItemFinder : RustPlugin
    {
        private const string Perm = "itemfinder.use";

        private void Init()
        {
            permission.RegisterPermission(Perm, this);
            
        }

        private class ItemsInfo
        {
            public string shortname;
            public int itemId;

            public int droppedCount;
            public int dropped;
            public int inPlayers;
            public int inCointaners;

            public int totalCount => droppedCount + inPlayers + inCointaners;
        }

        [ChatCommand("itemfinder")]
        private void GetActiveEnts(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, Perm))
            {
                player.IPlayer.Reply(Lang("NoPermission", player.UserIDString));
                return;
            }
            if(args.Length == 0 || args[0] == null)
            {
                player.IPlayer.Reply(Lang("InvalidSyntax", player.UserIDString));
                return;
            }
            var info = GetInfo(args[0]);
           
            if (info == null)
            {
                player.IPlayer.Reply(string.Format(Lang("NotFound", player.UserIDString), info.shortname));
                return;
            }

            player.IPlayer.Reply(string.Format(Lang("Found", player.UserIDString), info.itemId, info.dropped, info.inCointaners, info.inPlayers, info.totalCount ));
        }

        private int? GetItemId(string shortname) => ItemManager.FindItemDefinition(shortname)?.itemid;
        private ItemsInfo GetInfo(string shortname)
        {
            ItemsInfo info = new ItemsInfo();
            info.shortname = shortname;
            int? itemid = GetItemId(shortname);
            info.itemId = itemid != null ? itemid.Value : -1;

            ItemDefinition itemDef = null;
            for (int i = 0; i <  ItemManager.itemList.Count;i++)
            {
                if (ItemManager.itemList[i].shortname == shortname)
                    itemDef = ItemManager.itemList[i];
            }

            if(itemDef == null)
            {
                // item not found
                return null;
            }

            // Get in players inventory
            foreach(BasePlayer player in BasePlayer.allPlayerList)
            {
                if (player == null)
                    continue;

                player.inventory.containerMain.itemList.ForEach((item) =>
                {
                    if (item.info.itemid == info.itemId)
                        info.inPlayers += item.amount;
                });
                player.inventory.containerBelt.itemList.ForEach((item) =>
                {
                    if (item.info.itemid == info.itemId)
                        info.inPlayers += item.amount;
                });
                player.inventory.containerWear.itemList.ForEach((item) =>
                {
                    if (item.info.itemid == info.itemId)
                        info.inPlayers += item.amount;
                });
            }

            // Get in AllCointainers
            foreach(var entity in BaseNetworkable.serverEntities)
            {
                var droppedItem = entity as DroppedItem;
                if(droppedItem != null)
                {
                    var item = droppedItem.GetItem();
                    if (item.info.itemid == info.itemId)
                    {
                        info.dropped += item.amount;
                    }
                    continue;
                }

                var container = entity as StorageContainer;
                if (container == null || container is LootContainer)
                    continue;

                var foundItems = container.inventory.FindItemsByItemID(info.itemId);
                foundItems.ForEach(item =>
                {
                    info.inCointaners += item.amount;
                });
            }

            return info;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["InvalidSyntax"] = "Use <color=#eb4213>/itemfinder</color> [item shortname]",
                ["NotFound"] = "Item '{0}' not found",
                ["Found"] = "<color=#eb4213>ItemInfo:</color>\r\nItemId:{0}\r\nDropped:{1}\r\nInContainers:{2}\r\ninPlayers:{3}\r\nTotal:{4}",
                ["NoPermission"] = "You not have permission to use this command"
            }, this);
        }

        private string Lang(string key, string userid) => lang.GetMessage(key, this, userid);

    }
}