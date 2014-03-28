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

            /* get values from verification center */



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
            BigInteger[] s;
            byte c;

            /* binary vektor for the sign */
            c = (byte)r.Next(-128, 127);

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
    }
}
