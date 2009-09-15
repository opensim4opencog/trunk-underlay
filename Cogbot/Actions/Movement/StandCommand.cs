using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace cogbot.Actions
{
    public class StandCommand: Command
    {
        public StandCommand(BotClient testClient)
	{
		Name = "Stand";
		Description = "Stand";
        Category = CommandCategory.Movement;
	}
	
        public override string Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
	    {
            Client.Self.Stand();
		    return "Standing up.";  
	    }
    }
}
