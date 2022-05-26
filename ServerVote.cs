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
    [Info("Server Vote", "Camoec", 1.0)]
    [Description("Community Vote System")]

    public class ServerVote : RustPlugin
    {
        private const string CreatePerm = "servervote.create";
        private const string UsePerm = "servervote.use";



        private PluginConfig _config;
        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Vote Time (in seconds)")]
            public float VoteTime = 60;
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
            cmd.AddChatCommand("vote", this, "VoteCommand");
            permission.RegisterPermission(CreatePerm, this);
            permission.RegisterPermission(UsePerm, this);
        }
        private class Vote
        {
            private static int idCounter;
            public int id { get; private set; }
            public Vote()
            {
                id = idCounter;
                idCounter++;
            }

            public string question;
            public int yesCount;
            public int noCount;
            public List<BasePlayer> votePlayers;
        }

        private Vote activeVote = null;

        private void TriggerClose(int voteId)
        {
            if (activeVote == null || activeVote.id != voteId)
                return;

            // close votation
            PrintToChat(String.Format(Lang("VoteClosed", null), activeVote.question, activeVote.yesCount, activeVote.noCount));
            

            activeVote = null;
        }

        private string GetAvailableCommands(BasePlayer player)
        {
            bool create = permission.UserHasPermission(player.UserIDString, CreatePerm);
            List<string> availableCommands = new List<string>();
            if (activeVote != null)
            {
                if (!activeVote.votePlayers.Contains(player))
                {
                    availableCommands.Add(Lang("yes", player.UserIDString));
                    availableCommands.Add(Lang("no", player.UserIDString));
                }

                if (create)
                    availableCommands.Add(Lang("close", player.UserIDString));

            }
            else if (create)
            {
                if (activeVote == null)
                    availableCommands.Add(Lang("create", player.UserIDString));
            }

            if (availableCommands.Count > 0)
            {

                string ret = "[";
                for (int i = 0; i < availableCommands.Count; i++)
                {
                    if (i != 0)
                        ret += ",";
                    ret += availableCommands[i];
                }

                ret += "]";
                return ret;
            }
            return "";
        }


        private void VoteCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, UsePerm))
            {
                PrintToChat(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length != 0)
            {
                if (args[0].ToLower() == Lang("create", player.UserIDString))
                {
                    if(args.Length == 1)
                    {
                        PrintToChat(Lang("CreateSyntax", player.UserIDString));
                        return;
                    }
                    // create vote
                    string question = "";
                    for (int i = 1; i < args.Length; i++)
                    {
                        question += args[i];
                        if (i < args.Length - 1)
                            question += " ";
                    }
                    activeVote = new Vote()
                    {
                        question = question,
                        yesCount = 0,
                        noCount = 0,
                        votePlayers = new List<BasePlayer>()
                    };
                    int currentId = activeVote.id;

                    timer.Once(_config.VoteTime, () => TriggerClose(currentId));
                    PrintToChat(string.Format(Lang("VoteInit", null), activeVote.question));

                    // show vote created
                    PrintToChat(player, Lang("VoteCreated", player.UserIDString));

                    return;
                }

                if (activeVote == null)
                {
                    // No active vote available
                    PrintToChat(player, Lang("NoActiveVote", player.UserIDString));
                    return;
                }

                if (args[0].ToLower() == Lang("close", player.UserIDString) && permission.UserHasPermission(player.UserIDString, CreatePerm))
                {
                    TriggerClose(activeVote.id);
                    activeVote = null;
                }

                if (activeVote.votePlayers.Contains(player))
                {
                    // show you already voted!
                    PrintToChat(player, Lang("AlreadyVote", player.UserIDString));
                    return;
                }

                if (args[0].ToLower() == Lang("yes", player.UserIDString))
                {
                    // show you vote yes
                    activeVote.votePlayers.Add(player);
                    activeVote.yesCount++;

                    PrintToChat(player, Lang("OnVote", player.UserIDString));

                    if (activeVote.votePlayers.Count >= BasePlayer.activePlayerList.Count)
                        TriggerClose(activeVote.id);
                    return;
                }

                if (args[0].ToLower() == Lang("no", player.UserIDString))
                {
                    // show you vote no
                    activeVote.votePlayers.Add(player);
                    activeVote.noCount++;

                    PrintToChat(player, Lang("OnVote", player.UserIDString));

                    if (activeVote.votePlayers.Count >= BasePlayer.activePlayerList.Count)
                        TriggerClose(activeVote.id);
                    return;
                }
            }

            string tmp = GetAvailableCommands(player);
            if (!string.IsNullOrEmpty(tmp))
                PrintToChat(player, String.Format(Lang("Syntax", player.UserIDString), tmp));
            else
                PrintToChat(player, Lang("NoActiveVote", player.UserIDString));
        }

        void Test()
        {
            
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                PrintToChat(player, $"Hello {player.displayName}");
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use this command",
                ["Syntax"] = "Use /vote {0}",
                ["CreateSyntax"] = "Use /vote create 'question'",
                ["VoteCreated"] = "Vote created succesfuly",
                ["NoActiveVote"] = "No vote active..",
                ["AlreadyVote"] = "You had already voted!",
                ["OnVote"] = "Thanks for your vote",
                ["VoteClosed"] = "<color=white>{0}</color>\r\n<color=green>yes:</color> {1}\r\n<color=red>no:</color> {2}",
                ["VoteInit"] = "{0}\r\nUse /vote",
                ["yes"] = "yes",
                ["no"] = "no",
                ["create"] = "create",
                ["close"] = "close",
            }, this);
        }

        private string Lang(string key, string userid) => lang.GetMessage(key, this, userid);

    }
}