using System.Collections.Generic;
using cogbot.Listeners;
using cogbot.TheOpenSims;
using OpenMetaverse;

namespace cogbot.Actions
{
    public class SelectObjectCommand : cogbot.Actions.Command
    {
        public SelectObjectCommand(BotClient client)
        {
            Name = "selectobject";
            Description = "Re select object [prim]";
            Category = cogbot.Actions.CommandCategory.Objects;
            Parameters = new[] {  new NamedParam(typeof(SimObject), typeof(UUID)) };
        }

        public override CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate WriteLine)
        {
            if (args.Length==0) {
                WorldObjects.ResetSelectedObjects();
                return Success("ResetSelectedObjects");
            }
            int used;
            List<Primitive> PS = WorldSystem.GetPrimitives(args, out used);
            if (IsEmpty(PS)) return Failure("Cannot find objects from " + string.Join(" ", args));
            foreach (var P in PS)
            {               
                WorldSystem.ReSelectObject(P);                
            }
            return Success("objects selected " + PS.Count);
        }
    }
}