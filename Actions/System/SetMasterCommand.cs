using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace cogbot.Actions
{
    public class SetMasterCommand: Command
    {
		public DateTime Created = DateTime.Now;
        private UUID resolvedMasterKey = UUID.Zero;
        private ManualResetEvent keyResolution = new ManualResetEvent(false);
        private UUID query = UUID.Zero;

        public SetMasterCommand(cogbot.TextForm testClient)
		{
			Name = "setmaster";
            Description = "Sets the user name of the master user. The master user can IM to run commands. Usage: setmaster [name]";
            Category = CommandCategory.TestClient;
		}

        public override string Execute(string[] args, UUID fromAgentID)
		{
			string masterName = String.Empty;
			for (int ct = 0; ct < args.Length;ct++)
				masterName = masterName + args[ct] + " ";
            masterName = masterName.TrimEnd();

            if (masterName.Length == 0)
                return "Usage: setmaster [name]";

            DirectoryManager.DirPeopleReplyCallback callback = new DirectoryManager.DirPeopleReplyCallback(KeyResolvHandler);
            client.Directory.OnDirPeopleReply += callback;

            query = client.Directory.StartPeopleSearch(DirectoryManager.DirFindFlags.People, masterName, 0);

            if (keyResolution.WaitOne(TimeSpan.FromMinutes(1), false))
            {
                parent.MasterKey = resolvedMasterKey;
                keyResolution.Reset();
                client.Directory.OnDirPeopleReply -= callback;
            }
            else
            {
                keyResolution.Reset();
                client.Directory.OnDirPeopleReply -= callback;
                return "Unable to obtain UUID for \"" + masterName + "\". Master unchanged.";
            }
            
            // Send an Online-only IM to the new master
            client.Self.InstantMessage(
                parent.MasterKey, "You are now my master.  IM me with \"help\" for a command list.");

            return String.Format("Master set to {0} ({1})", masterName, parent.MasterKey.ToString());
		}

        private void KeyResolvHandler(UUID queryid, List<DirectoryManager.AgentSearchData> matches)
        {
            if (query != queryid)
                return;

            resolvedMasterKey = matches[0].AgentID;
            keyResolution.Set();
            query = UUID.Zero;
        }
    }
}
