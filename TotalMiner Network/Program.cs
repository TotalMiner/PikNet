using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TotalMiner_Network.Classes;
using System.IO;
using TotalMiner_Network.Extensions;
//
namespace TotalMiner_Network
{
    class Program
    {
        private static EventWaitHandle WaitHandler = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());

        static Thread RunThread;
        static Thread ListenThread;

        static bool ServerRunning = true;
        static bool ServerAccepting = false;

        static TcpListener Server;

        static int GlobalSessionCounter = 0;

        static List<Session> Sessions;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((object sE, UnhandledExceptionEventArgs sA) =>
            {
                //Console.WriteLine(((Exception)sA.ExceptionObject).Message);
            });
            
            Sessions = new List<Session>(32);

            RunThread = new Thread(new ThreadStart(RunMasterServer));
            ListenThread = new Thread(new ThreadStart(AcceptMasterServer));
            RunThread.Start();
            ListenThread.Start();

            //Console.WriteLine("Not Listening For Commands");
            while (true)
            {
                string cmd = Console.ReadLine();
                System.Threading.Thread.Sleep(1000);
            }
        }

        static void RunMasterServer()
        {
            Server = new TcpListener(IPAddress.Any, 5786);
            Server.Start();
            ServerAccepting = true;
            Console.WriteLine("[MASTER] Server Running");
            while (ServerRunning)
            {
                if (Sessions.Count > 0)
                {
                    for (int i = 0; i < Sessions.Count; i++)
                    {
                        Session curSes = Sessions[i];
                        if (!curSes.SessionOpen)
                        {
                            curSes.CloseSession();
                            
                            Sessions.Remove(curSes);
                            GC.Collect();
                            Console.WriteLine($"[MASTER] Closed and Removed Session \"{curSes.HostName}\"");
                        }
                        else
                        {

                        }
                    }
                }
                WaitHandler.WaitOne(1);
            }
        }
        static void AcceptMasterServer()
        {
            while (true)
            {
                if (ServerAccepting)
                {
                    try
                    {
                        TcpClient client = Server.AcceptTcpClient();
                        Master_ProcessNewClient(client);
                        //Console.WriteLine("[MASTER] Accepted new TCPClient");
                    } catch (InvalidOperationException ex)
                    {
                        //Console.WriteLine("[MASTER] ServerAcceppt Error");
                        //Console.WriteLine(ex.Message);
                    }
                
                   
                }
                System.Threading.Thread.Sleep(0);
            }
        }

        static void Master_ProcessNewClient(TcpClient targetClient)
        {
            //Console.WriteLine("[MASTER] Process New Client");
            BinaryReader reader = new BinaryReader(targetClient.GetStream());
            Master_Server_Op_In op = (Master_Server_Op_In)reader.ReadByte();
            switch (op)
            {
                case Master_Server_Op_In.Connect:
                    Master_Process_Connect(targetClient);
                    break;
            }
        }
        static void Master_Process_Connect(TcpClient target)
        {
            //Console.WriteLine("[MASTER] Processing New Client Connect");
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
        static void Master_Process_Connect_JoinSession(TcpClient target)
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

                    YesNo valid = targetSession.AddPlayer(newPlayer) && exeVersion == targetSession.EXEVersion ? YesNo.Yes : YesNo.No;
                    writer.Write((byte)valid);


                    if (valid == YesNo.No)
                        target.Close();
                    else
                    {
                        writer.Write(targetSession.Players.Count(x => x.PID != newPlayer.PID));
                        for (int i = 0; i < targetSession.Players.Count;i++)
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
        static void Master_Process_Connect_CreateSession(TcpClient target)
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
            YesNo good = AddSession(newSession)  ? YesNo.Yes : YesNo.No;

            writer.Write((byte)good);
            if (good == YesNo.Yes)
            {
                writer.Write(newSession.SessionID);
                Player newPlayer = new Player(hostName);
                newPlayer.IsHost = true;
                newPlayer.PID = hostGID;
                newPlayer.Connection = target;
                newSession.AddPlayer(newPlayer);
            }
            else
                writer.Close();
        }
        static void Master_Process_Connect_GetSessions(TcpClient target)
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

        #region Master Server Methods
        static bool AddSession(Session target)
        {
            lock (Sessions)
            {
                if (target.Sessiontype == NetworkSessionType.PlayerMatch && target.HostName.Length <= 15)
                {
                    target.SessionID = GlobalSessionCounter++;
                    target.CreateThreads();
                    target.Start();
                  
                    Sessions.Add(target);
                    //Console.WriteLine($"Added New Session: {target.HostName}");
                    return true;
                }
                //Console.WriteLine($"Could not add new session: {target.HostName}");
                return false;
            }
        }
        static Session GetSession(int id)
        {
            for (int i = 0; i < Sessions.Count; i++)
                if (Sessions[i].SessionID == id)
                    return Sessions[i];
            return null;
        }
        static List<Session> GetSessionsWithEXEVersion(int ver)
        {
            List<Session> toRet = new List<Session>();
            for (int i = 0; i < Sessions.Count; i++)
            {
                Session session = Sessions[i];
                if (session.EXEVersion == ver && session.SessionOpen)
                    toRet.Add(session);
            }
            return toRet;
        }
        #endregion
    }
}