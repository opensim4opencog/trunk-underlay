﻿using System;
using System.Collections.Generic;
using System.Threading;
using cogbot.Actions;
using cogbot.TheOpenSims;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using PathSystem3D.Navigation;
using RTParser;

namespace cogbot.Listeners
{

    public partial class WorldObjects : DebugAllEvents
    {
        public static readonly Dictionary<ulong, WorldObjects> SimMaster = new Dictionary<ulong, WorldObjects>();
        /// <summary>
        /// This is all bot simulator references: the Count would be Bots*Regions
        /// </summary>
        private static readonly List<Simulator> _AllSimulators = new List<Simulator>();
        public static WorldObjects Master;
        private static bool RequestedGridInfos = false;
        private bool IsConnected = false;
        private bool RegisterAllOnce = false;
        private readonly List<ulong> MasteringRegions = new List<ulong>();


        internal static List<Simulator> AllSimulators
        {
            get
            {
                List<Simulator> sims = null;

                lock (_AllSimulators)
                    sims = new List<Simulator>(_AllSimulators);

                return sims;
            }
        }

        public bool IsMaster(Simulator simulator)
        {
            return SimRegion.IsMaster(simulator, client);
        }

        public bool IsRegionMaster
        {
            get { return SimRegion.IsMaster(TheSimAvatar.GetSimulator(), client); }
        }



        public override void Parcels_OnSimParcelsDownloaded(Simulator simulator,
                                                            InternalDictionary<int, Parcel> simParcels, int[,] parcelMap)
        {
            //base.Parcels_OnSimParcelsDownloaded(simulator, simParcels, parcelMap);
        }

        public override void Network_OnConnected(object sender)
        {
            //Network_OnSimConnectedHook( (Simulator)sender);
            base.Network_OnConnected(sender);
            if (sender != client.gridClient)
            {
                throw new ArgumentException("wrong client " + sender);
            }
            //            RequestGridInfos();
        }

        private void RequestGridInfos(ulong regionHandle)
        {
            if (!RequestedGridInfos)
            {
                int Range = 0;
                RequestedGridInfos = true;
                uint X;
                uint Y;

                Utils.LongToUInts(regionHandle, out X, out Y);
                X /= 256;
                Y /= 256;
                if (X < 2) X = 2;
                else if (X > 65533) X = 65533;
                if (Y < 2) Y = 2;
                else if (Y > 65533) Y = 65533;
                client.Grid.RequestMapBlocks(GridLayerType.Objects, (ushort)(X - Range), (ushort)(Y - Range),
                                             (ushort)(X + Range), (ushort)(Y + Range), false);
                //client.Grid.RequestMainlandSims(GridLayerType.Objects);
                //  client.Grid.RequestMainlandSims(GridLayerType.Terrain);
                //   client.Grid.RequestMapLayer(GridLayerType.Objects);
                // client.Grid.RequestMapLayer(GridLayerType.Terrain);
            }
        }

        public void Network_OnSimConnectedHook(Simulator simulator)
        {
            base.Network_OnSimConnected(simulator);
            lock (WorldObjectsMasterLock)
            {
                EnsureSimulator(simulator);
                IsConnected = true;
                if (SimRegion.IsMaster(simulator, client.gridClient))
                {
                    Debug("---SIMMASTER---------" + client + " region: " + simulator);
                    SetWorldMaster(true);
                    //client.Grid.RequestMapRegion(simulator.Name, GridLayerType.Objects);
                    client.Grid.RequestMapRegion(simulator.Name, GridLayerType.Terrain);
                    //client.Grid.RequestMapRegion(simulator.Name, GridLayerType.LandForSale);
                    //client.Grid.RequestMapItems(simulator.Handle,OpenMetaverse.GridItemType.Classified,GridLayerType.Terrain);
                    MasteringRegions.Add(simulator.Handle);
                    RequestGridInfos(simulator.Handle);
                }
                else
                {
                    Debug("-----NOT SIMMASTER-------" + client + " region: " + simulator);
                    MasteringRegions.Remove(simulator.Handle);
                    if (MasteringRegions.Count == 0)
                    {
                        SetWorldMaster(false);
                        Debug("------UNREGISTERING------" + client);
                    }
                }
            }
        }

        public void CheckConnected(Simulator simulator)
        {
            if (!IsConnected)
            {
                Network_OnSimConnectedHook(simulator);
            }
        }

        public override void Network_OnEventQueueRunning(Simulator simulator)
        {
            base.Network_OnEventQueueRunning(simulator);
        }

        public override void Network_OnCurrentSimChanged(Simulator PreviousSimulator)
        {
            if (TheSimAvatar.GetSimulator() == PreviousSimulator)
            {
                Debug("TheSimAvatar._CurrentRegion.TheSimulator == PreviousSimulator " + PreviousSimulator);
            }
            base.Network_OnCurrentSimChanged(PreviousSimulator);
        }


