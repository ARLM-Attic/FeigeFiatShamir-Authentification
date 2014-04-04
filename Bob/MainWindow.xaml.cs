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
        EndPoint endp;
        Socket socket;
        Random random;
        XmlSerializer serializer;
        MemoryStream stream;
        const int PORT = 5555;
        byte[] buffer;

        /* Feige-Fiat-Shamir stuff */
        int id, k, t;
        BigInteger n;
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
            endp = new IPEndPoint(address, PORT);
     
            /* setup the request */
            serializer = new XmlSerializer(typeof(Data));
            stream = new MemoryStream();
            /* if the id is set to 0, the authentification server knows its a request */
            serializer.Serialize(stream, new Data { id = 0, k = 0, n = null, t = 0, w = null });

            /* send request */
            socket.SendTo(stream.ToArray(), endp);
            stream.Close();

            /* get response with n, k and t in it */
            buffer = new byte[1024];
            socket.ReceiveFrom(buffer, ref endp);
            stream = new MemoryStream(buffer);
            Data response = (Data)serializer.Deserialize(stream);
            /* initialize n, k and t */
            n = BigInteger.Parse(response.n);
            k = response.k;
            t = response.t;
            stream.Close();

            /* send the calculated w's */
            InitValuesForIdentification(n, out id, out w, k, t);
            stream = new MemoryStream();
            Data data_w = new Data { id = id, k = 0, n = null, t = 0, w = w };
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
                MessageBox.Show("Unbekannter Fehler");
                return;
            }
        }

        private void btn_authentification_Click(object sender, RoutedEventArgs e)
        {
            /* send authenitification request to Alice */
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
            endp = new IPEndPoint(address, PORT);
            stream = new MemoryStream();
            Data request = new Data { id = id, k = 0, n = null, t = 0, w = null };
            serializer.Serialize(stream, request);
            /* send request */
            socket.SendTo(stream.ToArray(), endp);
            stream.Close();
        }

        private void InitValuesForIdentification(BigInteger n, out int id, out string[] w, int k, int t)
        {
            BigInteger[] s = new BigInteger[k];
            BigInteger[] wBig = new BigInteger[k];
            w = new string[k];

            /* binary vektor for the sign */
            byte c = (byte)random.Next(-128, 127);

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

                    invElement = GetInverseElement(n, (s[i] << 2) % n);
                }

                if ((c & (1 << i)) != 0)
                    wBig[i] = BigInteger.Pow(-1, i) * s[i];

                else
                    wBig[i] = s[i];
            }
            /* generate the ID */
            id = random.Next();

            /* convert the BigInteger values to String */
            for (int i = 0; i < k; i++)
                w[i] = wBig[i].ToString();
        }

        private BigInteger GetInverseElement(BigInteger n, BigInteger s)
        {
            BigInteger ggT, x, y;

            Euklid(n, s, out ggT, out x, out y);

            /* error: ggT has to be 1 */
            if (ggT != 1)
                return 0;

            /* y is our inverse element, because (y * s) mod n equals 1 */
            return y;
        }

        private void Euklid(BigInteger a, BigInteger b, out BigInteger ggT, out BigInteger x, out BigInteger y)
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
