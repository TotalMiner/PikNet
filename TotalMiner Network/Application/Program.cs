using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using TotalMiner_Network.Extensions;
using TotalMiner_Network.Core.Network;
namespace TotalMiner_Network
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((object sE, UnhandledExceptionEventArgs sA) =>
            {
                //Console.WriteLine(((Exception)sA.ExceptionObject).Message);
            });

            Server masterServer = new Server(24);
            masterServer.CreateThreads();
            masterServer.Start();

            Console.WriteLine("Now Listening For Commands");
            while (true)
            {
                string cmd = Console.ReadLine();
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}