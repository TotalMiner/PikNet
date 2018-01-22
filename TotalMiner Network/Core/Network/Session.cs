using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using TotalMiner_Network.Extensions;
using TotalMiner_Network.Core.Data;
using TotalMiner_Network.Core.Classes;
namespace TotalMiner_Network.Core.Network
{
    public class Session
    {
        #region Threading Vars
        private EventWaitHandle WaitHandler = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
        private Thread RunThread;
        private bool DoRunThread;
        #endregion

        #region Connection Vars
        public static IPAddress IP = IPAddress.Any;
        public static int Port = 5786;
        #endregion

        #region Session Detail Vars
        public SessionProperties Properties;
        #endregion

        #region Session Vars
        public bool SessionOpen = false;
        public Player HostPlayer { get; set; }
        public List<Player> Players { get; set; }
        public List<Player> PlayersToRemove { get; set; }
        #endregion

        #region CTORS
        public Session(SessionProperties properties)
        {
            Players = new List<Player>(64);
            PlayersToRemove = new List<Player>(64);
            Properties = properties;
        }
        #endregion

        #region Methods
        public void CreateThreads()
        {
            this.RunThread = new Thread(new ThreadStart(Process));
        }
        public void Start()
        {
            this.DoRunThread = true;
            this.RunThread.Start();
            this.SessionOpen = true;
        }
        public void CloseSession()
        {
            SessionOpen = false;
            DoRunThread = false;
            WaitHandler.Dispose();
            for (int i = 0; i < Players.Count; i++)
            {
                Player cPlayer = Players[i];
              
                if (cPlayer != null)
                {
                
                    try
                    {
                        cPlayer.Reader.Close();
                        cPlayer.Writer.Close();
                        cPlayer.OutBuffer.ClearDataAndBuffer();
                        if (cPlayer.Connected)
                            cPlayer.Connection.Close();
                    }
                    catch
                    {
                        Console.WriteLine($"[SESSION] (shutdown) \"{this.Properties.HostName}\" could not close Player \"{cPlayer.Name}\"'s connection.  This is probably OK");
                    }
                }
            }
            Players.Clear();
        }

