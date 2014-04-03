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
        IPAddress address;
        EndPoint endp;
        Socket socket;
        Data data;
        Random random;
        XmlSerializer serializer;
        MemoryStream stream;
        int id;
        BigInteger[] w;
        const int PORT = 5555;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btn_send_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                address = IPAddress.Parse(tbx_ip.Text);
            }
            catch
            {
                tbx_ip.Text = "";
                MessageBox.Show("Please enter a valid IP address!");
                return;
            }

            random = new Random();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            endp = new IPEndPoint(address, PORT);
     
            /* setup the request */
            serializer = new XmlSerializer(typeof(Data));
            stream = new MemoryStream();
            /* if the id is set to 0, the authentification server knows its a request */
            serializer.Serialize(stream, new Data { id = 0, k = 1, n = 2, t = 3, w = null });

            /* send request */
            socket.SendTo(stream.ToArray(), endp);
            stream.Close();

            /* get response */
            byte[] buffer = new byte[1024];
            socket.ReceiveFrom(buffer, ref endp);
            stream = new MemoryStream(buffer);
            Data response = (Data)serializer.Deserialize(stream);
            stream.Close();

            /* if received package is ok... */
            btn_send.IsEnabled = false;

            /* send the calculated w's */
            InitValuesForIdentification(response.n, out id, out w, response.k, response.t);
        }

        private void InitValuesForIdentification(BigInteger n, out int id, out BigInteger[] w, int k, int t)
        {
            BigInteger[] s;
            /* binary vektor for the sign */
            byte c = (byte)random.Next(-128, 127);

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
                    random.NextBytes(tmp);
                    s[i] = new BigInteger(tmp);
                    s[i] = BigInteger.Abs(s[i]) % n;

                    invElement = GetInverseElement(n, (s[i] << 2) % n);
                }

                if ((c & (1 << i)) != 0)
                    w[i] = BigInteger.Pow(-1, i) * s[i];

                else
                    w[i] = s[i];
            }
            id = random.Next();
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
        public BigInteger n { get; set; }
        public BigInteger[] w { get; set; }
        public int k { get; set; }
        public int t { get; set; }       
    }
}
