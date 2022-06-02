using UnityEngine;
using System;
using Oxide.Core;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;



namespace Oxide.Plugins
{
    [Info("Grade Permissions", "Camoec", 1.0)]
    [Description("Allows players with permission to use grades")]

    public class GradePermissions : RustPlugin
    {
        private const string AllPerm = "GradePermissions.all";
        private const string WoodPerm = "GradePermissions.wood";
        private const string StonePerm = "GradePermissions.stone";
        private const string MetalPerm = "GradePermissions.metal";
        private const string TopTier = "GradePermissions.toptier";
        private void Init()
        {
            permission.RegisterPermission(AllPerm, this);
            permission.RegisterPermission(WoodPerm, this);
            permission.RegisterPermission(StonePerm, this);
            permission.RegisterPermission(MetalPerm, this);
            permission.RegisterPermission(TopTier, this);
        }

        bool CanChangeGrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (permission.UserHasPermission(player.UserIDString, AllPerm))
                return true;
            bool allowed = true;
            switch (grade)
            {
                case BuildingGrade.Enum.Twigs: return true;
                    break;
                case BuildingGrade.Enum.Wood:
                    allowed = permission.UserHasPermission(player.UserIDString, WoodPerm);
                    break;
                case BuildingGrade.Enum.Stone:
                    allowed = permission.UserHasPermission(player.UserIDString, StonePerm);
                    break;
                case BuildingGrade.Enum.Metal:
                    allowed = permission.UserHasPermission(player.UserIDString, MetalPerm);
                    break;
                case BuildingGrade.Enum.TopTier:
                    allowed = permission.UserHasPermission(player.UserIDString, TopTier);
                    break;
                default: return true;
            }

            if (!allowed)
                PrintToChat(player, Lang("NoPerm", player.UserIDString));

            return allowed;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPerm"] = "<color=#eb4213>GradePermissions:</color> You are not allowed to upgrade that!"
            }, this);
        }

        private string Lang(string key, string userid, params string[] args) => string.Format(lang.GetMessage(key, this, userid), args);
    }
}