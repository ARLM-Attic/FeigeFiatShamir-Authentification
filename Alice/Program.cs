using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Alice
{
    class Program
    {
        static Socket s;
        static IPEndPoint endp, ipendp;

        static void Main(string[] args)
        {
            Console.Title = "FeigeFiatShamir-Authentification";
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            /* listen on port 5555 */
            endp = new IPEndPoint(IPAddress.Any, 5555);

            while (true)
            {
                Console.WriteLine("Waiting for connections...");
                /* Client authentifiziert sich */
                Console.ReadLine();
            }
        }
    }
}
