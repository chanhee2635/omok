using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    class Listener
    {
        Socket _listenSocket;
        public Action<Socket> callbackOnNewClient;

        public Listener()
        {
            callbackOnNewClient = null;
        }

        public void Init(string host, int port, int backlog, int register = 5)
        {
            // TCP 소켓 생성
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress address;
            if (host == "0.0.0.0")
                address = IPAddress.Any;
            else
                address = IPAddress.Parse(host);
            IPEndPoint endpoint = new IPEndPoint(address, port);

            try
            {
                _listenSocket.Bind(endpoint);
                _listenSocket.Listen(backlog);

                for (int i = 0; i < register; ++i)
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                    RegisterAccept(args);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;
            try
            {
                if (!_listenSocket.AcceptAsync(args))
                    OnAcceptCompleted(null, args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Socket client_socket = args.AcceptSocket;
                callbackOnNewClient.Invoke(client_socket);
            }
            else
            {
                Console.WriteLine("Failed to accept client.");
            }

            RegisterAccept(args);
        }
    }
}