        public bool AddPlayer(Player target)
        {
            lock (Players)
            {
                if (!DoesPlayerExist(target.PID) && SessionOpen)
                {
                    if (target.PID <= 0)
                        return false;
                    if (target.IsHost)
                        this.HostPlayer = target;
                    Players.Add(target);
                    ProcessOut_SendAll_PlayerJoined(target);
                    //Console.WriteLine($"[SESSION] Session \"{target.Name}\" Added To Session \"{this.HostName}\" (PID: {target.PID})");
                    return true;
                }
                return false;
            }
        }
        public bool DoesPlayerExist(short pid)
        {
            for (int i = 0; i < Players.Count; i++)
                if (Players[i].PID == pid)
                    return true;
            return false;
        }
        public Player GetPlayerByID(short pid)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Player cPlayer = Players[i];
                if (cPlayer.PID == pid)
                    return cPlayer;
            }
            return null;
        }
        #endregion

        #region Session Processing Methods
        private void Process()
        {
            Console.WriteLine($"[SESSION] Session {Properties.HostName} Now Processing");
            while (this.DoRunThread)
            {
                try
                {
                    if (SessionOpen && HostPlayer.Connected)
                    {
                        if (PlayersToRemove.Count > 0)
                        {
                            for (int i = 0; i < PlayersToRemove.Count; i++)
                            {
                                Player targetPlayer = PlayersToRemove[i];
                                if (targetPlayer.IsHost)
                                {
                                    ProcessOut_SendAll_SessionEnd(NetworkSessionEndReason.HostEndedSession);
                                    SessionOpen = false;
                                    return;
                                }
                                else
                                {
                                    ProcessOut_SendAll_PlayerLeave(targetPlayer);
                                    Players.RemoveAll(x => x.PID == targetPlayer.PID);
                                }
                            }
                            PlayersToRemove.Clear();
                        }

                        for (int i = 0; i < Players.Count; i++)
                        {
                            Player cPlayer = Players[i];
                            if (cPlayer == null || cPlayer.IsBeingRemoved)
                                continue;
                            if (cPlayer.Connected)
                            {
                                if (cPlayer.Connection.Available > 0)
                                {
                                    while (cPlayer.Connection.Available > 0)
                                    {
                                        ProcessPlayerData(cPlayer);
                                    }
                                }
                            }
                            else
                            {
                                cPlayer.IsBeingRemoved = true;
                                PlayersToRemove.Add(cPlayer);
                            }
                        }

                        for (int i = 0; i < Players.Count; i++)
                        {
                            Player cPlayer = Players[i];
                            if (cPlayer.OutBuffer.DataCount > 0 && !cPlayer.IsBeingRemoved && cPlayer.Connected)
                                cPlayer.SendData();
                        }

                    }
                    else if (HostPlayer != null || !HostPlayer.Connected)
                    {
                        ProcessOut_SendAll_SessionEnd(NetworkSessionEndReason.HostEndedSession);
                        SessionOpen = false;
                    }
                }
                catch
                {
                    //Console.WriteLine($"[SESSION] Error In Session \"{this.HostName}\" Processing");
                }
                WaitHandler.WaitOne(1);
            }
        }
        #endregion

        #region Processing Data

        #region General Data Processing [IN]
        private void ProcessPlayerData(Player target)
        {
            try
            {
                PacketType type = (PacketType)target.Reader.ReadByte();
                switch (type)
                {
                    case PacketType.TMData:
                        ProcessPlayerData_TMData(target);
                        break;
                    case PacketType.Internal:
                        ProcessPlayerData_Internal(target);
                        break;
                    default:
                        throw new Exception($"[SESSION] Invalid PacketType from Player \"{target.Name}\" In Session \"{this.Properties.HostName}\"");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Player {target.Name ?? "INVALID_PLAYER" } ProcessPlayerData Failure\tRemoving this player.  Error Message:\r\n{ex.Message}");
                target.IsBeingRemoved = true;
            }

        }
        #endregion

        #region TM Processing [IN]
        private void ProcessPlayerData_TMData(Player sourcePlayer)
        {
            short target = sourcePlayer.Reader.ReadInt16();
            int len = sourcePlayer.Reader.ReadInt32();
            byte[] _data = sourcePlayer.Reader.ReadBytes(len);

           // DataPacket tp = new DataPacket(len);
           // tp.SetData(_data);

            Player targetPlayer = this.GetPlayerByID(target);
            if (targetPlayer == null || target == 0)
                ProcessOut_SendAll_TMData(_data, sourcePlayer);
            else
                ProcessOut_Send_TMDataToPlayer(sourcePlayer, targetPlayer, _data);

        }
        #endregion

        #region TM Processing [OUT]
        private void ProcessOut_SendAll_TMData(byte[] _data, Player sourcePlayer)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Player cPlayer = Players[i];
                if (sourcePlayer.PID != cPlayer.PID)
                    ProcessOut_Send_TMDataToPlayer(sourcePlayer, cPlayer, _data);
            }
        }
        private void ProcessOut_Send_TMDataToPlayer(Player sourcePlayer, Player targetPlayer, byte[] _data)
        {
            targetPlayer.OutBuffer.Add((byte)PacketType.TMData);
            targetPlayer.OutBuffer.Add(sourcePlayer == null ? (short)0 : sourcePlayer.PID);
            targetPlayer.OutBuffer.Add(_data.Length);
            targetPlayer.OutBuffer.Add(_data);
            targetPlayer.OutBuffer.Commit();
        }
        #endregion

        #region Internal Processing [IN]
        private void ProcessPlayerData_Internal(Player sourcePlayer)
        {
            short sender = sourcePlayer.Reader.ReadInt16();
            CustomInternalPacket type = (CustomInternalPacket)sourcePlayer.Reader.ReadByte();
            switch (type)
            {
                case CustomInternalPacket.SessionUpdate:
                    ProcessPlayerData_Internal_SessionUpdate(sourcePlayer);
                    break;
                default:
                    throw new Exception("Invalid Internal Packet Type");
            }

        }
        private void ProcessPlayerData_Internal_SessionUpdate(Player sourcePlayer)
        {
            SessionUpdateType type = (SessionUpdateType)sourcePlayer.Reader.ReadByte();
            switch (type)
            {
                case SessionUpdateType.StateUpdate:
                    ProcessPlayerData_Internal_SessionUpdate_StateUpdate(sourcePlayer);
                    break;
                case SessionUpdateType.SessionEnd:
                    ProcessPlayerData_Internal_SessionUpdate_SessionEnded(sourcePlayer);
                    break;
            }

        }
        private void ProcessPlayerData_Internal_SessionUpdate_StateUpdate(Player sourcePlayer)
        {
            if (sourcePlayer.IsHost)
            {
                NetworkSessionState newState = (NetworkSessionState)sourcePlayer.Reader.ReadByte();
                this.Properties.SessionState = newState;
                Console.WriteLine($"[SESSION] Session \"{this.Properties.HostName}\"'s host \"{sourcePlayer.Name}\" Updated Session State To {newState.ToString()}");
                ProcessOut_SendAll_SessionStateUpdate(sourcePlayer, newState);
            }
        }
        private void ProcessPlayerData_Internal_SessionUpdate_SessionEnded(Player sourcePlayer)
        {
            if (sourcePlayer.IsHost)
            {
                ProcessOut_SendAll_SessionEnd(NetworkSessionEndReason.HostEndedSession);
            }
        }
        #endregion

        #region Internal Processing [OUT]
        private void ProcessOut_SendAll_SessionEnd(NetworkSessionEndReason reason)
        {
            for (int i = 0; i < Players.Count; i++)
            {

                Player cPlayer = Players[i];
                if (cPlayer != null && cPlayer.PID != HostPlayer.PID && cPlayer.Connection.IsConnected())
                {
                    cPlayer.Writer.Write((byte)PacketType.Internal);
                    cPlayer.Writer.Write(HostPlayer.PID);

                    cPlayer.Writer.Write((byte)CustomInternalPacket.SessionUpdate);
                    cPlayer.Writer.Write((byte)SessionUpdateType.SessionEnd);
                    cPlayer.Writer.Write((byte)reason);
                    cPlayer.Writer.Flush();
                }
            }
        }
        private void ProcessOut_SendAll_SessionStateUpdate(Player sourcePlayer, NetworkSessionState newState)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Player cPlayer = Players[i];
                if (cPlayer != null && cPlayer.PID != sourcePlayer.PID)
                {
                    cPlayer.OutBuffer.Add((byte)PacketType.Internal);
                    cPlayer.OutBuffer.Add(sourcePlayer.PID);

                    cPlayer.OutBuffer.Add((byte)CustomInternalPacket.SessionUpdate);
                    cPlayer.OutBuffer.Add((byte)SessionUpdateType.StateUpdate);
                    cPlayer.OutBuffer.Add((byte)newState);
                    cPlayer.OutBuffer.Commit();
                }
            }
        }
        private void ProcessOut_SendAll_PlayerLeave(Player targetPlayer)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Player cPlayer = Players[i];
                if (cPlayer.PID != targetPlayer.PID)
                {
                    cPlayer.OutBuffer.Add((byte)PacketType.Internal);
                    cPlayer.OutBuffer.Add(HostPlayer.PID);

                    cPlayer.OutBuffer.Add((byte)CustomInternalPacket.PlayerUpdate);
                    cPlayer.OutBuffer.Add((byte)PlayerUpdateType.Leave);
                    cPlayer.OutBuffer.Add(targetPlayer.PID);
                    cPlayer.OutBuffer.Commit();
                }
            }
        }
        private void ProcessOut_SendAll_PlayerJoined(Player newPlayer)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Player cPlayer = Players[i];
                if (cPlayer.PID != newPlayer.PID)
                {
                    //Header
                    cPlayer.OutBuffer.Add((byte)PacketType.Internal);
                    cPlayer.OutBuffer.Add(newPlayer.PID); //sender

                    //Internal
                    cPlayer.OutBuffer.Add((byte)CustomInternalPacket.PlayerUpdate);
                    cPlayer.OutBuffer.Add((byte)PlayerUpdateType.Join);
                    cPlayer.OutBuffer.Add(newPlayer.PID);
                    cPlayer.OutBuffer.Add(newPlayer.Name);
                    cPlayer.OutBuffer.Commit();
                }
            }
        }
        #endregion

        #endregion

    }
}
