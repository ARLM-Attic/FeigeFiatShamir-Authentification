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

namespace VerificationCenter
{
    class Program
    {
        static Socket socket;
        static Thread thread;
        static Data data;
        static Random r;

        static void Main(string[] args)
        {
            Console.Title = "Verification Center";

           
            data = InitValues();

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            thread = new Thread(Receive);
            thread.Start();

            Console.ReadLine();
        }

        static Data InitValues()
        {
            BigInteger p, q, n;
            byte[] buffer = new byte[sizeof(UInt64)];
            /* constants */
            const int k = 8;
            const int t = 3;

            /* once for p */
            do
            {
                r.NextBytes(buffer);
                p = BitConverter.ToUInt64(buffer, 0);
            }
            while (!MillerRabin(p) || !BlumNumber(p));

            /* once for q */
            do
            {
                r.NextBytes(buffer);
                q = BitConverter.ToUInt64(buffer, 0);
            }
            while (!MillerRabin(q) || !BlumNumber(q));

            /* calculating n */
            n = p * q;
            return new Data(0, n, null, k, t);
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

    class Data
    {
        private int id;
        private BigInteger n;
        private BigInteger[] w;
        private int k;
        private int t;

        public Data(int id, BigInteger n, BigInteger[] w, int k, int t)
        {
            this.id = id;
            this.n = n;
            this.w = w;
            this.k = k;
            this.t = t;
        }

        public int Get_id()
        {
            return id;
        }

        public BigInteger Get_n()
        {
            return n;
        }

        public BigInteger[] Get_w()
        {
            return w;
        }

        public int Get_k()
        {
            return k;
        }

        public int Get_t()
        {
            return t;
        }
    }
}
