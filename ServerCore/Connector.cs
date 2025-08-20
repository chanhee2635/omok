using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    public class Connector
    {
        // 서버연결 시 호출할 콜백 메서드를 등록
        public Action<UserToken> callbackConnected;

        // 서버소켓
        Socket _socket;

        NetworkService _networkService;

        public Connector(NetworkService networkService)
        {
            _networkService = networkService;
            callbackConnected = null;
        }

        public void Connect(IPEndPoint endpoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs event_arg = new SocketAsyncEventArgs();
            event_arg.Completed += OnConnectCompleted;
            event_arg.RemoteEndPoint = endpoint;
            if (!_socket.ConnectAsync(event_arg))
                OnConnectCompleted(null, event_arg);
        }

        void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                UserToken token = new UserToken();

                _networkService.OnConnectCompleted(_socket, token);

                callbackConnected.Invoke(token);
            }
            else
            {
                Console.WriteLine(string.Format("Failed to connect. {0}", args.SocketError));
            }
        }
    }
}
