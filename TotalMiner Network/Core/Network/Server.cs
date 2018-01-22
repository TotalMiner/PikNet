using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using TotalMiner_Network.Extensions;
using TotalMiner_Network.Core.Data;
using TotalMiner_Network.Core.Classes;
namespace TotalMiner_Network.Core.Network
{
    public class Server
    {
        #region Event
        private EventWaitHandle WaitHandler = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
        #endregion

        #region Vars
        public List<Session> AllSessions;

        private TcpListener ServerListener;

        private int SessionIDCounter = 0;

        private Thread RunThread;

        private bool ServerRunning = true;
        #endregion

        #region CTORS
        public Server(int sessionsCapacity)
        {
            AllSessions = new List<Session>(sessionsCapacity);
        }
        #endregion

        #region Methods
        public void CreateThreads(bool start = false)
        {
            RunThread = new Thread(new ThreadStart(this.RunMasterServer));
            if (start)
                Start();
        }
        public void Start()
        {
            ServerListener = new TcpListener(IPAddress.Any, 5786);
            ServerListener.Start();
            RunThread.Start();
        }
        #endregion

        #region Threadded Functions
       
        private void RunMasterServer()
        {
            Console.WriteLine("[MASTER] Server Running");


            while (ServerRunning)
            {
                ServerListener.BeginAcceptTcpClient(AcceptClientCallBack, ServerListener);
              
                if (AllSessions.Count > 0)
                {
                    for (int i = 0; i < AllSessions.Count; i++)
                    {
                    
                        Session curSes = AllSessions[i];
                        if (!curSes.SessionOpen)
                        {
                            
                            curSes.CloseSession();
                          
                            AllSessions.Remove(curSes);
                            GC.Collect();
                            Console.WriteLine($"[MASTER] Closed and Removed Session \"{curSes.Properties.HostName}\"");
                        }
                    }
                }
                WaitHandler.WaitOne(1);
            }
            WaitHandler.Dispose();
        }
        private void AcceptClientCallBack(IAsyncResult res)
        {
            try
            {
                TcpClient clientSocket = ServerListener.EndAcceptTcpClient(res);
                Master_ProcessNewClient(clientSocket);
            }
            catch
            {
                Console.WriteLine("BeginAcceptTcpClient Error");
            }
        }
        #endregion

