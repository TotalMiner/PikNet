using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalMiner_Network.Core.Data
{
    public unsafe struct DataPacketWriter
    {
        public DataPacketWriter(int size)
        {
            Data = new byte[size];
            _Position = (int*)0;
            OptionalTarget = 0;
            OptionalSender = 0;
            _Length = 0;
        }

        private byte[] Data;
        private int* _Position;

        public int FullLength
        {
            get
            {
                return Data.Length;
            }
        }
        private int _Length;
        public int Length
        {
            get
            {
                return _Length;
            }
        }
        public int Position
        {
            get
            {
                return (int)_Position;
            }
        }

        public short OptionalTarget;
        public short OptionalSender;

        public void Write(byte data)
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + 1);
            _Length += 1;
            Data[(int)dp] = data;
        }
        public void Write(byte[] data)
        {
            byte* dp = (byte*)_Position;
            _Position = (int*)(dp + data.Length);
            _Length += data.Length;
            Buffer.BlockCopy(data, 0, Data, (int)dp, data.Length);
        }
        public void Write(short val)
        {
            Write(BitConverter.GetBytes(val));
        }
        public void Write(ushort val)
        {
            Write(BitConverter.GetBytes(val));
        }
        public void Write(int val)
        {
            Write(BitConverter.GetBytes(val));
        }
        public void Write(uint val)
        {
            Write(BitConverter.GetBytes(val));
        }
        public void Write(long val)
        {
            Write(BitConverter.GetBytes(val));
        }
        public void Write(ulong val)
        {
            Write(BitConverter.GetBytes(val));
        }
        public void Write7BitEncodedInt(int val)
        {
            uint num;
            for (num = (uint)val; num >= 128u; num >>= 7)
                Write((byte)(num | 128u));
            Write((byte)num);
        }
        public void Write(string val)
        {
            Write7BitEncodedInt(Encoding.ASCII.GetByteCount(val));
            Write(Encoding.ASCII.GetBytes(val));
        }
        public byte[] GetAllData()
        {
            return Data;
        }
        public void GetAllData(byte[] target)
        {
            Buffer.BlockCopy(Data, 0, target, 0, FullLength);
        }

        public byte[] GetData()
        {
            byte[] _data = new byte[_Length];
            Buffer.BlockCopy(Data, 0, _data, 0, _Length);
            return _data;
        }
        public void GetData(byte[] target)
        {
            Buffer.BlockCopy(Data, 0, target, 0, _Length);
        }
    }
}
