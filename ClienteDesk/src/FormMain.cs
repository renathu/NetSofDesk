using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.IO;
using System;
using System.Reflection.Emit;
using System.Drawing.Imaging;
using System.Timers;

namespace ClienteDesk.src
{
    public partial class FormMain : Form
    {
        private const int discoveryPort = 5000;
        private const string discoveryIp = "127.0.0.1";
        private int ClientPort = 6000; // Porta onde o cliente ouve conexões
        private static string clientId;
        private bool conectadoServidor = false;
        private TcpClient discoveryClient = null;
        private SslStream sslStream = null;
        private TcpClient targetClient = null;
        private SslStream targetSslStream = null;

        private TcpListener listener = null;

        // Configurar certificado do cliente para autenticação
        private static X509Certificate2 ClientCertificate;

        private System.Timers.Timer timerServidor = null;
        private bool flagTimer = false;

        public FormMain()
        {
            InitializeComponent();

            timerServidor = new System.Timers.Timer();
            timerServidor.Interval = TimeSpan.FromSeconds(10).TotalMilliseconds;
            timerServidor.Elapsed += TimerServidor_Elapsed;
        }

        private void CarregarId()
        {
            string id = string.Empty;

            if (File.Exists("id.json"))
            {
                string json = File.ReadAllText("id.json");
                var data = JsonSerializer.Deserialize<JsonData>(json);
                id = data?.Id;
            }
            else
            {
                // Instanciar o gerador de números aleatórios
                Random random = new Random();

                // Criar um buffer para armazenar o ID
                char[] idBuffer = new char[8];

                // Preencher o buffer com dígitos aleatórios
                for (int i = 0; i < 8; i++)
                {
                    // Gerar um dígito aleatório entre 0 e 9
                    idBuffer[i] = (char)('0' + random.Next(0, 10));
                }

                // Criar uma string a partir do buffer
                id = (new string(idBuffer) + DateTime.Now.ToString("mmss"));

                //var data = new JsonData { Id = id };
                //string json = JsonSerializer.Serialize(data);
                //File.WriteAllText("id.json", json);
            }

            clientId = id;
            lbMeuID.Text = id;
        }

