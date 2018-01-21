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

            #region Async
            AsyncCallback AcceptClients = new AsyncCallback((IAsyncResult res) => 
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

            });
            #endregion

            while (ServerRunning)
            {
                ServerListener.BeginAcceptTcpClient(AcceptClients, ServerListener);
                if (AllSessions.Count > 0)
                {
                    for (int i = 0; i < AllSessions.Count; i++)
                    {
                    
                        Session curSes = AllSessions[i];
                        if (!curSes.SessionOpen)
                        {
                            curSes.CloseSession();

                            AllSessions.Remove(curSes);
                            Console.WriteLine($"[MASTER] Closed and Removed Session \"{curSes.HostName}\"");
                        }
                    }
                }
                WaitHandler.WaitOne(1);
            }
        }
        #endregion

        #region Processing
        private void Master_ProcessNewClient(TcpClient targetClient)
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
                int exeVersion = reader.ReadInt32();
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

                    YesNo valid = targetSession.AddPlayer(newPlayer) && exeVersion == targetSession.EXEVersion 
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

            int exeVersion = reader.ReadInt32();
            short hostGID = reader.ReadInt16();
            string hostName = reader.ReadString();
            NetworkSessionType type = (NetworkSessionType)reader.ReadByte();
            NetworkSessionState state = (NetworkSessionState)reader.ReadByte();

            Session newSession = new Session(hostName, hostGID, exeVersion, type, state);
            writer.Write((byte)Master_Server_Op_Out.Connect);
            writer.Write((byte)Master_server_ConnectionType.CreateSession);
            YesNo good = AddSession(newSession) ? YesNo.Yes : YesNo.No;

            writer.Write((byte)good);
            if (good == YesNo.Yes)
            {
                writer.Write(newSession.SessionID);
                Player newPlayer = new Player(hostName)
                {
                    IsHost = true,
                    PID = hostGID,
                    Connection = target
                };
                newSession.AddPlayer(newPlayer);
            }
            else
                writer.Close();
        }
        private void Master_Process_Connect_GetSessions(TcpClient target)
        {
            //Console.WriteLine("[MASTER] Sending Sessions To Target");
            BinaryReader reader = new BinaryReader(target.GetStream());
            BinaryWriter writer = new BinaryWriter(target.GetStream());
            int exeVersion = reader.ReadInt32();

            List<Session> sessionsWithVer = GetSessionsWithEXEVersion(exeVersion);

            writer.Write((byte)Master_Server_Op_Out.Connect);
            writer.Write((byte)Master_server_ConnectionType.GetSessions);
            writer.Write(sessionsWithVer.Count);
            for (int i = 0; i < sessionsWithVer.Count; i++)
            {
                Session curSes = sessionsWithVer[i];

                writer.Write(curSes.HostName);
                writer.Write(curSes.HostGID);
                writer.Write(curSes.EXEVersion);
                writer.Write(curSes.SessionID);
                writer.Write(curSes.Players.Count);
                writer.Write((byte)curSes.SessionState);
                writer.Write((byte)curSes.Sessiontype);
            }
        }
        #endregion

        #region Master Server Methods
        private bool AddSession(Session target)
        {
            lock (AllSessions)
            {
                if (target.Sessiontype == NetworkSessionType.PlayerMatch && target.HostName.Length <= 15)
                {
                    target.SessionID = SessionIDCounter++;
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
                if (AllSessions[i].SessionID == id)
                    return AllSessions[i];
            return null;
        }
        private List<Session> GetSessionsWithEXEVersion(int ver)
        {
            List<Session> toRet = new List<Session>();
            for (int i = 0; i < AllSessions.Count; i++)
            {
                Session session = AllSessions[i];
                if (session.EXEVersion == ver && session.SessionOpen)
                    toRet.Add(session);
            }
            return toRet;
        }
        #endregion
    }
}
