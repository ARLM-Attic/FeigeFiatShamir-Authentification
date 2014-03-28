using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Bob
{
    class Program
    {
        static Socket socket;
        static IPEndPoint endp;
        static Random r;
        static BigInteger n;
        static BigInteger[] w;
        static int id;
        /* constants */
        const int k = 8;
        const int t = 3;

        static void Main(string[] args)
        {
            Console.Title = "FeigeFiatShamir-Authentification";
            Console.WriteLine("Welcome to the FeigeFiatShamir-Authentification Service!\n");
            
            r = new Random();

            InitValuesForIdentification(out n, out id, out w, k, t);

            /* Send that shit to verification center */
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            bool validIP = false;
            while (!validIP)
            {
                Console.Write("\nEnter IP address of verification center: ");
                try
                {
                    endp = new IPEndPoint(IPAddress.Parse(Console.ReadLine()), 5555);
                    validIP = true;
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid IP address");
                }
            }

            socket.SendTo(Encoding.Default.GetBytes("Test"), endp);
            Console.WriteLine("Waiting for verification...");

            Console.ReadLine();
        }

        private static void InitValuesForIdentification(out BigInteger n, out int id, out BigInteger[] w, int k, int t)
        {
            BigInteger p, q;
            BigInteger[] s;
            byte c;
            byte[] buffer = new byte[sizeof(UInt64)];

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

            /* binary vektor for the sign */
            c = (byte)r.Next(-128, 127);

            Console.WriteLine("p = " + p);
            Console.WriteLine("q = " + q);
            Console.WriteLine("n = " + n + "\n");

            /* calculating elements of s and w */
            s = new BigInteger[k];
            w = new BigInteger[k];   

            int length = n.ToByteArray().Length;            
            byte[] tmp = new byte[length];
            BigInteger invElement;

            for (int i = 0; i < k; i++)
            {
                /* searching for an inverse element */
                invElement = 0;

                while (invElement == 0)
                {
                    r.NextBytes(tmp);
                    s[i] = new BigInteger(tmp);
                    s[i] = BigInteger.Abs(s[i]) % n;

                    invElement = GetInverseElement(n, (s[i] << 2) % n);
                }

                if ((c & (1 << i)) != 0)
                    w[i] = BigInteger.Pow(-1, i) * s[i];

                else
                    w[i] = s[i];

                Console.WriteLine("w[" + i + "]= " + w[i]);
            }

            id = r.Next();
        }

        private static BigInteger GetInverseElement(BigInteger n, BigInteger s)
        {
            BigInteger ggT, x, y;

            Euklid(n, s, out ggT, out x, out y);

            /* error: ggT has to be 1 */
            if(ggT != 1)                
                return 0;

            /* y is our inverse element, because (y * s) mod n equals 1 */
            return y;
        }

        private static void Euklid(BigInteger a, BigInteger b, out BigInteger ggT, out BigInteger x, out BigInteger y)
        {
            if (a % b == 0)
            {
                x = 0;
                y = 1;
                ggT = b * y;
                return;
            }

            BigInteger dX, dY, remainder;
            /* repeatedly call the function till a % b equals 0 */ 
            Euklid(b, a % b, out ggT, out dX, out dY);

            /* dY is our first coefficient */
            x = dY;
            /* calculating the second coefficient */
            y = dX - x * BigInteger.DivRem(a, b, out remainder);

            /* updating ggT */
            ggT = a * x + b * y;
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
}
