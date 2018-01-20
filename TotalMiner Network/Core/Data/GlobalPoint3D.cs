using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalMiner_Network.Core.Data
{
    public struct GlobalPoint3D
    {
        public int X;
        public int Y;
        public int Z;
        public GlobalPoint3D(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }
}
