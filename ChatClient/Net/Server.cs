using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ChatClient.Net.IO;
using ChatClient.Firebase;

namespace ChatClient.Net
{
    public class Server
    {
        private readonly TcpClient _client;
        public PacketReader? PacketReader { get; private set; }
        private readonly FirebaseHelper _firebaseHelper;

        public event Action? MsgReceivedEvent;
        public event Action<string, string>? UserConnectedEvent;
        public event Action? UserDisconnectEvent;

        private string _username = string.Empty;

        public Server()
        {
            _client = new TcpClient();
            _firebaseHelper = new FirebaseHelper();
        }

        public void ConnectToServer(string username)
        {
            if (!_client.Connected)
            {
                _username = username;
                _client.Connect("127.0.0.1", 7891);
                PacketReader = new PacketReader(_client.GetStream());

                if (!string.IsNullOrEmpty(username))
                {
                    var connectPacket = new PacketBuilder();
                    connectPacket.WriteOpCode(0);
                    connectPacket.WriteString(username);
                    _client.Client.Send(connectPacket.GetPacketBytes());
                }

                ReadPackets();
            }
        }

        private void ReadPackets()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (PacketReader == null)
                        continue;

                    var opcode = PacketReader.ReadByte();
                    switch (opcode)
                    {
                        case 1:
                            var username = PacketReader.ReadMessage();
                            var uid = PacketReader.ReadMessage();
                            UserConnectedEvent?.Invoke(username, uid);
                            break;
                        case 5:
                            MsgReceivedEvent?.Invoke();
                            break;
                        case 10:
                            UserDisconnectEvent?.Invoke();
                            break;
                        default:
                            Console.WriteLine("Unknown opcode received...");
                            break;
                    }
                }
            });
        }

        public async Task SendMessageToServer(string message, string roomName)
        {
            var messagePacket = new PacketBuilder();
            messagePacket.WriteOpCode(5);
            messagePacket.WriteString(message);
            _client.Client.Send(messagePacket.GetPacketBytes());

            await _firebaseHelper.SaveMessageAsync(roomName, _username, message);
        }
    }
}
