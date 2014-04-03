﻿using System;
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
        static Socket socket;
        static Thread thread;
        static Data data;
        static XmlSerializer serializer;
        static MemoryStream stream;
        static Random random;
        const int PORT = 5555;
        const int k = 8;
        const int t = 3;

        static void Main(string[] args)
        {
            Console.Title = "Verification Center";

            random = new Random();
            data = InitValues();

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            thread = new Thread(ReceiveRequests);
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
            return new Data { id = 0, k = k, n = n, t = t, w = null };
        }

        static void ReceiveRequests()
        {
            Console.WriteLine("Waiting for requests...\n");

            while (true)
            {
                EndPoint endp = new IPEndPoint(IPAddress.Any, 0);

                /* check if valid Data object */
                serializer = new XmlSerializer(typeof(Data));
                byte[] buffer = new byte[1024];
                socket.ReceiveFrom(buffer, ref endp);
                stream = new MemoryStream(buffer);
                Data request = (Data)serializer.Deserialize(stream);
                stream.Close();

                IPEndPoint ep = (IPEndPoint)endp;
                Console.WriteLine(ep.Address + ": Requested verification");
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
        public BigInteger n { get; set; }
        public BigInteger[] w { get; set; }
        public int k { get; set; }
        public int t { get; set; }
    }
}
