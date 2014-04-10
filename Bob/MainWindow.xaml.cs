using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO;

namespace Bob
{
    public partial class MainWindow : Window
    {
        /* Network stuff */
        IPAddress address;
        Socket socket;
        Random random;
        XmlSerializer serializer;
        MemoryStream stream;
        const int PORT_TC = 55556;
        const int PORT_ALICE = 55558;
        byte[] buffer;

        /* Feige-Fiat-Shamir stuff */
        int id, k, t;
        BigInteger n;
        BigInteger[] s;
        string[] w;        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                address = IPAddress.Parse(tbx_ip1.Text);
            }
            catch
            {
                tbx_ip1.Text = "";
                MessageBox.Show("Please enter a valid IP address!");
                return;
            }
            tbx_ip1.Text = "";

            random = new Random();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint endp = new IPEndPoint(address, PORT_TC);
     
            /* setup the request */
            serializer = new XmlSerializer(typeof(Data));
            stream = new MemoryStream();
            /* if the id is set to 0, the trust center knows its a request */
            serializer.Serialize(stream, new Data { id = 0 });
            /* send request */
            socket.SendTo(stream.ToArray(), endp);
            stream.Close();

            /* get response with n, k and t in it */
            buffer = new byte[2048];
            socket.ReceiveFrom(buffer, ref endp);
            stream = new MemoryStream(buffer);
            Data response = (Data)serializer.Deserialize(stream);
            /* initialize n, k and t */
            n = BigInteger.Parse(response.n);
            k = response.k;
            t = response.t;
            stream.Close();

            /* send the calculated w's */
            InitValuesForIdentification(n, out s, out id, out w, k, t);
            stream = new MemoryStream();
            Data data_w = new Data { id = id, w = w };
            serializer.Serialize(stream, data_w);
            socket.SendTo(stream.ToArray(), endp);
            stream.Close();

            /* wait for the acknowledgement (has to be the same packet) */
            buffer = new byte[1024];
            socket.ReceiveFrom(buffer, ref endp);
            stream = new MemoryStream(buffer);
            response = (Data)serializer.Deserialize(stream);
            stream.Close();

            /* comparing the objects */
            if (response.Compare(data_w))
            {
                btn_send.IsEnabled = false;
                btn_authentification.IsEnabled = true;
                tbx_ip1.IsEnabled = false;
                lbl_id.Content += id.ToString();
            }

            else
            {
                MessageBox.Show("Unknown error!");
                return;
            }
        }

        private void btn_authentification_Click(object sender, RoutedEventArgs e)
        {
            /* send authentification request to Alice */
            try
            {
                address = IPAddress.Parse(tbx_ip2.Text);
            }
            catch
            {
                tbx_ip2.Text = "";
                MessageBox.Show("Please enter a valid IP address!");
                return;
            }
            tbx_ip2.Text = "";

            /* setup request */
            stream = new MemoryStream();
            serializer.Serialize(stream, new Data { id = id });
            /* send request */
            socket.SendTo(stream.ToArray(), new IPEndPoint(address, 55553));
            stream.Close();

            /* wait for the OK to send the v's */
            buffer = new byte[2048];
            EndPoint endp = new IPEndPoint(address, 55558);
            socket.ReceiveFrom(buffer, ref endp);
            stream = new MemoryStream(buffer);
            Data ack = (Data)serializer.Deserialize(stream);
            stream.Close();

            for (int i = 0; i < t; i++)
            {
                byte[] tmp = new byte[n.ToByteArray().Length];
                random.NextBytes(tmp);
                BigInteger r = new BigInteger(tmp);
                r = BigInteger.Abs(r) % n;
                int bit = random.Next(0, 2);
                BigInteger v;
                /* calculate v */
                if (bit == 0)
                    v = BigInteger.ModPow(r, 2, n);
                else
                    v = BigInteger.ModPow(-r, 2, n);

                /* send v */
                stream = new MemoryStream();
                serializer.Serialize(stream, new Data { v = v.ToString() });
                socket.SendTo(stream.ToArray(), endp);
                stream.Close();

                /* get the binary vector */
                buffer = new byte[2048];
                socket.ReceiveFrom(buffer, ref endp);
                stream = new MemoryStream(buffer);
                Data b = (Data)serializer.Deserialize(stream);
                stream.Close();

                /* calculating u */
                BigInteger u = r;
                for (int j = 0; j < 8; j++)
                {
                    if ((b.b & (1 << j)) != 0)
                        u = u * s[j] % n;
                }

                /* send u */
                stream = new MemoryStream();
                serializer.Serialize(stream, new Data { u = u.ToString() });
                socket.SendTo(stream.ToArray(), endp);
                stream.Close();

                /* get the result */
                buffer = new byte[2048];
                socket.ReceiveFrom(buffer, ref endp);
                stream = new MemoryStream(buffer);
                Data res = (Data)serializer.Deserialize(stream);
                stream.Close();

                if (!res.LooksGoodForNow)
                {
                    MessageBox.Show("You are NOT authenticated!");
                    break;
                }

                if (i == t - 1 && res.LooksGoodForNow)
                    MessageBox.Show("You are authenticated!");
            }
        }

        private void InitValuesForIdentification(BigInteger n, out BigInteger[] s, out int id, out string[] w, int k, int t)
        {
            s = new BigInteger[k];
            BigInteger[] wBig = new BigInteger[k];
            w = new string[k];

            /* binary vektor for the sign */
            byte b = (byte)random.Next(-128, 127);

            int length = n.ToByteArray().Length;
            byte[] tmp = new byte[length];
            BigInteger invElement;

            for (int i = 0; i < k; i++)
            {
                /* searching for an inverse element */
                invElement = 0;

                while (invElement == 0)
                {
                    random.NextBytes(tmp);
                    s[i] = new BigInteger(tmp);
                    s[i] = BigInteger.Abs(s[i]) % n;

                    invElement = GetInverseElement(s[i] * s[i], n);
                }

                if ((b & (1 << i)) != 0)
                    wBig[i] = BigInteger.Pow(-1, i) * invElement;

                else
                    wBig[i] = invElement;
            }
            /* generate the ID */
            id = random.Next();

            /* convert the BigInteger values to String */
            for (int i = 0; i < k; i++)
                w[i] = wBig[i].ToString();
        }

        private static BigInteger GetInverseElement(BigInteger a, BigInteger b)
        {
            BigInteger dividend = a % b;
            BigInteger divisor = b;

            BigInteger last_x = BigInteger.One;
            BigInteger curr_x = BigInteger.Zero;

            while (divisor.Sign > 0)
            {
                BigInteger quotient = dividend / divisor;
                BigInteger remainder = dividend % divisor;
                if (remainder.Sign <= 0)
                    break;

                BigInteger next_x = last_x - curr_x * quotient;
                last_x = curr_x;
                curr_x = next_x;

                dividend = divisor;
                divisor = remainder;
            }

            /* error: ggT has to be 1 */
            if (divisor != 1)
                return 0;

            return (curr_x.Sign < 0 ? curr_x + b : curr_x);
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
