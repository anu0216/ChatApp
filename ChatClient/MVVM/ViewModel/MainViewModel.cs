using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatClient.MVVM.Core;
using ChatClient.MVVM.Model;
using ChatClient.Net;
using ChatClient.Firebase;

namespace ChatClient.MVVM.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<UserModel> Users { get; set; } = new ObservableCollection<UserModel>();
        public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
        public ICommand ConnectToServerCommand { get; set; }
        public ICommand SendMessageCommand { get; set; }

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set
            {
                if (_message != value)
                {
                    _message = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly Server _server;
        private readonly FirebaseHelper _firebaseHelper;

        public MainViewModel()
        {
            _server = new Server();
            _firebaseHelper = new FirebaseHelper();
            _server.MsgReceivedEvent += OnMessageReceived;
            _server.UserConnectedEvent += OnUserConnected;
            _server.UserDisconnectEvent += OnUserDisconnected;

            ConnectToServerCommand = new RelayCommand(async o => await ConnectToServer(), o => !string.IsNullOrEmpty(Username));
            SendMessageCommand = new RelayCommand(async o => await SendMessage(), o => !string.IsNullOrEmpty(Message));
        }

        private async Task ConnectToServer()
        {
            _server.ConnectToServer(Username);
            await LoadMessages();
        }

        private async Task LoadMessages()
        {
            if (Users.Count == 2)
            {
                var roomName = GetRoomName(Users[0].Username, Users[1].Username);
                var messages = await _firebaseHelper.LoadMessagesAsync(roomName);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();
                    foreach (var message in messages)
                    {
                        Messages.Add(message);
                    }
                });
            }
        }

        private async Task SendMessage()
        {
            var roomName = GetRoomName(Users[0].Username, Users[1].Username);
            await _server.SendMessageToServer(Message, roomName);
            await _firebaseHelper.SaveMessageAsync(roomName, Username, Message);
            Message = string.Empty; // 메시지 전송 후 입력창 비우기
        }

        private void OnMessageReceived()
        {
            if (_server.PacketReader != null)
            {
                var msg = _server.PacketReader.ReadMessage();
                Application.Current.Dispatcher.Invoke(() => Messages.Add(msg));
            }
        }

        private async void OnUserConnected(string username, string uid)
        {
            var user = new UserModel
            {
                Username = username,
                UID = uid,
            };

            if (!Users.Any(x => x.UID == user.UID))
            {
                Application.Current.Dispatcher.Invoke(() => Users.Add(user));
                await LoadMessages(); // 사용자 연결 시 메시지 불러오기 시도
            }
        }

        private void OnUserDisconnected()
        {
            if (_server.PacketReader != null)
            {
                var uid = _server.PacketReader.ReadMessage();
                var user = Users.FirstOrDefault(x => x.UID == uid);
                if (user != null)
                {
                    Application.Current.Dispatcher.Invoke(() => Users.Remove(user));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string GetRoomName(string user1, string user2)
        {
            var users = new List<string> { user1, user2 };
            users.Sort();
            return $"ChatRoom_{users[0]}_{users[1]}";
        }
    }
}
