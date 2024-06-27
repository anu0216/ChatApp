using ChatServer.Net.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ChatServer
{
    class Program
    {
        private static readonly List<Client> _users = new();
        private static readonly TcpListener _listener = new(IPAddress.Parse("127.0.0.1"), 7891);

        static void Main(string[] _)
        {
            _listener.Start();

            while (true)
            {
                var client = new Client(_listener.AcceptTcpClient());
                _users.Add(client);
                SendUserList(client);
                BroadcastConnection(client);
            }
        }

        private static void SendUserList(Client client)
        {
            foreach (var usr in _users)
            {
                var userListPacket = new PacketBuilder();
                userListPacket.WriteOpCode(1);
                userListPacket.WriteString(usr.Username);
                userListPacket.WriteString(usr.UID.ToString());
                client.ClientSocket.Client.Send(userListPacket.GetPacketBytes());
            }
        }

        private static void BroadcastConnection(Client newClient)
        {
            foreach (var client in _users)
            {
                if (client == newClient) continue; // 이미 신규 클라이언트에게는 보냈으므로 생략
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(1);
                broadcastPacket.WriteString(newClient.Username);
                broadcastPacket.WriteString(newClient.UID.ToString());
                client.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }
        }

        public static void BroadcastMessage(string message)
        {
            foreach (var user in _users)
            {
                var msgPacket = new PacketBuilder();
                msgPacket.WriteOpCode(5);
                msgPacket.WriteString(message);
                user.ClientSocket.Client.Send(msgPacket.GetPacketBytes());
            }
        }

        public static void BroadcastDisconnect(string uid)
        {
            var disconnectedUser = _users.FirstOrDefault(x => x.UID.ToString() == uid);
            if (disconnectedUser != null)
            {
                _users.Remove(disconnectedUser);

                foreach (var user in _users)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(10);
                    broadcastPacket.WriteString(uid);
                    user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }

                BroadcastMessage($"[{disconnectedUser.Username}] Disconnected!");
            }
        }
    }
}