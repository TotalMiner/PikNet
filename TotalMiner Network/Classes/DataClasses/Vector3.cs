using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TotalMiner_Network.Classes
{
    public struct Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public static Vector3 Zero = new Vector3(0, 0, 0);

        public Vector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
