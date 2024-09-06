using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ServidorDesk.src
{
    public class DiscoveryServer
    {
        private const int Port = 5000;
        private static readonly ConcurrentDictionary<string, (string, string, IPEndPoint)> Clients = new ConcurrentDictionary<string, (string, string, IPEndPoint)>();

        // Gerar um certificado autoassinado para SSL/TLS
        private static X509Certificate2 Certificate = null;

        public static async Task Iniciar()
        {
            Certificate = new X509Certificate2("certificate.pfx", "HyRj4178UeKurh45748785");

            var listener = new TcpListener(IPAddress.Any, Port);           
            listener.Start();
            Console.WriteLine($"Discovery Server listening on port {Port}...");

            while (true)
            {
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(tcpClient));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting incoming connection: {ex.Message}");
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient tcpClient)
        {
            try
            {
                using var networkStream = tcpClient.GetStream();
                using var sslStream = new SslStream(networkStream, false);                

                // Authenticate the server
                await sslStream.AuthenticateAsServerAsync(Certificate,
                                                false,
                                                System.Security.Authentication.SslProtocols.Tls,
                                                false);

                var buffer = new byte[1024];

                while (true)
                {
                    try
                    {
                        // Leia a solicitação do cliente
                        var bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            // O cliente fechou a conexão
                            Console.WriteLine("Client disconnected.");
                            break;
                        }

                        var requestMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        var parts = requestMessage.Split(':', 2);
                        string responseMessage = "Invalid request";

                        if (parts.Length == 2)
                        {
                            var command = parts[0];
                            var data = parts[1];

                            switch (command)
                            {
                                case "REGISTER":
                                    responseMessage = RegisterClient(data, tcpClient);
                                    break;
                                case "GET":
                                    responseMessage = GetClientEndpoint(data, tcpClient);
                                    break;
                                case "PING":
                                    //Mantendo a conexão aberta
                                    break;
                            }                            
                        }                        

                        // Envie a resposta de volta ao cliente
                        var responseData = Encoding.UTF8.GetBytes(responseMessage);
                        await sslStream.WriteAsync(responseData, 0, responseData.Length);
                        await sslStream.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing client request: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client connection: {ex.Message}");
            }
            finally
            {
                tcpClient.Close();
            }
        }        

        private static string RegisterClient(string data, TcpClient tcpClient)
        {
            var parts = data.Split(';');
            if (parts.Length != 2) return "Invalid registration data";

            var id = parts[0];
            var endpointData = parts[1];

            if (Clients.ContainsKey(id))
            {
                return "ID already registered";
            }

            Console.WriteLine($"Cliente com ID: {id} foi registrado");

            IPEndPoint remoteEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            var partsRemoteEndPoint = endpointData.Split(':');

            var endpoint = new IPEndPoint(remoteEndPoint.Address, Convert.ToInt32(partsRemoteEndPoint[2]));
            Clients[id] = (partsRemoteEndPoint[0], partsRemoteEndPoint[1], endpoint);
            return "Registered successfully";
        }        

        private static string GetClientEndpoint(string id, TcpClient tcpClient)
        {
            IPEndPoint remoteEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;

            if (Clients.TryGetValue(id, out var endpoint))
            {
                if(remoteEndPoint.Address.ToString() == endpoint.Item3.Address.ToString())
                {
                    return $"{endpoint.Item3.Address}:{endpoint.Item3.Port}";
                }
                else
                {
                    return $"{endpoint.Item1}:{endpoint.Item3.Port}";
                }
            }

            return "Client not found";
        }        
    }
}
