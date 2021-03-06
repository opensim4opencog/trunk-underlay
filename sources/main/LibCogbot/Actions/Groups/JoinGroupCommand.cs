using System;
using System.Collections.Generic;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Text;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Groups
{
    public class JoinGroupCommand : Command, BotPersonalCommand
    {
        private ManualResetEvent GetGroupsSearchEvent = new ManualResetEvent(false);
        private UUID queryID = UUID.Zero;
        private UUID resolvedGroupID = UUID.Zero;
        private string groupName;
        private string resolvedGroupName;
        private bool joinedGroup;

        public JoinGroupCommand(BotClient testClient)
        {
            Name = "joingroup";
            TheBotClient = testClient;
        }

        public override void MakeInfo()
        {
            Description = "join a group.";
            Parameters = CreateParams("group", typeof (Group), "group you are going to " + Name);
            Details = AddUsage(Name + " group", Description);
            Category = CommandCategory.Groups;
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (args.Length < 1)
                return ShowUsage();

            groupName = String.Empty;
            resolvedGroupID = UUID.Zero;
            resolvedGroupName = String.Empty;

            if (args[0].ToLower() == "uuid")
            {
                if (args.Length < 2)
                    return ShowUsage();

                resolvedGroupName = groupName = args[1];
                int argsUsed;
                if (!UUIDTryParse(args, 1, out resolvedGroupID, out argsUsed))
                    return Failure(resolvedGroupName + " doesn't seem a valid UUID");
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                    groupName += args[i] + " ";
                groupName = groupName.Trim();

                Client.Directory.DirGroupsReply += Directory_DirGroups;

                queryID = Client.Directory.StartGroupSearch(groupName, 0);

                GetGroupsSearchEvent.WaitOne(60000, false);

                Client.Directory.DirGroupsReply -= Directory_DirGroups;

                GetGroupsSearchEvent.Reset();
            }

            if (resolvedGroupID == UUID.Zero)
            {
                if (string.IsNullOrEmpty(resolvedGroupName))
                    return Failure("Unable to obtain UUID for group " + groupName);
                else
                    return Success(resolvedGroupName);
            }

            Client.Groups.GroupJoinedReply += Groups_OnGroupJoined;
            Client.Groups.RequestJoinGroup(resolvedGroupID);

            /* A.Biondi 
             * TODO: implement the pay to join procedure.
             */

            GetGroupsSearchEvent.WaitOne(60000, false);

            Client.Groups.GroupJoinedReply -= Groups_OnGroupJoined;
            GetGroupsSearchEvent.Reset();
            Client.ReloadGroupsCache();

            if (joinedGroup)
                return Success("Joined the group " + resolvedGroupName);
            return Failure("Unable to join the group " + resolvedGroupName);
        }

        private void Directory_DirGroups(object sender, DirGroupsReplyEventArgs e)
        {
            if (queryID == e.QueryID)
            {
                queryID = UUID.Zero;
                if (e.MatchedGroups.Count < 1)
                {
                    WriteLine("ERROR: Got an empty reply");
                }
                else
                {
                    if (e.MatchedGroups.Count > 0)
                    {
                        /* A.Biondi 
                         * The Group search doesn't work as someone could expect...
                         * It'll give back to you a long list of groups even if the 
                         * searchText (groupName) matches esactly one of the groups 
                         * names present on the server, so we need to check each result.
                         * UUIDs of the matching groups are written on the console.
                         */
                        WriteLine("Matching groups are:\n");
                        foreach (DirectoryManager.GroupSearchData groupRetrieved in e.MatchedGroups)
                        {
                            WriteLine(groupRetrieved.GroupName + "\t\t\t(" +
                                      Name + " UUID " + groupRetrieved.GroupID.ToString() + ")");

                            if (groupRetrieved.GroupName.ToLower() == groupName.ToLower())
                            {
                                resolvedGroupID = groupRetrieved.GroupID;
                                resolvedGroupName = groupRetrieved.GroupName;
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(resolvedGroupName))
                            resolvedGroupName = "Ambiguous name. Found " + e.MatchedGroups.Count.ToString() +
                                                " groups (UUIDs on console)";
                    }
                }
                GetGroupsSearchEvent.Set();
            }
        }

        private void Groups_OnGroupJoined(object sender, GroupOperationEventArgs e)
        {
            WriteLine(Client.ToString() + (e.Success ? " joined " : " failed to join ") + e.GroupID.ToString());

            /* A.Biondi 
             * This code is not necessary because it is yet present in the 
             * GroupCommand.cs as well. So the new group will be activated by 
             * the mentioned command. If the GroupCommand.cs would change, 
             * just uncomment the following two lines.
                
            if (success)
            {
                DLRConsole.WriteLine(Client.ToString() + " setting " + groupID.ToString() + " as the active group");
                Client.Groups.ActivateGroup(groupID);
            }
                
            */

            joinedGroup = e.Success;
            GetGroupsSearchEvent.Set();
        }
    }
}