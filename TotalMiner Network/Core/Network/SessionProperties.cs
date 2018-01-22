using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace TotalMiner_Network.Core.Network
{
    public class SessionProperties
    {
        public int SessionID;
        public short HostID;
        public NetworkSessionType NetType;

        public MapAttribute Attribute;
        public bool CombatEnabled;
        public int CurrentPlayerCount;
        public Permissions DefaultPermission;
        public int ExeVersion;
        public GameMode GameMode;
        public string HostName;
        public string MapName;
        public int ModsEnabledCount;
        public string OwnerName;
        public float RatingAvgStars;
        public int RatingsCount;
        public NetworkSessionState SessionState;
        public SessionType SessionType;
        public bool SkillsEnabled;
        public bool SkillsLocal;

        public void Write(BinaryWriter writer)
        {
            writer.Write(SessionID);
            writer.Write(HostID);
            writer.Write((byte)NetType);
            writer.Write((byte)SessionType);
            writer.Write((byte)SessionState);
            writer.Write(ExeVersion);
            writer.Write(MapName);
            writer.Write(OwnerName);
            writer.Write(HostName);
            writer.Write((byte)GameMode);
            writer.Write((byte)Attribute);
            writer.Write(CurrentPlayerCount);
            writer.Write(RatingAvgStars);
            writer.Write(SkillsEnabled);
            writer.Write(SkillsLocal);
            writer.Write(CombatEnabled);
            writer.Write((byte)DefaultPermission);
            writer.Write(ModsEnabledCount);
        }
        public static SessionProperties Read(BinaryReader reader)
        {
            SessionProperties toRet = new SessionProperties();
            toRet.SessionID = reader.ReadInt32();
            toRet.HostID = reader.ReadInt16();
            toRet.NetType = (NetworkSessionType)reader.ReadByte();
            toRet.SessionType = (SessionType)reader.ReadByte();
            toRet.SessionState = (NetworkSessionState)reader.ReadByte();
            toRet.ExeVersion = reader.ReadInt32();
            toRet.MapName = reader.ReadString();
            toRet.OwnerName = reader.ReadString();
            toRet.HostName = reader.ReadString();
            toRet.GameMode = (GameMode)reader.ReadByte();
            toRet.Attribute = (MapAttribute)reader.ReadByte();
            toRet.CurrentPlayerCount = reader.ReadInt32();
            toRet.RatingAvgStars = reader.ReadSingle();
            toRet.SkillsEnabled = reader.ReadBoolean();
            toRet.SkillsLocal = reader.ReadBoolean();
            toRet.CombatEnabled = reader.ReadBoolean();
            toRet.DefaultPermission = (Permissions)reader.ReadByte();
            toRet.ModsEnabledCount = reader.ReadInt32();
            return toRet;
        }
    }
}
