using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using cogbot.Listeners;
using System.Threading;
using cogbot.TheOpenSims.Navigation;
using cogbot.TheOpenSims.Navigation.Debug;
using Quaternion = OpenMetaverse.Quaternion;
using IrrlichtNETCP;
using TheOpenSims.Navagation.Mesher;

namespace cogbot.TheOpenSims
{


    //TheSims-like object
    public class SimObject : BotMentalAspect, SimPosition
    {
        public bool IsPassable
        {
            get
            {
                if (IsPhantom) return true;
                if (IsTypeOf(SimTypeSystem.PASSABLE) != null) return true;
                if (IsRoot() || true) return false;
                if (GetParent() == null) return true;
                return GetParent().IsPassable;
            }
        }
        public bool IsPhantom
        {
            get
            {
                if (MadePhantom) return true;
                if (IsRoot() || true) return (thePrim.Flags & PrimFlags.Phantom) == PrimFlags.Phantom;
                if (GetParent() == null) return true;
                return GetParent().IsPhantom;
            }
            set
            {
                if (IsPhantom == value) return;
                if (value)
                {
                    WorldSystem.SetPrimFlags(thePrim, (PrimFlags)(thePrim.Flags | PrimFlags.Phantom));
                    MadePhantom = true;
                }
                else
                {
                    WorldSystem.SetPrimFlags(thePrim, (PrimFlags)(thePrim.Flags - PrimFlags.Phantom));
                    MadePhantom = false;
                }

            }
        }

        public bool IsPhysical
        {
            get
            {
                if (!IsRoot()) return GetParent().IsPhysical;
                return (thePrim.Flags & PrimFlags.Physics) == PrimFlags.Physics;
            }
            set
            {
                if (IsPhysical == value) return;
                if (value)
                {
                    WorldSystem.SetPrimFlags(thePrim, (PrimFlags)(thePrim.Flags | PrimFlags.Physics));
                    MadeNonPhysical = false;
                }
                else
                {
                    WorldSystem.SetPrimFlags(thePrim, (PrimFlags)(thePrim.Flags - PrimFlags.Physics));
                    MadeNonPhysical = true;
                }

            }
        }

        public virtual SimWaypoint GetWaypoint()
        {
            Vector3 v3 = GetSimPosition();
            SimWaypoint swp = WorldSystem.SimPaths.CreateClosestWaypointBox(v3, GetSizeDistance() + 1, 7, 1.0f);
            float dist = Vector3.Distance(v3, swp.GetSimPosition());
            if (!swp.Passable)
            {
                WorldSystem.output("CreateClosestWaypoint: " + v3 + " <- " + dist + " -> " + swp + " " + this);
            }
            return swp;
            //            return WorldSystem.SimPaths.CreateClosestWaypointBox(v3, 4f);
        }

        public SimRoute[] GetRouteList(SimWaypoint to, out bool IsFake)
        {
            SimWaypoint from = this.GetWaypoint();
            if (GraphFormer.DEBUGGER != null)
            {
                GraphFormer.DEBUGGER.SetTryPathNow(from, to);
                GraphFormer.DEBUGGER.Invalidate();
            }
            return WorldSystem.SimPaths.GetRoute(from, to, out IsFake);
        }


        public float Distance(SimPosition prim)
        {
            if (!prim.CanGetSimPosition()) return 1300;
            if (!CanGetSimPosition()) return 1300;
            return Vector3.Distance(GetSimPosition(), prim.GetSimPosition());
        }

        readonly public Primitive thePrim; // the prim in Secondlife
        readonly public SimObjectType ObjectType;
        public WorldObjects WorldSystem;
        bool MadeNonPhysical = false;
        bool MadePhantom = false;
        bool needUpdate = true;


        public SimObjectType IsTypeOf(SimObjectType superType)
        {
            return ObjectType.IsSubType(superType);
        }

        public ListAsSet<SimObject> AttachedChildren = new ListAsSet<SimObject>();

        public ListAsSet<SimObject> GetChildren()
        {
            if (AttachedChildren.Count == 0)
            {
            }
            return AttachedChildren;
        }

        /// <summary>
        /// the bonus or handicap the object has compared to the defination 
        /// (more expensive chair might have more effect)
        /// </summary>
        public float scaleOnNeeds = 1.11F;

        public SimObject(Primitive prim, WorldObjects objectSystem)
            : base(prim.ID.ToString())
        {
            thePrim = prim;
            WorldSystem = objectSystem;
            ObjectType = SimTypeSystem.GetObjectType(prim.ID.ToString());
            UpdateProperties(thePrim.Properties);
            // GetParent(); // at least request it
        }

