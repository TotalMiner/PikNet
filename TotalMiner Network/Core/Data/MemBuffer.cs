using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace TotalMiner_Network.Core.Data
{
    public class MemBuffer
    {
        public MemBuffer()
        {
            this.StreamBuffer = new MemoryStream(262144);
            this._Buffer = new List<byte>(262144);
            this.Data = new Queue<byte[]>(128);
        }
        public MemBuffer(int cap)
        {
            this.StreamBuffer = new MemoryStream(cap);
            this._Buffer = new List<byte>(cap);
            this.Data = new Queue<byte[]>(128);
        }

        private MemoryStream StreamBuffer;
        private List<byte> _Buffer;
        public Queue<byte[]> Data { get; set; }

        public int BufferLength
        {
            get
            {
                return _Buffer.Count;
            }
        }
        public int DataCount
        {
            get
            {
                return Data.Count;
            }
        }
    
        public void Add(byte data)
        {
            _Buffer.Add(data);
        }
        public void Add(sbyte data)
        {
            _Buffer.Add((byte)data);
        }
        public void Add(byte[] data)
        {
             _Buffer.AddRange(data);
        }
        public void Add(short data)
        {
            _Buffer.AddRange(BitConverter.GetBytes(data));
        }
        public void Add(ushort data)
        {
            _Buffer.AddRange(BitConverter.GetBytes(data));
        }
        public void Add(int data)
        {
            _Buffer.AddRange(BitConverter.GetBytes(data));
        }
        public void Add(uint data)
        {
            _Buffer.AddRange(BitConverter.GetBytes(data));
        }
        public void Add(float data)
        {
            _Buffer.AddRange(BitConverter.GetBytes(data));
        }
        public void Add(string data)
        {
            ClearStreamBuffer();
            BinaryWriter writer = new BinaryWriter(this.StreamBuffer);
            writer.Write(data);
            this.Add(this.StreamBuffer.ToArray());
            ClearStreamBuffer();
        }
        public void Add(Vector3 vec)
        {
            _Buffer.AddRange(BitConverter.GetBytes(vec.X));
            _Buffer.AddRange(BitConverter.GetBytes(vec.Y));
            _Buffer.AddRange(BitConverter.GetBytes(vec.Z));
        }
        public void Add(GlobalPoint3D point)
        {
            _Buffer.AddRange(BitConverter.GetBytes(point.X));
            _Buffer.AddRange(BitConverter.GetBytes(point.Y));
            _Buffer.AddRange(BitConverter.GetBytes(point.Z));
        }


        public void ClearBuffer()
        {
            _Buffer.Clear();
        }
        public void ClearData()
        {
            Data.Clear();
        }
        public void CleatDataAndBuffer()
        {
            Data.Clear();
            _Buffer.Clear();
        }
        public void Write(Stream xOut, bool allInOneChunk = true)
        {
            if (Data.Count > 0)
            {
                byte[] _dataToSend = null;
                if (allInOneChunk)
                {
                    _dataToSend = new byte[Data.Sum(x => x.Length)];
                    int curSize = 0;
                    while (Data.Count != 0)
                    {
                        byte[] _thisData = Data.Dequeue();
                        Array.Copy(_thisData, 0, _dataToSend, curSize, _thisData.Length);
                        curSize += _thisData.Length;
                    }
                    xOut.Write(_dataToSend, 0, _dataToSend.Length);
                }
                else
                {
                    while (Data.Count != 0)
                    {
                        _dataToSend = Data.Dequeue();
                        xOut.Write(_dataToSend, 0, _dataToSend.Length);
                    }
                }
                xOut.Flush();
            }
        }
        public void ClearStreamBuffer()
        {
            this.StreamBuffer.SetLength(0);
        }
        public void Commit()
        {
            Data.Enqueue(_Buffer.ToArray());
            ClearBuffer();
        }



    }
}
