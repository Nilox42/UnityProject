using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace UnityServer
{
    public class Server
    {
        public static int MaxPlayer { get; set; }
        public static int Port { get; set; }

        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int fromClient, Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static UdpClient udpListeneer;

        public static void Start(int maxplayers, int port)
        {
            MaxPlayer = maxplayers;
            Port = port;

            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectionCallback), null);

            udpListeneer = new UdpClient(port);
            udpListeneer.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on {Port}");
        }

        private static void TCPConnectionCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectionCallback), null);
            Console.WriteLine($"Incomming connection from {client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= MaxPlayer; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(client);
                    return;
                }
            }

            Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
        }

        private static void UDPReceiveCallback(IAsyncResult result)
        {
            try
            {
                IPEndPoint clientENdpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListeneer.EndReceive(result, ref clientENdpoint);
                udpListeneer.BeginReceive(UDPReceiveCallback, null);

                if (data.Length < 4)
                {
                    return;
                }

                using (Packet packet = new Packet(data))
                {
                    int clientid = packet.ReadInt();

                    if (clientid == 0)
                    {
                        return;
                    }

                    if (clients[clientid].udp.endPoint == null)
                    {
                        clients[clientid].udp.Connect(clientENdpoint);
                        return;
                    }

                    if (clients[clientid].udp.endPoint.ToString() == clientENdpoint.ToString())
                    {
                        clients[clientid].udp.HandleData(packet);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error receiving UDP data: {e.Message}");
            }
        }

        public static void SendUDPData(IPEndPoint clientendpoint, Packet packet)
        {
            try
            {
                if (clientendpoint != null)
                {
                    udpListeneer.BeginSend(packet.ToArray(), packet.Length(), clientendpoint, null, null);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending data to {clientendpoint} via UDP: {e}");
            }
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayer; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived }
            };
            Console.WriteLine("Initialized packets.");
        }
    }
}