        private void ConectarServidor()
        {
            if (ClientCertificate == null)
            {
                MessageBox.Show("Não foi possivel localizar o certificado", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ClientPort = Convert.ToInt32("60" + DateTime.Now.ToString("ss"));

            try
            {
                discoveryClient = new TcpClient(discoveryIp, discoveryPort);
                var networkStream = discoveryClient.GetStream();

                sslStream = new SslStream(networkStream, false, new RemoteCertificateValidationCallback(CertificateValidationCallback));

                //// Autenticar com o servidor de descoberta            
                sslStream.AuthenticateAsClient(new SslClientAuthenticationOptions
                {
                    TargetHost = discoveryIp,
                    ClientCertificates = new X509CertificateCollection { ClientCertificate },
                    EnabledSslProtocols = SslProtocols.Tls,
                    AllowRenegotiation = false
                });

                string ipExtern = string.Empty;
                using (HttpClient httpClient = new HttpClient())
                {
                    // Bloqueia até a resposta ser recebida
                    var response = httpClient.GetStringAsync("https://api.ipify.org?format=text").Result;
                    ipExtern = response.Trim();
                }

                // Obtém o nome do host
                string hostName = Dns.GetHostName();

                // Obtém os endereços IP associados ao nome do host
                var ipLocal = Dns.GetHostAddresses(hostName).Where(f => f.AddressFamily == AddressFamily.InterNetwork).First();

                // Register with the discovery server                
                var endpoint = $"{ipExtern}:{ipLocal}:{ClientPort}";
                var registerMessage = $"REGISTER:{clientId};{endpoint}";
                var registerData = Encoding.UTF8.GetBytes(registerMessage);
                sslStream.Write(registerData, 0, registerData.Length);
                sslStream.Flush();

                // Receive response from the discovery server
                var responseBuffer = new byte[1024];
                var responseBytesRead = sslStream.Read(responseBuffer, 0, responseBuffer.Length);
                var registerResponse = Encoding.UTF8.GetString(responseBuffer, 0, responseBytesRead);

                if (registerResponse != "Registered successfully")
                {
                    MessageBox.Show($"Não foi possivel registar o ID: {lbMeuID.Text} no servidor", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                conectadoServidor = true;
                timerServidor.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possivel conectar ao servidor", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                discoveryClient?.Close();
            }
        }

        private bool ObterDadosCliente()
        {
            if (tbxIdParceiro.Text.Length != 12)
            {
                MessageBox.Show("Informe o ID do parceiro com 12 digitos", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            flagTimer = true;

            var requestMessage = $"GET:{tbxIdParceiro.Text}";
            var requestData = Encoding.UTF8.GetBytes(requestMessage);
            sslStream.Write(requestData, 0, requestData.Length);
            sslStream.Flush();

            // Receive response with the target client's endpoint
            var buffer = new byte[1024];
            var bytesRead = sslStream.Read(buffer, 0, buffer.Length);
            var endpointString = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            if (endpointString == "Client not found")
            {
                MessageBox.Show($"Não foi encontrado parceiro com o ID: {tbxIdParceiro.Text}", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                flagTimer = true;
                return false;
            }
            else if (endpointString == "Invalid request")
            {
                MessageBox.Show($"Requisiçao inválida", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                flagTimer = true;
                return false;
            }

            flagTimer = true;
            var targetEndpoint = ParseEndpoint(endpointString);
            return ConectarCliente(targetEndpoint);
        }

        private bool ConectarCliente(IPEndPoint targetEndpoint)
        {
            var ip = targetEndpoint.Address.ToString() == "0.0.0.0" ? "127.0.0.1" : targetEndpoint.Address.ToString();
            targetClient = new TcpClient(ip, targetEndpoint.Port);
            var targetNetworkStream = targetClient.GetStream();

            targetSslStream = new SslStream(targetNetworkStream, false, new RemoteCertificateValidationCallback(CertificateValidationCallback));

            //// Autenticar com o servidor de descoberta
            var hostName = targetEndpoint.Address.ToString();
            targetSslStream.AuthenticateAsClient(new SslClientAuthenticationOptions
            {
                TargetHost = hostName,
                ClientCertificates = new X509CertificateCollection { ClientCertificate },
                EnabledSslProtocols = SslProtocols.Tls,
                AllowRenegotiation = false
            });            

            // Read response from target client
            //var responseBuffer2 = new byte[1024];
            //var responseBytesRead2 = targetSslStream.Read(responseBuffer2, 0, responseBuffer2.Length);
            //var responseMessage = Encoding.UTF8.GetString(responseBuffer2, 0, responseBytesRead2);          

            return true;   
        }

        private IPEndPoint ParseEndpoint(string endpointString)
        {
            var parts = endpointString.Split(':');
            if (parts.Length != 2) throw new FormatException("Invalid endpoint format");

            var address = IPAddress.Parse(parts[0]);
            var port = int.Parse(parts[1]);
            return new IPEndPoint(address, port);
        }

        private void StartListeningAsync()
        {
            listener = new TcpListener(IPAddress.Any, Convert.ToInt32(ClientPort));
            listener.Start();


            while (true)
            {
                try
                {
                    var tcpClient = listener.AcceptTcpClient();
                    _ = Task.Run(() => HandleIncomingConnection(tcpClient));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao iniciar o aceite de conexões", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void HandleIncomingConnection(TcpClient tcpClient)
        {
            try
            {
                var networkStream = tcpClient.GetStream();
                var sslStreamReceive = new SslStream(networkStream, false);

                // Authenticate the server
                sslStreamReceive.AuthenticateAsServer(ClientCertificate,
                                                false,
                                                System.Security.Authentication.SslProtocols.Tls,
                                                false);

                while (true)
                {
                    var image = CaptureScreen();
                    var compressedImage = CompressImage(image);

                    // Envia o tamanho da imagem seguido pelos dados da imagem
                    var lengthBuffer = BitConverter.GetBytes(compressedImage.Length);
                    sslStreamReceive.Write(lengthBuffer, 0, lengthBuffer.Length);
                    sslStreamReceive.Flush();
                    sslStreamReceive.Write(compressedImage, 0, compressedImage.Length);
                    sslStreamReceive.Flush();

                    Thread.Sleep((int)TimeSpan.FromSeconds(2).TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao lidar com conexão de entrada: {ex.Message}", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap CaptureScreen()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            var screenshot = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            return screenshot;
        }

        private byte[] CompressImage(Bitmap image)
        {
            using (var ms = new MemoryStream())
            {
                var encoder = GetEncoder(ImageFormat.Jpeg);
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L); // Ajuste a qualidade conforme necessário

                image.Save(ms, encoder, encoderParams);
                return ms.ToArray();
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        static bool CertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; // Certificado é válido
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CarregarId();

            if (File.Exists("certificate.pfx"))
            {
                ClientCertificate = new X509Certificate2("certificate.pfx", "HyRj4178UeKurh45748785");
            }

            ConectarServidor();

            if (conectadoServidor)
            {
                // Start listening for incoming connections
                _ = Task.Run(() => StartListeningAsync());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timerServidor.Stop();
            discoveryClient?.Close();
            targetClient?.Close();
            listener?.Stop();
        }

        private void TimerServidor_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (flagTimer == false)
            {
                // Ping            
                var registerData = Encoding.UTF8.GetBytes($"PING:{clientId}");
                sslStream.Write(registerData, 0, registerData.Length);
                sslStream.Flush();
            }
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            if (conectadoServidor)
            {
                if(ObterDadosCliente() == true)
                {
                    FrmViewerClient frmViewerClient = new FrmViewerClient();
                    frmViewerClient.targetSslStream = targetSslStream;
                    frmViewerClient.targetTcpClient = targetClient;
                    frmViewerClient.Show();
                }
            }
        }

        private void lbMeuID_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(lbMeuID.Text);
        }
    }
}