        SimObject Parent = null; // null means unknown if we IsRoot then Parent == this;

        public virtual SimObject GetParent()
        {
            if (Parent == null)
            {
                uint parent = thePrim.ParentID;
                if (parent != 0)
                {
                    Primitive prim = WorldSystem.GetPrimitive(parent);
                    if (prim == null)
                    {
                        // try to request for next time
                        WorldSystem.EnsureSelected(parent);
                        return null;
                    }
                    Parent = WorldSystem.GetSimObject(prim);
                    Parent.AddChild(this);
                }
                else
                {
                    Parent = this;
                }
            }
            return Parent;
        }

        public bool AddChild(SimObject simObject)
        {
            needUpdate = true;
            simObject.Parent = this;
            simObject.needUpdate = true;
            bool b = AttachedChildren.AddTo(simObject);
            if (b)
            {
                if (!IsTyped())
                {// borrow from child?
                    // UpdateProperties(simObject.thePrim.Properties);
                }
            }
            return b;

        }

        public bool IsTyped()
        {
            return ObjectType.IsComplete();
        }

        public virtual bool IsRoot()
        {
            if (thePrim.ParentID == 0) return true;
            GetParent();
            return false;
        }

        public virtual string DebugInfo()
        {
            string str = ToString();
            if (thePrim.ParentID != 0)
                return thePrim.ParentID + " " + str;
            return str;
        }

        public float RateIt(BotNeeds needs)
        {
            return ObjectType.RateIt(needs, GetBestUse(needs)) * scaleOnNeeds;
        }

        public IList<SimTypeUsage> GetTypeUsages()
        {
            return ObjectType.GetTypeUsages();
        }

        public List<SimObjectUsage> GetUsages()
        {
            List<SimObjectUsage> uses = new List<SimObjectUsage>();
            if (needUpdate)
            {
                UpdateProperties(thePrim.Properties);
            }
            foreach (SimTypeUsage typeUse in ObjectType.GetTypeUsages())
            {
                uses.Add(new SimObjectUsage(typeUse, this));
            }
            return uses;
        }

        public List<string> GetMenu(SimAvatar avatar)
        {
            //props.Permissions = new Permissions(objectData.BaseMask, objectData.EveryoneMask, objectData.GroupMask,
            //  objectData.NextOwnerMask, objectData.OwnerMask);
            List<string> list = new List<string>();
            if (thePrim.Properties != null)
            {
                //  if (thePrim.Properties.TextName != "")
                list.Add("grab");
                //   if (thePrim.Properties.SitName != "")
                list.Add("sit");
                PermissionMask mask = thePrim.Properties.Permissions.EveryoneMask;
                if (thePrim.OwnerID == avatar.theAvatar.ID) { mask = thePrim.Properties.Permissions.OwnerMask; }
                PermissionMask result = mask | thePrim.Properties.Permissions.BaseMask;
                if ((result & PermissionMask.Copy) != 0)
                    list.Add("copy");
                if ((result & PermissionMask.Modify) != 0)
                    list.Add("modify");
                if ((result & PermissionMask.Move) != 0)
                    list.Add("move");
                if ((result & PermissionMask.Transfer) != 0)
                    list.Add("transfer");
                if ((result & PermissionMask.Damage) != 0)
                    list.Add("damage");
            }
            return list;
        }

        public void UpdateProperties(Primitive.ObjectProperties objectProperties)
        {
            if (objectProperties != null)
            {
                ObjectType.SitName = objectProperties.SitName;
                ObjectType.TouchName = objectProperties.TouchName;
                needUpdate = false;
            }
            try
            {
                //  GetParent();
                AddSuperTypes(SimTypeSystem.GuessSimObjectTypes(objectProperties));
            }
            catch (Exception e)
            {
                Debug("" + e);
            }

        }

        public virtual bool IsFloating
        {
            get
            {
                return !IsPhysical;
            }
            set
            {
                IsPhysical = !value;
            }
        }

        public void UpdateObject(ObjectUpdate objectUpdate)
        {
        }

        private void AddSuperTypes(IList<SimObjectType> listAsSet)
        {
            //SimObjectType UNKNOWN = SimObjectType.UNKNOWN;
            foreach (SimObjectType type in listAsSet)
            {
                ObjectType.AddSuperType(type);
            }
        }

