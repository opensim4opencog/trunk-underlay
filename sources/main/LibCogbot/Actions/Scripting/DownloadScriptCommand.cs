using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Cogbot;
using OpenMetaverse;
using OpenMetaverse.Assets;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Scripting
{
    public class DownloadScriptCommand : Command, GridMasterCommand
    {
        public DownloadScriptCommand(BotClient testClient)
        {
            Name = "Download Script";
        }

        public override void MakeInfo()
        {
            Description = "Downloads the specified stript. Usage: downloadscript <uuid|name> from your inventory";
            Category = CommandCategory.Inventory;
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (args.Length < 2) return ShowUsage();
            AssetType assetType;

            FieldInfo fi = typeof (AssetType).GetField(args[1], BindingFlags.IgnoreCase | BindingFlags.Static);
            if (fi == null)
            {
                int typeInt;
                if (int.TryParse(args[1], out typeInt))
                {
                    assetType = (AssetType) typeInt;
                }
                else
                {
                    WriteLine(typeof (AssetType).GetFields().ToString());
                    return Failure("Unknown asset type");
                }
            }
            else
            {
                assetType = (AssetType) fi.GetValue(null);
            }
            //var v = WorldObjects.uuidTypeObject;
            UUID AssetID = UUID.Zero;
            int argsUsed;
            UUIDTryParse(args, 0, out AssetID, out argsUsed);
            AutoResetEvent DownloadHandle = new AutoResetEvent(false);
            Client.Assets.RequestAsset(AssetID, assetType, true, delegate(AssetDownload transfer, Asset asset)
                                                                     {
                                                                         WriteLine("Status: " + transfer.Status +
                                                                                   "  Asset: " + asset);

                                                                         if (transfer.Success)
                                                                             try
                                                                             {
                                                                                 File.WriteAllBytes(
                                                                                     String.Format("{0}.{1}", AssetID,
                                                                                                   assetType.ToString().
                                                                                                       ToLower()),
                                                                                     asset.AssetData);
                                                                                 try
                                                                                 {
                                                                                     asset.Decode();
                                                                                     WriteLine("Asset decoded as " +
                                                                                               asset.AssetType);
                                                                                 }
                                                                                 catch (Exception ex)
                                                                                 {
                                                                                     WriteLine("Asset not decoded: " +
                                                                                               ex);
                                                                                 }
                                                                             }
                                                                             catch (Exception ex)
                                                                             {
                                                                                 Logger.Log(ex.Message,
                                                                                            Helpers.LogLevel.Error, ex);
                                                                             }
                                                                         DownloadHandle.Set();
                                                                     });
            DownloadHandle.WaitOne(30000);
            return Success("Done RequestAsset " + AssetID);
        }
    }
}