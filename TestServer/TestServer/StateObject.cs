using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    class StateObject
    {
        public static int MaxDataSize = 260;
        public const int BufferSize = 1024;
        public byte[] Buffer = new byte[BufferSize];
        public List<byte> ResultantBuffer = new List<byte>();
    }
}
