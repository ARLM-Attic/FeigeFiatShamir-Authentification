using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO;

namespace VerificationCenter
{
    class Program
    {
        /* network stuff */
        static Socket socketAlice, socketBob;
        static Thread thread;
        static Data data;
        static List<Data> users;
        static XmlSerializer serializer;
        static MemoryStream stream;
        static Random random;
        /* constants */
        const int PORT_ALICE = 5557;
        const int PORT_BOB = 5556;
        const int k = 8;
        const int t = 3;

        static void Main(string[] args)
        {
            Console.Title = "Verification Center";

            random = new Random();
            data = InitValues();

            socketAlice = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socketBob = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socketAlice.Bind(new IPEndPoint(IPAddress.Any, PORT_ALICE));
            socketBob.Bind(new IPEndPoint(IPAddress.Any, PORT_BOB));

            thread = new Thread(ReceiveFromBob);
            thread.IsBackground = true;
            thread.Start();

            thread = new Thread(ReceiveFromAlice);
            thread.IsBackground = true;
            thread.Start();

            Console.ReadLine();
        }

        static Data InitValues()
        {
            BigInteger p, q, n;
            byte[] buffer = new byte[sizeof(UInt64)];

            /* once for p */
            do
            {
                random.NextBytes(buffer);
                p = BitConverter.ToUInt64(buffer, 0);
            }
            while (!MillerRabin(p) || !BlumNumber(p));

            /* once for q */
            do
            {
                random.NextBytes(buffer);
                q = BitConverter.ToUInt64(buffer, 0);
            }
            while (!MillerRabin(q) || !BlumNumber(q));

            /* calculating n */
            n = p * q;
            return new Data { id = 0, k = k, n = n.ToString(), t = t, w = null };
        }

        static void ReceiveFromBob()
        {
            Console.WriteLine("Waiting for requests...\n");
            users = new List<Data>();

            while (true)
            {
                EndPoint endp = new IPEndPoint(IPAddress.Any, 0);

                /* check if valid Data object */
                serializer = new XmlSerializer(typeof(Data));
                byte[] buffer = new byte[1024];
                socketBob.ReceiveFrom(buffer, ref endp);
                stream = new MemoryStream(buffer);
                Data request = (Data)serializer.Deserialize(stream);
                stream.Close();

                IPEndPoint ipendp = (IPEndPoint)endp;
                /* requested variables (n, k and t) */
                if (request.id == 0)
                {
                    /* serialize data object (has the variables in it) */
                    serializer = new XmlSerializer(typeof(Data));
                    stream = new MemoryStream();
                    serializer.Serialize(stream, data);
                    /* send variables */
                    socketBob.SendTo(stream.ToArray(), endp);
                    stream.Close();
                    Console.WriteLine(ipendp.Address + ": Requested variables");
                }
                /* Bob already has variables and has now sent the w's */
                else
                {
                    users.Add(request);
                    Console.WriteLine(ipendp.Address + ": User with ID " + request.id + " is now in the database!\n");

                    /* Acknowledgement: send same package back */
                    serializer = new XmlSerializer(typeof(Data));
                    stream = new MemoryStream();
                    serializer.Serialize(stream, request);

                    /* send ACK */
                    socketBob.SendTo(stream.ToArray(), endp);
                    stream.Close();
                }                
            }
        }

        static void ReceiveFromAlice()
        {           
            while (true)
            {
                EndPoint endp = new IPEndPoint(IPAddress.Any, 0);

                /* request from Alice (wants the w's of Bob) */
                serializer = new XmlSerializer(typeof(Data));
                byte[] buffer = new byte[1024];
                socketAlice.ReceiveFrom(buffer, ref endp);
                stream = new MemoryStream(buffer);
                Data request = (Data)serializer.Deserialize(stream);
                stream.Close();

                foreach (Data data in users)
                {
                    if (request.id == data.id)
                    {
                        /* get the w's of Bob */                        
                        serializer = new XmlSerializer(typeof(Data));
                        stream = new MemoryStream();
                        serializer.Serialize(stream, data);

                        /* send w's to Alice */
                        socketAlice.SendTo(stream.ToArray(), new IPEndPoint(((IPEndPoint)endp).Address, 5554));
                        stream.Close();
                    }
                }
            }
        }

        private static bool BlumNumber(BigInteger number)
        {
            if (number % 4 == 3)
                return true;
            return false;
        }

        private static bool MillerRabin(BigInteger number)
        {
            /* explicit test of 2, 3 and 5 */
            /* if (number == 2 || number == 3 || number == 5) return true; */

            /* if number is either dividable by 2 or smaller than 2 */
            if (number < 2 || (number & 1) == 0) return false;

            /* the usage of BigIntegers makes it possible to test very high numbers, although it is a bit slower */
            BigInteger d = number - 1;
            int s = 0;

            /* d and s help us to skip a few numbers */
            while ((d & 1) == 0) { d /= 2; s++; }

            /* after four successful runs the probability of a prime number is already over 99,96% */
            for (int a = 2; a < 6; a++)
            {
                /* Fermat's little theorem */
                BigInteger x = BigInteger.ModPow(a, d, number);
                if (x == 1 || x == number - 1)
                    continue;

                for (int r = 1; r < s; r++)
                {
                    x = BigInteger.ModPow(x, 2, number);
                    if (x == 1) return false;
                    if (x == number - 1) break;
                }

                if (x != number - 1) return false;
            }
            /* if the four loops were all successful, then we have a prime number */
            return true;
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
            if (id == obj.id && n == obj.n && w == obj.w && k == obj.k && t == obj.t)
                return true;
            return false;
        }
    }
}
