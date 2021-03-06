using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenMetaverse;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Land
{
    public class ParcelDetailsCommand : Command, RegionMasterCommand, AsynchronousCommand
    {
        public ParcelDetailsCommand(BotClient testClient)
        {
            Name = "parceldetails";
        }

        public override void MakeInfo()
        {
            Description = "Displays parcel details from the ParcelTracker dictionary. Usage: parceldetails parcelID";
            Category = CommandCategory.Parcel;
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (args.Length < 1)
                return ShowUsage(); // " parceldetails parcelID (use parcelinfo to get ID)";

            int parcelID;
            Parcel parcel;
            int argsUsed;
            Simulator CurSim = TryGetSim(args, out argsUsed) ?? Client.Network.CurrentSim;

            // test argument that is is a valid integer, then verify we have that parcel data stored in the dictionary
            if (Int32.TryParse(args[argsUsed], out parcelID) && CurSim.Parcels.TryGetValue(parcelID, out parcel))
            {
                // this request will update the parcels dictionary
                Client.Parcels.RequestParcelProperties(CurSim, parcelID, 0);

                // Use reflection to dynamically get the fields from the Parcel struct
                Type t = parcel.GetType();
                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);

                StringBuilder sb = new StringBuilder();
                foreach (FieldInfo field in fields)
                {
                    sb.AppendFormat("{0} = {1}" + Environment.NewLine, field.Name, field.GetValue(parcel));
                }
                return Success(sb.ToString());
            }
            else
            {
                return
                    Failure(
                        string.Format(
                            "Unable to find Parcel {0} in Parcels Dictionary, Did you run parcelinfo to populate the dictionary first?",
                            args[argsUsed]));
            }
        }
    }
}