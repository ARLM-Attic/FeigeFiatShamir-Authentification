using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace VerificationCenter
{
    class Program
    {
        static Socket socket;
        static Thread t;

        static void Main(string[] args)
        {
            Console.Title = "Verification Center";

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            t = new Thread(Receive);
            t.Start();

            Console.ReadLine();
        }

        static void Receive()
        {
            Console.WriteLine("Waiting for requests...\n");

            while (true)
            {
                EndPoint endp = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = new byte[1024];
                int anz = socket.ReceiveFrom(buffer, 1024, SocketFlags.None, ref endp);
                IPEndPoint ep = (IPEndPoint) endp;
                Console.WriteLine(ep.Address + ": Requested verification");

                /* check if valid struct (yet to be written) */
            }
        }
    }
}
