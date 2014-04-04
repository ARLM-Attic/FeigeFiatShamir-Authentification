using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using System.Numerics;
using System.Xml.Serialization;
using System.IO;

namespace Alice
{
    class Program
    {
        /* Network stuff */
        static Socket socket;
        static EndPoint endp;
        static Thread thread;
        static MemoryStream stream;
        static XmlSerializer serializer;
        const int PORT = 5555;
     
        /* Feige-Fiat-Shamir stuff */

        static void Main(string[] args)
        {
            Console.Title = "Alice";
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            /* listen on port 5555 */
            socket.Bind(new IPEndPoint(IPAddress.Any, PORT));                   

            thread = new Thread(ReceiveRequests);
            thread.IsBackground = true;
            thread.Start();

            Console.ReadLine();
        }

        private static void ReceiveRequests()
        {
            Console.WriteLine("Waiting for connections...");

            while (true)
            {
                /* check if valid Data object */
                serializer = new XmlSerializer(typeof(Data));
                byte[] buffer = new byte[1024];
                socket.ReceiveFrom(buffer, ref endp);
                stream = new MemoryStream(buffer);
                Data request = (Data)serializer.Deserialize(stream);
                stream.Close();

                IPEndPoint ipendp = (IPEndPoint)endp;
                /* request for authentification from Bob (w == null) */
                if (request.w == null)
                {
                    Console.WriteLine(ipendp.Address + ": Requested authentification");

                    /* send request for w's to the authentification center */
                    serializer = new XmlSerializer(typeof(Data));
                    stream = new MemoryStream();
                    serializer.Serialize(stream, request);
                    socket.SendTo(stream.ToArray(), endp);
                }

                /* response from verifcation center with w's of Bob */ 
                else
                {
                    
                }  
            }
        }
    }

    [Serializable]
    public class Data
    {
        public int id { get; set; }
        /* BigInteger is not serializable --> Parse to String */
        public string n { get; set; }
        public string[] w { get; set; }
        public int k { get; set; }
        public int t { get; set; }

        public bool Compare(Data obj)
        {
            if (id == obj.id && n == obj.n && w.SequenceEqual(obj.w) && k == obj.k && t == obj.t)
                return true;
            return false;
        }
    }
}
