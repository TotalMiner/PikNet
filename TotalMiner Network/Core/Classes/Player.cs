using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using TotalMiner_Network.Extensions;
using TotalMiner_Network.Core.Data;

namespace TotalMiner_Network.Core.Classes
{
    public class Player
    {
        private TcpClient _Connection;
        public TcpClient Connection
        {
            get
            {
                return _Connection;
            }
            set
            {
                Reader = new BinaryReader(value.GetStream());
                Writer = new BinaryWriter(value.GetStream());
                _Connection = value;
            }
        }
        public bool Connected
        {
            get
            {
                return Connection.Connected && Connection.IsConnected();
            }
        }

        public BinaryWriter Writer;
        public BinaryReader Reader;
        public bool IsBeingRemoved;
        public MemBuffer OutBuffer;

        public short PID;
        public string Name;
        public bool IsHost;

        public Player(string name)
        {
            this.Name = name;
            OutBuffer = new MemBuffer(262144);
        }

        public void SendData()
        {
            if (OutBuffer.DataCount > 0)
            {
                OutBuffer.WriteDataToStream(this.Connection.GetStream(), true);
            }
        }
    }
}
