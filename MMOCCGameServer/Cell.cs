using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MMOCCGameServer
{
    public class Cell
    {
        public int X;
        public int Y;
        public int Number;

        public Vector3 ConvertToVector3()
        {
            return new Vector3(X, Y, 0);
        }
    }


}