        public void TrackPrim(Simulator[] sims, ulong regionHandle, uint parentID)
        {
           // GetSimulator()
        }

        public override void Network_OnSimDisconnected(Simulator simulator, NetworkManager.DisconnectType reason)
        {
            if (IsMaster(simulator))
            {
                Debug("SIM LOOSING ITS MASTER!" + this + " " + simulator);
            }
            if (TheSimAvatar.GetSimulator() == simulator)
            {
                Debug("TheSimAvatar._CurrentRegion.TheSimulator == simulator " + simulator);
            }
            base.Network_OnSimDisconnected(simulator, reason);
        }

        public override void Network_OnDisconnected(NetworkManager.DisconnectType reason, string message)
        {
            base.Network_OnDisconnected(reason, message);
        }

        public override void Grid_OnGridRegion(GridRegion region)
        {
            SimRegion R = SimRegion.GetRegion(region.RegionHandle);
            if (R != null)
                R.GridInfo = region;
            // base.Grid_OnGridRegion(region);
        }


        public override void Grid_OnRegionHandleReply(UUID regionID, ulong regionHandle)
        {
            RegisterUUID(regionID, SimRegion.GetRegion(regionHandle));
            base.Grid_OnRegionHandleReply(regionID, regionHandle);
        }


        public override void RegisterAll()
        {
            lock (WorldObjectsMasterLock)
            {
                if (!RegisterAllOnce)
                {
                    RegisterAllOnce = true;
                    base.RegisterAll();
                    /* client.Sound.OnAttachSound -= Sound_OnAttachSound;
                     client.Sound.OnAttachSoundGainChange -= Sound_OnAttachSoundGainChange;
                     client.Sound.OnSoundTrigger -= Sound_OnSoundTrigger;
                     client.Sound.OnPreloadSound -= Sound_OnPreloadSound;
                     */
                    // Viewer effect callback
                    client.Network.RegisterCallback(PacketType.ViewerEffect,
                                                    new NetworkManager.PacketCallback(ViewerEffectHandler));
                    // raises these events already
                    client.Avatars.OnPointAt -= Avatars_OnPointAt;
                    client.Avatars.OnLookAt -= Avatars_OnLookAt;
                    client.Avatars.OnEffect -= Avatars_OnEffect;
                    client.Assets.OnUploadProgress -= Assets_OnUploadProgress; // On-Upload-Progress
                    client.Self.OnCameraConstraint -= Self_OnCameraConstraint;
                    client.Settings.PIPELINE_REQUEST_TIMEOUT = 60000;

                    client.Objects.OnPrimitiveProperties += Objects_OnPrimitiveProperties;
                    client.Objects.OnPrimitiveUpdate += Objects_OnPrimitiveUpdate;
                    client.Objects.OnObjectUpdated -= Objects_OnObjectUpdated;
                }
            }
        }

        public override void UnregisterAll()
        {
            lock (WorldObjectsMasterLock)
            {
                if (RegisterAllOnce)
                {
                    RegisterAllOnce = false;
                    base.UnregisterAll();
                    client.Network.UnregisterCallback(PacketType.ViewerEffect,
                                new NetworkManager.PacketCallback(ViewerEffectHandler));

                    client.Objects.OnPrimitiveProperties -= Objects_OnPrimitiveProperties;
                    client.Objects.OnPrimitiveUpdate -= Objects_OnPrimitiveUpdate;
                    client.Objects.OnObjectUpdated -= Objects_OnObjectUpdated;
                    RegisterThinClient();
                }
            }
        }

