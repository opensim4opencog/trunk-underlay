using System;
using System.Collections.Generic;
using Cogbot.World;
using OpenMetaverse;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Objects
{
    public class ObjectInventoryCommand : Command, RegionMasterCommand, AsynchronousCommand
    {
        public ObjectInventoryCommand(BotClient testClient)
        {
            Name = "objectinventory";
        }

        public override void MakeInfo()
        {
            Description =
                "Retrieves a listing of items inside an object (task inventory). Usage: objectinventory [objectID]";
            Category = CommandCategory.Inventory;
            Parameters = CreateParams("targets", typeof (PrimSpec), "The targets of " + Name);
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (args.Length < 1)
                return ShowUsage(); // " objectinventory [objectID]";

            int argsUsed;
            List<SimObject> PS;
            if (!args.TryGetValue("targets", out PS) || IsEmpty(PS))
            {
                PS = WorldSystem.GetAllSimObjects();
            } 
            foreach (var found in PS)
            {
                uint objectLocalID = found.LocalID;
                UUID objectID = found.ID;

                List<InventoryBase> items = Client.Inventory.GetTaskInventory(objectID, objectLocalID, 1000*30);

                if (items != null)
                {
                    string result = String.Empty;

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i] is InventoryFolder)
                        {
                            result += String.Format("[Folder] Name: {0}", items[i].Name) + Environment.NewLine;
                        }
                        else
                        {
                            InventoryItem item = (InventoryItem) items[i];
                            result += String.Format("[Item] Name: {0} Desc: {1} Type: {2}", item.Name, item.Description,
                                                    item.AssetType) + Environment.NewLine;
                        }
                    }

                    AddSuccess(result);
                }
                else
                {
                    Failure("failed to download task inventory for " + objectLocalID);
                }
            }
            return SuccessOrFailure();
        }
    }
}