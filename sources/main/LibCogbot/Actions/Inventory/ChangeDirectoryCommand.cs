using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Inventory.Shell
{
    public class ChangeDirectoryCommand : Command, BotPersonalCommand
    {
        private InventoryManager Manager;
        private OpenMetaverse.Inventory Inventory;

        public ChangeDirectoryCommand(BotClient client)
        {
            Name = "cd";
        }

        public override void MakeInfo()
        {
            Description = "Changes the current working inventory folder.";
            Category = CommandCategory.Inventory;
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            Manager = Client.Inventory;
            Inventory = Client.Inventory.Store;

            if (args.Length > 1)
                return ShowUsage(); // " cd [path-to-folder]";
            string pathStr = "";
            string[] path = null;
            if (args.Length == 0)
            {
                path = new string[] {""};
                // cd without any arguments doesn't do anything.
            }
            else if (args.Length == 1)
            {
                pathStr = args[0];
                path = pathStr.Split(new char[] {'/'});
                // Use '/' as a path seperator.
            }
            InventoryFolder currentFolder = Client.CurrentDirectory;
            if (pathStr.StartsWith("/"))
                currentFolder = Inventory.RootFolder;

            if (currentFolder == null) // We need this to be set to something. 
                return Failure("Error: Client not logged in.");

            // Traverse the path, looking for the 
            for (int i = 0; i < path.Length; ++i)
            {
                string nextName = path[i];
                if (string.IsNullOrEmpty(nextName) || nextName == ".")
                    continue; // Ignore '.' and blanks, stay in the current directory.
                if (nextName == ".." && currentFolder != Inventory.RootFolder)
                {
                    // If we encounter .., move to the Client folder.
                    currentFolder = Inventory[currentFolder.ParentUUID] as InventoryFolder;
                }
                else
                {
                    List<InventoryBase> currentContents = Inventory.GetContents(currentFolder);
                    // Try and find an InventoryBase with the corresponding name.
                    bool found = false;
                    foreach (InventoryBase item in currentContents)
                    {
                        // Allow lookup by UUID as well as name:
                        if (item.Name == nextName || item.UUID.ToString() == nextName)
                        {
                            found = true;
                            if (item is InventoryFolder)
                            {
                                currentFolder = item as InventoryFolder;
                            }
                            else
                            {
                                return Failure(item.Name + " is not a folder.");
                            }
                        }
                    }
                    if (!found)
                        return Failure(nextName + " not found in " + currentFolder.Name);
                }
            }
            Client.CurrentDirectory = currentFolder;
            return Success("Current folder: " + currentFolder.Name);
        }
    }
}