        public virtual bool RestoreEnterable()
        {
            bool changed = false;
            PrimFlags tempFlags = thePrim.Flags;
            if (MadePhantom && (tempFlags & PrimFlags.Phantom) != PrimFlags.Phantom)
            {
                WorldSystem.client.Self.Touch(thePrim.LocalID);
                tempFlags -= PrimFlags.Phantom;
                changed = true;
                MadePhantom = false;
            }
            if (MadeNonPhysical && (tempFlags & PrimFlags.Physics) == 0)
            {
                tempFlags |= PrimFlags.Physics;
                changed = true;
                MadeNonPhysical = false;
            }
            if (changed) WorldSystem.SetPrimFlags(thePrim, tempFlags);
            if (!IsRoot())
            {
                if (GetParent().RestoreEnterable()) return true;
            }
            return changed;
        }

        public virtual bool MakeEnterable()
        {
            bool changed = false;
            PrimFlags tempFlags = thePrim.Flags;
            if ((tempFlags & PrimFlags.Phantom) == 0)
            {
                WorldSystem.client.Self.Touch(thePrim.LocalID);
                tempFlags |= PrimFlags.Phantom;
                changed = true;
                MadePhantom = true;
            }
            if ((tempFlags & PrimFlags.Physics) != 0)
            {
                tempFlags -= PrimFlags.Physics;
                changed = true;
                MadeNonPhysical = true;
            }
            if (changed) WorldSystem.SetPrimFlags(thePrim, tempFlags);
            if (!IsRoot())
            {
                if (GetParent().MakeEnterable()) return true;
            }
            return changed;

        }

        public override string ToString()
        {
            String str = thePrim.ToString() + " ";
            if (thePrim.Properties != null)
            {
                if (!String.IsNullOrEmpty(thePrim.Properties.Name))
                    str += thePrim.Properties.Name + " ";
                if (!String.IsNullOrEmpty(thePrim.Properties.Description))
                    str += " | " + thePrim.Properties.Description + " ";
            }
            if (!String.IsNullOrEmpty(thePrim.Text))
                str += " | " + thePrim.Text + " ";
            uint ParentId = thePrim.ParentID;
            if (ParentId != 0)
            {
                str += " (parent ";
                Primitive pp = WorldSystem.GetPrimitive(ParentId);
                if (pp != null)
                {
                    str += WorldSystem.GetPrimTypeName(pp) + " " + pp.ID.ToString().Substring(0, 8);
                }
                else
                {
                    str += ParentId;
                }
                str += ") ";
            }
            if (AttachedChildren.Count > 0)
            {
                str += " (childs " + AttachedChildren.Count + ") ";
            }
            else
            {
                str += " (ch0) ";
            }
            str += " (size " + GetSizeDistance() + ") ";
            str += SuperTypeString();
            if (thePrim.Sound != UUID.Zero)
                str += "(Audible)";
            return str.Replace("  ", " ").Replace(") (", ")(");
        }

        private string SuperTypeString()
        {
            String str = "[";
            ObjectType.SuperType.ForEach(delegate(SimObjectType item)
            {
                str += item.GetTypeName() + " ";
            });
            return str.TrimEnd() + "]";
        }

        public bool CanGetSimPosition()
        {
            if (IsRoot()) return true;
            Primitive pUse = WorldSystem.GetPrimitive(thePrim.ParentID);
            if (pUse == null)
            {
                WorldSystem.EnsureSelected(thePrim.ParentID);
                return false;
            }
            return GetParent().CanGetSimPosition();
        }

        public virtual Vector3 GetSimPosition()
        {
            Primitive theLPrim = thePrim;
            Vector3 theLPos = theLPrim.Position;
            while (theLPrim.ParentID != 0)
            {
                uint theLPrimParentID = theLPrim.ParentID;
                theLPrim = WorldSystem.GetPrimitive(theLPrimParentID);
                while (theLPrim == null)
                {
                    Thread.Sleep(100);
                    theLPrim = WorldSystem.RequestMissingObject(theLPrimParentID);
                }
                theLPos = theLPos + theLPrim.Position;
            }
            if (BadLocation(theLPos))
            {
                Debug("-------------------------" + this + " shouldnt be at " + theLPos);
                WorldSystem.DeletePrim(thePrim);
            }
            return theLPos;
        }

        private bool BadLocation(Vector3 theLPos)
        {
            if (theLPos.Z < 0.0f) return true;
            if (theLPos.X < 0.0f) return true;
            if (theLPos.X > 255.0f) return true;
            if (theLPos.Y < 0.0f) return true;
            if (theLPos.Y > 255.0f) return true;
            return false;
        }


