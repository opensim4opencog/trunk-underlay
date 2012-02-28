using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.IO;
using cogbot.Actions.SimExport;
using cogbot.Listeners;
using cogbot.TheOpenSims;
using MushDLR223.Utilities;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.StructuredData;

using MushDLR223.ScriptEngines;

namespace cogbot.Actions.SimExport
{
    public partial class ImportCommand 
    {
        public class UserOrGroupMapping : UUIDChange
        {
            public bool IsGroup = false;
            public string OldName;
            public string NewName;
            public UserOrGroupMapping(UUID id, String name, bool isGroup)
            {
                OldID = id;
                IsGroup = isGroup;
                OldName = name;
            }

        }
    }
}