        #region Processing
        private void Master_ProcessNewClient(TcpClient targetClient)
        {
            try
            {
                BinaryReader reader = new BinaryReader(targetClient.GetStream());
                Master_Server_Op_In op = (Master_Server_Op_In)reader.ReadByte();
                switch (op)
                {
                    case Master_Server_Op_In.Connect:
                        Master_Process_Connect(targetClient);
                        break;
                }
            }
            catch
            {
                try
                {
                    targetClient.Close();
                }
                catch { }
                Console.WriteLine("[MASTER] Error Processing New Client.  Shutting Down Connection");
            }
        }
        private void Master_Process_Connect(TcpClient target)
        {
            BinaryReader reader = new BinaryReader(target.GetStream());
            Master_server_ConnectionType type = (Master_server_ConnectionType)reader.ReadByte();
            switch (type)
            {
                case Master_server_ConnectionType.CreateSession:
                    Master_Process_Connect_CreateSession(target);
                    break;
                case Master_server_ConnectionType.GetSessions:
                    Master_Process_Connect_GetSessions(target);
                    break;
                case Master_server_ConnectionType.JoinSession:
                    Master_Process_Connect_JoinSession(target);
                    break;
            }

        }
        private void Master_Process_Connect_JoinSession(TcpClient target)
        {
            lock (target)
            {
                BinaryReader reader = new BinaryReader(target.GetStream());
                BinaryWriter writer = new BinaryWriter(target.GetStream());

                int sessID = reader.ReadInt32();
                short gid = reader.ReadInt16();
                string pName = reader.ReadString();

                Session targetSession = GetSession(sessID);

                writer.Write((byte)Master_Server_Op_Out.Connect);
                writer.Write((byte)Master_server_ConnectionType.JoinSession);
                if (targetSession != null)
                {
                    Player newPlayer = new Player(pName)
                    {
                        Connection = target,
                        IsHost = false,
                        PID = gid,
                    };

                    YesNo valid = targetSession.AddPlayer(newPlayer)
                        ? YesNo.Yes 
                        : YesNo.No;
                    writer.Write((byte)valid);


                    if (valid == YesNo.No)
                        target.Close();
                    else
                    {
                        writer.Write(targetSession.Players.Count(x => x.PID != newPlayer.PID));
                        for (int i = 0; i < targetSession.Players.Count; i++)
                        {
                            Player cPlayer = targetSession.Players[i];
                            if (cPlayer.PID != newPlayer.PID)
                            {
                                writer.Write(cPlayer.PID);
                                writer.Write(cPlayer.Name);
                                writer.Write(cPlayer.IsHost);
                            }
                        }
                        Console.WriteLine($"[SESSION] Player {newPlayer.Name} Has Joined Session {targetSession.Properties.HostName}");
                        writer.Flush();
                    }
                }
                else
                {
                    writer.Write((byte)YesNo.No);
                    writer.Flush();
                    target.Close();
                }
            }
        }
        private void Master_Process_Connect_CreateSession(TcpClient target)
        {
            BinaryReader reader = new BinaryReader(target.GetStream());
            BinaryWriter writer = new BinaryWriter(target.GetStream());

            SessionProperties newProperties = SessionProperties.Read(reader);

            Session newSession = new Session(newProperties);
            writer.Write((byte)Master_Server_Op_Out.Connect);
            writer.Write((byte)Master_server_ConnectionType.CreateSession);
            YesNo good = AddSession(newSession) ? YesNo.Yes : YesNo.No;

            writer.Write((byte)good);
            if (good == YesNo.Yes)
            {
                writer.Write(newSession.Properties.HostName);
                Player newPlayer = new Player(newSession.Properties.HostName)
                {
                    IsHost = true,
                    PID = newSession.Properties.HostID,
                    Connection = target
                };
                newSession.AddPlayer(newPlayer);
            }
            else
                writer.Close();
        }
        private void Master_Process_Connect_GetSessions(TcpClient target)
        {
            BinaryReader reader = new BinaryReader(target.GetStream());
            BinaryWriter writer = new BinaryWriter(target.GetStream());
            //int exeVersion = reader.ReadInt32();

            List<Session> sessions = AllSessions;

            writer.Write((byte)Master_Server_Op_Out.Connect);
            writer.Write((byte)Master_server_ConnectionType.GetSessions);
            writer.Write(sessions.Count);
            for (int i = 0; i < sessions.Count; i++)
            {
                Session curSes = sessions[i];
                curSes.Properties.Write(writer);
            }
        }
        #endregion

        #region Master Server Methods
        private bool AddSession(Session target)
        {
            lock (AllSessions)
            {
                if (target.Properties.NetType == NetworkSessionType.PlayerMatch && target.Properties.HostName.Length <= 15)
                {
                    target.Properties.SessionID = SessionIDCounter++;
                    target.CreateThreads();
                    target.Start();

                    AllSessions.Add(target);
                    return true;
                }
                return false;
            }
        }
        private Session GetSession(int id)
        {
            for (int i = 0; i < AllSessions.Count; i++)
                if (AllSessions[i].Properties.SessionID == id)
                    return AllSessions[i];
            return null;
        }
        private List<Session> GetSessionsWithEXEVersion(int ver)
        {
            List<Session> toRet = new List<Session>();
            for (int i = 0; i < AllSessions.Count; i++)
            {
                Session session = AllSessions[i];
                if (session.Properties.ExeVersion == ver && session.SessionOpen)
                    toRet.Add(session);
            }
            return toRet;
        }
        #endregion
    }
}