        public virtual OpenMetaverse.Quaternion GetSimRotation()
        {
            Primitive theLPrim = thePrim;
            Quaternion theLPos = theLPrim.Rotation;
            while (theLPrim.ParentID != 0)
            {
                uint theLPrimParentID = theLPrim.ParentID;
                theLPrim = WorldSystem.GetPrimitive(theLPrimParentID);
                while (theLPrim == null)
                {
                    Thread.Sleep(100);
                    theLPrim = WorldSystem.RequestMissingObject(theLPrimParentID);
                }
                theLPos = theLPos + theLPrim.Rotation;
                theLPos.Normalize();
            }
            return theLPos;
        }

        public BotNeeds GetActualUpdate(string pUse)
        {
            if (needUpdate)
            {
                UpdateProperties(thePrim.Properties);
            }
            return ObjectType.GetUsageActual(pUse).Magnify(scaleOnNeeds);
        }


        public SimTypeUsage GetBestUse(BotNeeds needs)
        {
            if (needUpdate)
            {
                UpdateProperties(thePrim.Properties);
            }

            IList<SimTypeUsage> all = ObjectType.GetTypeUsages();
            if (all.Count == 0) return null;
            SimTypeUsage typeUsage = all[0];
            float typeUsageRating = 0.0f;
            foreach (SimTypeUsage use in all)
            {
                float f = ObjectType.RateIt(needs, use);
                if (f > typeUsageRating)
                {
                    typeUsageRating = f;
                    typeUsage = use;
                }
            }
            return typeUsage;
        }

        public Vector3 GetUsePosition()
        {
            return GetSimPosition();
        }

        internal BotNeeds GetProposedUpdate(string pUse)
        {
            return ObjectType.GetUsagePromise(pUse).Magnify(scaleOnNeeds);
        }

        /// <summary>
        ///  Gets the distance a SimAvatar may be from SimObject to use
        /// </summary>
        /// <returns>1-255</returns>
        public virtual float GetSizeDistance()
        {
            float size = 1;

            float fx = thePrim.Scale.X;
            if (fx > size) size = fx;
            float fy = thePrim.Scale.Y;
            if (fy > size) size = fy;

            foreach (SimObject obj in AttachedChildren)
            {
                Primitive cp = obj.thePrim;
                fx = cp.Scale.X;
                if (fx > size) size = fx;
                fy = cp.Scale.Y;
                if (fy > size) size = fy;
            }
            return size;
        }

        public List<SimObject> GetNearByObjects(float maxDistance, bool rootOnly)
        {
            if (!CanGetSimPosition())
            {
                List<SimObject> objs = new List<SimObject>();
                GetParent();
                if (Parent != null && Parent != this)
                {
                    objs.Add(Parent);
                }
                return objs;
            }
            List<SimObject> objs2 = GetNearByObjects(GetSimPosition(), WorldSystem, this, maxDistance, rootOnly);
            SortByDistance(objs2);
            return objs2;
        }

        //static ListAsSet<SimObject> CopyObjects(List<SimObject> objects)
        //{
        //    ListAsSet<SimObject> KnowsAboutList = new ListAsSet<SimObject>();
        //    lock (objects) foreach (SimObject obj in objects)
        //        {
        //            KnowsAboutList.Add(obj);
        //        }
        //    return KnowsAboutList;
        //}

        internal static List<SimObject> GetNearByObjects(Vector3 here, WorldObjects WorldSystem, object thiz, float pUse, bool rootOnly)
        {
            List<SimObject> nearby = new List<SimObject>();
            foreach (SimObject obj in WorldSystem.GetAllSimObjects())
            {
                if (!(rootOnly && !obj.IsRoot() && !obj.IsTyped()))
                    if (obj != thiz && obj.CanGetSimPosition() && Vector3.Distance(obj.GetSimPosition(), here) <= pUse)
                        nearby.Add(obj);
            };
            return nearby;
        }

        public virtual bool Matches(string name)
        {
            return SimTypeSystem.MatchString(ToString(), name);
        }
        public virtual void Debug(string p)
        {
            WorldSystem.output(thePrim + ":" + p);
        }

        internal void SortByDistance(List<SimObject> sortme)
        {
            lock (sortme) sortme.Sort(CompareDistance);
        }

        public int CompareDistance(SimObject p1, SimObject p2)
        {
            return (int)(Distance(p1) - Distance(p2));
        }

