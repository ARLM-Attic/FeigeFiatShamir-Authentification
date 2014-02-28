﻿using System;
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
        static Socket s;
        static Random r;
        static BigInteger p, q, n;
        static byte c;
        /* constants */
        const int k = 8;
        const int t = 3;

        static void Main(string[] args)
        {
            Console.Title = "FeigeFiatShamir-Authentification";
            Console.WriteLine("Welcome to the FeigeFiatShamir-Authentification Service!\n");

            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            r = new Random();

            Keyscheduling(ref p, ref q, ref c);

            Console.ReadLine();
        }

        private static void Keyscheduling(ref BigInteger p, ref BigInteger q, ref byte c)
        {
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
            Console.WriteLine("n = " + n);
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