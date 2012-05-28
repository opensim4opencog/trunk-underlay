using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using cogbot.Listeners;
using cogbot.TheOpenSims;
using cogbot.Utilities;
using MushDLR223.Utilities;
using OpenMetaverse;
using PathSystem3D.Navigation;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.System
{
    public class SysVarCommand : cogbot.Actions.Command, BotSystemCommand
    {
        public SysVarCommand(BotClient client)
        {
            Name = "sysvar";
            Description = "Manipulates system variables." +
                          " These are global settings that affect Cogbot." +
                          "Many SysVars contain the word 'Maintain' (eg MaintainSounds). Generally this means" +
                          "Cogbot won't make a special request from the server to get information about this sort of thing" +
                          "and will provide information about it only if available" +
                          "For booleans anything but no or false (case insensitive) is true.";
            AddVersion(CreateParams(), "List all Sysvars and their settings");
            AddVersion(CreateParams(
                           "key", typeof (string), "substring to match sysvar names",
                           Optional("value", typeof (object), "value to set")),
                       "Show sysvars matching key if value is supplied it tried to set those values");

            Details = Example("sysvar CanUseSit True", "allow the bot to sit on things") +
                      Example("sysvar CanUseSit no", "don't allow the bot to sit on things") +
                      Example("sysvar Maintain false",
                              "set every sysvar that contains Maintain in it's name to false") +
                      Example("sysvar MaintainEffectsDistance 8.0",
                              "set the maximum distance to notice effects to 8.0") + SysVarHtml();


            Category = CommandCategory.BotClient;
        }

        public string SysVarHtml()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<table><tr><th>Variable Name</th><th>current value</th><th>Description</th></tr>");
            foreach (var sv in LockInfo.CopyOf(ScriptManager.SysVars))
            {
                ConfigSettingAttribute svv = sv.Value;
                sb.AppendLine(string.Format("<tr name=\"{0}\" id='{0}'><td>{0}</td><td>{1}</td><td>{2}</td></tr>", Htmlize.NoEnts(svv.Key), Htmlize.NoEnts("" + svv.Value), Htmlize.NoEnts(svv.Comments)));
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            int used = 0;
            var sysvars = LockInfo.CopyOf(ScriptManager.SysVars);
            if (args.Length == 0)
            {
                foreach (var sv in LockInfo.CopyOf(ScriptManager.SysVars))
                {
                    ConfigSettingAttribute svv = sv.Value;
                    WriteLine(string.Format("{0}={1} //{2}", (svv.Key), svv.Value, svv.Comments));
                }
                return Success("count=" + ScriptManager.SysVars.Count);
            }
            List<ConfigSettingAttribute> setThese = new List<ConfigSettingAttribute>();
            int found = 0;
            string find = args[0].ToLower();
            foreach (var sv in sysvars)
            {
                var svv = sv.Value;
                if (svv.Finds(find))
                {
                    found++;
                    WriteLine("" + svv.DebugInfo);
                    setThese.Add(svv);
                }
            }
            if (args.Length == 1)
            {
                return Success("Found sysvars: " + found);
            }
            int changed = 0;
            foreach(var one in setThese)
            {
                try
                {
                    one.Value = args[1];
                    Success("Set sysvar: " + one.Key + " to " + one.Value);
                    changed++;
                } catch(Exception e)
                {
                    
                }
            }
            return Success("set vars = " + changed);
        }
    }
}