        private void RegisterThinClient()
        {
            client.Network.OnLogin += Network_OnLogin;
            client.Network.OnConnected += Network_OnConnected;
            client.Network.OnLogoutReply += Network_OnLogoutReply;
            client.Network.OnSimConnecting += Network_OnSimConnecting;
            client.Network.OnSimConnected += Network_OnSimConnected;
            client.Network.OnSimDisconnected += Network_OnSimDisconnected;
            client.Network.OnDisconnected += Network_OnDisconnected;
            client.Network.OnCurrentSimChanged += Network_OnCurrentSimChanged;
            client.Network.OnEventQueueRunning += Network_OnEventQueueRunning;

            client.Self.OnChat += Self_OnChat;
            client.Self.OnScriptDialog += Self_OnScriptDialog;
            client.Self.OnScriptQuestion += Self_OnScriptQuestion;
            client.Self.OnLoadURL += Self_OnLoadURL;
            client.Self.OnInstantMessage += Self_OnInstantMessage;
            client.Self.OnTeleport += Self_OnTeleport;
            client.Self.OnBalanceUpdated += Self_OnBalanceUpdated;
            client.Self.OnMoneyBalanceReplyReceived += Self_OnMoneyBalanceReplyReceived;
            client.Self.OnAgentDataUpdated += Self_OnAgentDataUpdated;
            client.Self.OnAnimationsChanged += Self_OnAnimationsChanged;
            client.Self.OnMeanCollision += Self_OnMeanCollision;
            client.Self.OnRegionCrossed += Self_OnRegionCrossed;
            client.Self.OnGroupChatJoin += Self_OnGroupChatJoin;
            client.Self.OnGroupChatLeft += Self_OnGroupChatLeft;
            client.Self.OnAlertMessage += Self_OnAlertMessage;
            client.Self.OnScriptControlChange += Self_OnScriptControlChange;
            //client.Self.OnCameraConstraint += Self_OnCameraConstraint;
            client.Self.OnScriptSensorReply += Self_OnScriptSensorReply;
            client.Self.OnAvatarSitResponse += Self_OnAvatarSitResponse;
            client.Self.OnChatSessionMemberAdded += Self_OnChatSessionMemberAdded;
            client.Self.OnChatSessionMemberLeft += Self_OnChatSessionMemberLeft;
            client.Appearance.OnAgentWearables += Appearance_OnAgentWearables;
            client.Appearance.OnAppearanceUpdated += Appearance_OnAppearanceUpdated;
            client.Friends.OnFriendNamesReceived += Friends_OnFriendNamesReceived;
            client.Friends.OnFriendOnline += Friends_OnFriendOnline;
            client.Friends.OnFriendOffline += Friends_OnFriendOffline;
            client.Friends.OnFriendRights += Friends_OnFriendRights;
            client.Friends.OnFriendshipOffered += Friends_OnFriendshipOffered;
            client.Friends.OnFriendshipResponse += Friends_OnFriendshipResponse;
            client.Friends.OnFriendshipTerminated += Friends_OnFriendshipTerminated;
            client.Inventory.OnItemReceived += Inventory_OnItemReceived;
            client.Inventory.OnFolderUpdated += Inventory_OnFolderUpdated;
            client.Inventory.OnObjectOffered += Inventory_OnObjectOffered;
            client.Inventory.OnFindObjectByPath += Inventory_OnFindObjectByPath;
            client.Inventory.OnTaskItemReceived += Inventory_OnTaskItemReceived;


            // so we can find ourselves
            client.Objects.OnNewAvatar += Objects_OnNewAvatar;

            // just in case
            client.Network.OnSimConnected -= Network_OnSimConnectedHook;
            client.Inventory.OnScriptRunning -= Inventory_OnScriptRunning;
            // just in case twice
            client.Network.OnSimConnected -= Network_OnSimConnectedHook;
            client.Inventory.OnScriptRunning -= Inventory_OnScriptRunning;
            client.Network.OnSimConnected += Network_OnSimConnectedHook;
            client.Inventory.OnScriptRunning += Inventory_OnScriptRunning;
        }

        //public volatile static WorldObjects Master;
        public void SetWorldMaster(bool isMaster)
        {
            lock (WorldObjectsMasterLock)
            {
                if (isMaster) Master = this;
                if (MasteringRegions.Count > 0 && !isMaster) throw new ArgumentException("Cant un-master!");

                client.Settings.ALWAYS_DECODE_OBJECTS = isMaster;
                client.Settings.ALWAYS_REQUEST_OBJECTS = isMaster;
                client.Settings.ALWAYS_REQUEST_PARCEL_ACL = isMaster;
                client.Settings.ALWAYS_REQUEST_PARCEL_DWELL = isMaster;
                client.Settings.STORE_LAND_PATCHES = isMaster;
                client.Settings.OBJECT_TRACKING = isMaster;
                client.Settings.PARCEL_TRACKING = isMaster;

                // client.Settings.OBJECT_TRACKING = isMaster;

                if (isMaster) RegisterAll();
                else UnregisterAll();
            }
        }

        public void EnsureSimulator(Simulator simulator)
        {
            if (simulator == null) return;
            lock (_AllSimulators)
            {
                if (!_AllSimulators.Contains(simulator))
                    _AllSimulators.Add(simulator);
            }
        }


        public override void Terrain_OnLandPatch(Simulator simulator, int x, int y, int width, float[] data)
        {
            Console.Write(",");
            //SimRegion R = SimRegion.GetRegion(simulator);
            //base.Terrain_OnLandPatch(simulator, x, y, width, null);

            //throw new NotImplementedException();
            //   SendNewEvent("On-Land-Patch", x, y, width, BVHData);
            //            output("TextForm Terrain_OnLandPatch: "+simulator.ToString()+"/"+x.ToString()+"/"+y.ToString()+" w="+width.ToString());
        }

    }
}