        public int CompareDistance(Vector3 v1, Vector3 v2)
        {
            Vector3 rp = GetSimPosition();
            return (int)(Vector3.Mag(rp - v1) - Vector3.Mag(rp - v2));
        }

        public string DistanceVectorString(SimPosition obj)
        {
            String str;
            Vector3 loc;
            if (!obj.CanGetSimPosition())
            {
                str = "unknown relative ";
                loc = obj.GetUsePosition();
            }
            else
            {
                loc = obj.GetSimPosition();
                float dist = Vector3.Distance(GetSimPosition(), loc);
                if (dist == float.NaN)
                {
                    throw new InvalidCastException("NaN is not a number");
                }
                str = String.Format("{0:0.00}m ", dist);
            }
            return str + String.Format("<{0:0.00}, {1:0.00}, {2:0.00}>", loc.X, loc.Y, loc.Z);
        }

        public virtual string GetName()
        {
            if (thePrim.Properties != null)
            {
                String s = thePrim.Properties.Name;
                if (s.Length > 8) return s;
                s += " | " + thePrim.Properties.Description;
                if (s.Length > 12) return s;
            }
            return ToString();
        }

        /// <summary>
        /// </summary>
        /// <param name="ZSlice"> right now ussually 22 or 23</param>
        /// <returns>ICollection&lt;Vector3&gt; probly could be Vector2s but takes more time to wrap them</returns>
        public virtual ICollection<Vector3> GetOccupied(float min,float max)
        {
            List<Vector3> copy = new List<Vector3>();
            foreach (Vector3 point in GetPointsList())
            {
                if (point.Z > max || point.Z < min) continue;
                copy.Add(point);
            }
            return copy;
        }

        Mesh mesh;
        List<Vector3> PointsOccupied = null;
        internal ICollection<Vector3> GetPointsList()
        {
            if (PointsOccupied == null)
            {
                PointsOccupied = new List<Vector3>();
                mesh = PrimMesherG.PrimitiveToIrrMesh(thePrim, LevelOfDetail.High,GetSimRotation());
                if (mesh == null)
                {
                    List<Vector3> temp = new List<Vector3>();
                    temp.Add(GetSimPosition());
                    return temp;
                }
                                   int count = mesh.MeshBufferCount;
                for (int b = 0; b < count; b++)
                {
                    foreach (Vector3 wp in BoxToPoints(mesh.GetMeshBuffer(b).BoundingBox, GetSimPosition(), SimPathStore.StepSize))
                    {
                        if (!PointsOccupied.Contains(wp))
                        {
                            PointsOccupied.Add(wp);
                        }
                    }
                }
            }
            return PointsOccupied;
        }


        static List<Vector3> BoxToPoints(Box3D box, Vector3 loc, float StepLevel)
        {
            List<Vector3> PointsOccupied = new List<Vector3>();
            Vector3D min = box.MinEdge;
            Vector3D max = box.MaxEdge;
            float detailLevel = (float)(1f / StepLevel);
            float minX = (float)(Math.Round((double)min.X * detailLevel) / detailLevel);
            float minY = (float)(Math.Round((double)min.Y * detailLevel) / detailLevel);
            float minZ = (float)(Math.Round((double)min.Z * detailLevel) / detailLevel);
            float maxX = (float)(Math.Round((double)max.X * detailLevel) / detailLevel);
            float maxY = (float)(Math.Round((double)max.Y * detailLevel) / detailLevel);
            float maxZ = (float)(Math.Round((double)max.Z * detailLevel) / detailLevel);
            if (maxX < minX || maxZ < minZ || maxY < minY)
            {
                throw new ArgumentException("is box3d.MinEdge and box3d.MaxEdge in the TopLeft-to-BottemRight Order? " + min + " > " + max);
            }
            for (float x = minX; x <= maxX; x += StepLevel)
            {
                for (float y = minY; y <= maxY; y += StepLevel)
                {
                    for (float z = minZ; z <= maxZ; z += StepLevel)
                    {
                        Vector3 v3 = SimWaypoint.RoundPoint( new Vector3(x + loc.X, y + loc.Y, z + loc.Z));
                        PointsOccupied.Add(v3);
                    }
                }
            }
            return PointsOccupied;
        }

        //internal SimMesh theMesh;
        //public SimMesh GetSimMesh()
        //{
        //    if (theMesh == null)
        //    {
        //        theMesh = new SimMesh(this);
        //    }
        //    return theMesh;
        //}

    }
}
