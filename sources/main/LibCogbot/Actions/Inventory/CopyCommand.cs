using System.Collections.Generic;
using Cogbot;
using Cogbot.World;
using OpenMetaverse;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Inventory
{
    public class CopyCommand : Cogbot.Actions.Command, RegionMasterCommand
    {
        public CopyCommand(BotClient client)
        {
            Name = "Copy";
        }

        public override void MakeInfo()
        {
            Description = "Copys from a prim.";
            Category = Cogbot.Actions.CommandCategory.Objects;
            Parameters = CreateParams("targets", typeof (PrimSpec), "The targets of " + Name);
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (args.Length == 0)
            {
                return ShowUsage();
            }
            int argsUsed;
            List<SimObject> PS = WorldSystem.GetPrimitives(args, out argsUsed);
            if (IsEmpty(PS)) return Failure("Cannot find objects from " + args.str);
            GridClient client = TheBotClient;
            foreach (var currentPrim in PS)
            {
                AddSuccess(Name + " on " + currentPrim);
                client.Inventory.RequestDeRezToInventory(currentPrim.LocalID, DeRezDestination.AgentInventoryCopy,
                                                         client.Inventory.FindFolderForType(AssetType.Object), UUID.Zero);
            }
            return SuccessOrFailure();
        }
    }
}