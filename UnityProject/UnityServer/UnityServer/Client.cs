using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Numerics;
using System.Net;
using System.Net.Sockets;

namespace UnityServer
{
    public class Client
    {
        public static int dataBuffersize = 4096;

        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        public Client(int clientid)
        {
            id = clientid;
            tcp = new TCP(id);
            udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] recieveBuffer;

            public TCP(int id)
            {
                this.id = id;
            }

            public void Connect(TcpClient socket)
            {
                this.socket = socket;
                socket.ReceiveBufferSize = dataBuffersize;
                socket.SendBufferSize = dataBuffersize;

                stream = socket.GetStream();

                receivedData = new Packet();
                recieveBuffer = new byte[dataBuffersize];

                stream.BeginRead(recieveBuffer, 0, dataBuffersize, ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome to the server!");
                Console.WriteLine($"{id} Welcome to the server!");
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error sending data to player {id} vie TCP: {e.Message}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int bytelength = stream.EndRead(result);
                    if (bytelength <= 0)
                    {
                        //TODO: Disconnect
                        return;
                    }

                    byte[] data = new byte[bytelength];
                    Array.Copy(recieveBuffer, data, bytelength);

                    receivedData.Reset(HandleData(data));

                    stream.BeginRead(recieveBuffer, 0, dataBuffersize, ReceiveCallback, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error recieving TCP data:{e.Message}");
                }
            }


            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;

                receivedData.SetBytes(_data);

                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }

                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            Server.packetHandlers[_packetId](id, _packet);
                        }
                    });

                    _packetLength = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        _packetLength = receivedData.ReadInt();
                        if (_packetLength <= 0)
                        {
                            return true;
                        }
                    }
                }

                if (_packetLength <= 1)
                {
                    return true;
                }

                return false;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint endPoint)
            {
                this.endPoint = endPoint;
            }

            public void SendData(Packet packet)
            {
                Server.SendUDPData(endPoint, packet);
            }

            public void HandleData(Packet packetData)
            {
                int packetLength = packetData.ReadInt();
                byte[] packetBytes = packetData.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        Server.packetHandlers[packetId](id, packet);
                    }
                });
            }
        }

        public void SendIntoGame(string playerName)
        {
            player = new Player(id, playerName, new Vector3(0,0,0) );

            foreach (Client client in Server.clients.Values)
            {
                if (client.player != null)
                {
                    if (client.id != id)
                    {
                        ServerSend.SpawnPlayer(id,client.player);
                    }
                }
            }
            foreach (Client client in Server.clients.Values)
            {
                if (client.player != null)
                {
                    ServerSend.SpawnPlayer(client.id, player);
                }
            }
        }
    }


    
}
