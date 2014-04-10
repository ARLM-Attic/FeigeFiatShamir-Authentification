using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading;
using System.Xml.Serialization;
using System.IO;

namespace Alice
{
    class Program
    {
        /* Network stuff */
        static Socket socketBob, socketTC;
        static Thread thread;
        static XmlSerializer serializer;
        /* constants */
        const int PORT_BOB = 55553;
        const int PORT_TC = 55554;
        /* Feige-Fiat-Shamir stuff */
        static int k, t;
        static BigInteger n;
        static Random random;

        static void Main(string[] args)
        {
            Console.Title = "Alice";

            random = new Random();

            socketBob = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketTC = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socketBob.Bind(new IPEndPoint(IPAddress.Any, PORT_BOB));
            socketTC.Bind(new IPEndPoint(IPAddress.Any, PORT_TC));

            thread = new Thread(ReceiveFromBob);
            thread.IsBackground = true;
            thread.Start();

            Console.ReadLine();
        }

        private static void ReceiveFromBob()
        {
            Console.WriteLine("Waiting for connections...\n");

            while (true)
            {
                EndPoint endpBob = new IPEndPoint(IPAddress.Any, 0);

                /* check if valid Data object */
                serializer = new XmlSerializer(typeof(Data));
                byte[] buffer = new byte[2048];
                socketBob.ReceiveFrom(buffer, ref endpBob);
                MemoryStream stream = new MemoryStream(buffer);
                Data request = (Data)serializer.Deserialize(stream);                
                stream.Close();

                IPEndPoint ipendp = (IPEndPoint)endpBob;
                Console.WriteLine(ipendp.Address + ": Requested authentification");

                /* send request for w's to the trust center */
                serializer = new XmlSerializer(typeof(Data));
                stream = new MemoryStream();
                serializer.Serialize(stream, request);
                socketTC.SendTo(stream.ToArray(), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55557));
                stream.Close();

                EndPoint endpTC = new IPEndPoint(IPAddress.Any, 0);

                /* get the w's of Bob from TC */
                serializer = new XmlSerializer(typeof(Data));
                buffer = new byte[2048];
                socketTC.ReceiveFrom(buffer, ref endpTC);
                stream = new MemoryStream(buffer);
                Data bob = (Data)serializer.Deserialize(stream);
                k = bob.k;
                t = bob.t;
                n = BigInteger.Parse(bob.n);
                stream.Close();

                IPEndPoint ipendp2 = (IPEndPoint)endpTC;
                Console.WriteLine(ipendp2.Address + ": Got the w's from user with ID " + bob.id);

                /* Alice tells Bob that she has his w's and that he can now send his v's */
                serializer = new XmlSerializer(typeof(Data));
                stream = new MemoryStream();
                serializer.Serialize(stream, bob);
                socketBob.SendTo(stream.ToArray(), endpBob);
                stream.Close();

                for (int i = 0; i < t; i++)
                {
                    /* wait for the v */
                    serializer = new XmlSerializer(typeof(Data));
                    buffer = new byte[2048];
                    socketBob.ReceiveFrom(buffer, ref endpBob);
                    stream = new MemoryStream(buffer);
                    Data v = (Data)serializer.Deserialize(stream);
                    stream.Close();

                    /* get the binary vector and send it to Bob */
                    byte b = (byte)random.Next(-128, 127);
                    serializer = new XmlSerializer(typeof(Data));
                    stream = new MemoryStream();
                    serializer.Serialize(stream, new Data { b = b });
                    socketBob.SendTo(stream.ToArray(), endpBob);
                    stream.Close();

                    /* receive u */
                    serializer = new XmlSerializer(typeof(Data));
                    buffer = new byte[2048];
                    socketBob.ReceiveFrom(buffer, ref endpBob);
                    stream = new MemoryStream(buffer);
                    Data u = (Data)serializer.Deserialize(stream);
                    stream.Close();

                    /* calculating v' */
                    BigInteger v2 = BigInteger.Pow(BigInteger.Parse(u.u), 2);
                    for (int j = 0; j < 8; j++)
                    {
                        if ((b & (1 << j)) != 0)
                            v2 = v2 * BigInteger.Parse(bob.w[j]) % n;
                    }
                    BigInteger vNeu = BigInteger.Parse(v.v);

                    /* looks good for now */
                    if (v2 == vNeu || BigInteger.Abs(v2) == vNeu)
                    {                     
                        serializer = new XmlSerializer(typeof(Data));
                        stream = new MemoryStream();
                        serializer.Serialize(stream, new Data { LooksGoodForNow = true });
                        socketBob.SendTo(stream.ToArray(), endpBob);
                        stream.Close();
                    }

                    else
                    {
                        /* NO AUTHENTIFICATION */
                        serializer = new XmlSerializer(typeof(Data));
                        stream = new MemoryStream();
                        serializer.Serialize(stream, new Data { LooksGoodForNow = false });
                        socketBob.SendTo(stream.ToArray(), endpBob);
                        stream.Close();

                        break;
                    }
                }
                /* if the loop passed without a single "no authentification", then the user is authenticated */
            }
        }
    }

    [Serializable]
    public class Data
    {
        public int id { get; set; }
        /* BigInteger is not serializable --> Parse to String */
        public string n { get; set; }
        public string u { get; set; }
        public string v { get; set; }
        public string[] w { get; set; }
        public int k { get; set; }
        public int t { get; set; }
        /* binary vector */
        public byte b { get; set; }
        public bool LooksGoodForNow { get; set; }

        public bool Compare(Data obj)
        {
            if (id == obj.id && n == obj.n && w.SequenceEqual(obj.w) && k == obj.k && t == obj.t)
                return true;
            return false;
        }
    }
}
