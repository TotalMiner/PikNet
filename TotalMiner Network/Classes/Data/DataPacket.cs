using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalMiner_Network.Classes.Data
{
    public unsafe struct DataPacket
    {
        public DataPacket(int size)
        {
            Data = new byte[size];
            _Position = (int*)0;
            OptionalTarget = 0;
            OptionalSender = 0;
        }

        private byte[] Data;
        private int* _Position;
        public int Position
        {
            get
            {
                return (int)_Position;
            }
            set
            {
                _Position = (int*)value;
            }
        }

        public short OptionalTarget;
        public short OptionalSender;

        public byte ReadByte()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 1);
            return Data[(int)dp++];
        }
        public sbyte ReadSByte()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)dp + 1;
            return (sbyte)Data[(int)dp++];
        }
        public byte[] ReadBytes(int len)
        {
            byte[] _data = new byte[len];
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + len);
            Buffer.BlockCopy(Data, (int)dp, _data, 0, len);
            return _data;
        }
        public short ReadShort()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 2);
            return (short)(Data[(int)dp++] | (Data[(int)dp++] << 8));
        }
        public ushort ReadUShort()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 2);
            return (ushort)(Data[(int)dp++] | (Data[(int)dp++] << 8));
        }
        public int ReadInt32()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 4);
            return (int)(Data[(int)dp++] | (Data[(int)dp++] << 8) | (Data[(int)dp++] << 16) | (Data[(int)dp++] << 24));
        }
        public uint ReadUInt32()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 4);
            return (uint)(Data[(int)dp++] | (Data[(int)dp++] << 8) | (Data[(int)dp++] << 16) | (Data[(int)dp++] << 24));
        }
        public long ReadInt64()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 8);
            return (long)(Data[(int)dp++] | ((long)Data[(int)dp++] << 8) | ((long)Data[(int)dp++] << 16) | ((long)Data[(int)dp++] << 24) | ((long)Data[(int)dp++] << 32) | ((long)Data[(int)dp++] << 40) | ((long)Data[(int)dp++] << 48) | ((long)Data[(int)dp++] << 56));
        }
        public ulong ReadUInt64()
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 8);
            return (ulong)(Data[(int)dp++] | ((ulong)Data[(int)dp++] << 8) | ((ulong)Data[(int)dp++] << 16) | ((ulong)Data[(int)dp++] << 24) | ((ulong)Data[(int)dp++] << 32) | ((ulong)Data[(int)dp++] << 40) | ((ulong)Data[(int)dp++] << 48) | ((ulong)Data[(int)dp++] << 56));
        }
        public int Read7BitEncodedInt()
        {
            int num = 0;
            int num2 = 0;
            while (num2 != 35)
            {
                byte b = ReadByte();
                num |= (int)(b & 127) << num2;
                num2 += 7;
                if ((b & 128) == 0)
                {
                    return num;
                }
            }
            throw new FormatException("Invalid 7BitEncodedInt Format");
        }
        public string ReadString()
        {
            return Encoding.ASCII.GetString(ReadBytes(Read7BitEncodedInt()));
        }

        public void SetData(byte[] _data)
        {
            if (_data.Length != Data.Length) throw new ArgumentException("Invalid Data Size");
            Buffer.BlockCopy(_data, 0, Data, 0, Data.Length);
            _Position = (int*)0;
        }
        public byte[] GetData()
        {
            return Data;
        }

    }
}
