using System;
using System.Collections.Generic;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Packets;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Voice
{
    public class ParcelVoiceInfoCommand : Command, RegionMasterCommand, AsynchronousCommand
    {
        private AutoResetEvent ParcelVoiceInfoEvent = new AutoResetEvent(false);
        private string VoiceRegionName = null;
        private int VoiceLocalID = -1;
        private string VoiceChannelURI = null;

        public ParcelVoiceInfoCommand(BotClient testClient)
        {
            Name = "voiceparcel";
            TheBotClient = testClient;
        }

        public override void MakeInfo()
        {
            Description = "obtain parcel voice info. Usage: voiceparcel";
            Category = CommandCategory.Parcel;
            Parameters = CreateParams();
        }

        private bool registered = false;

        private bool IsVoiceManagerRunning()
        {
            BotClient Client = TheBotClient;
            if (null == Client.VoiceManager) return false;

            if (!registered)
            {
                Client.VoiceManager.OnParcelVoiceInfo += Voice_OnParcelVoiceInfo;
                registered = true;
            }
            return true;
        }


        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            BotClient Client = TheBotClient;
            if (!IsVoiceManagerRunning())
                return Failure(String.Format("VoiceManager not running for {0}", Client));

            if (!Client.VoiceManager.RequestParcelVoiceInfo())
            {
                return Failure("RequestParcelVoiceInfo failed. Not available for the current grid?");
            }
            ParcelVoiceInfoEvent.WaitOne(30*1000, false);

            if (String.IsNullOrEmpty(VoiceRegionName) && -1 == VoiceLocalID)
            {
                return Failure(String.Format("Parcel Voice Info request for {0} failed.", Client.Self.Name));
            }

            return
                Success(
                    string.Format(
                        "Parcel Voice Info request for {0}: region name \"{1}\", parcel local id {2}, channel URI {3}",
                        Client.Self.Name, VoiceRegionName, VoiceLocalID, VoiceChannelURI));
        }

        private void Voice_OnParcelVoiceInfo(string regionName, int localID, string channelURI)
        {
            VoiceRegionName = regionName;
            VoiceLocalID = localID;
            VoiceChannelURI = channelURI;

            ParcelVoiceInfoEvent.Set();
        }
    }
}