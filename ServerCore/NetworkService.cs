using System.Net.Sockets;

namespace ServerCore
{
    public class NetworkService
    {
        // 연결된 클라이언트 수 (체크용)
        int _count;
        // 클라이언트 접속을 받아들이는 객체
        Listener _listener;
        // SocketAsyncEventArgs 송,수신 Pool
        SocketAsyncEventArgsPool _recvArgsPool;
        SocketAsyncEventArgsPool _sendArgsPool;
        // SocketAsyncEventArgs에 할당할 버퍼(공간)를 관리하는 Manager
        BufferManager _bufferManager;
        // 클라이언트 연결시 컨텐츠단에 세션을 생성하는 콜백메서드 (Program.CreateSession 호출)
        public Action<UserToken> sessionFactory;

        // 최대 동접 수 
        readonly int MaxConnections = 1000;
        // 송,수신에 사용될 버퍼 크기
        readonly int BufferSize = 2048;
        // 송,수신 = 2
        readonly int PreAllocCount = 2;

        public NetworkService()
        {
            _count = 0;
            sessionFactory = null;
        }

        /// <summary>
        /// BufferManager, SocketAsyncEventArgsPool 생성 및 할당
        /// SocketAsyncEventArgs의 버퍼를 분할해 할당해 놓는다.
        /// GC 부하 감소, 메모리 단편화 방지
        /// </summary>
        public void Init()
        {
            _bufferManager = new BufferManager(MaxConnections * BufferSize * PreAllocCount, BufferSize);
            _recvArgsPool = new SocketAsyncEventArgsPool(MaxConnections);
            _sendArgsPool = new SocketAsyncEventArgsPool(MaxConnections);
            _bufferManager.Init();

            SocketAsyncEventArgs arg;

            for (int i = 0; i < MaxConnections; i++)
            {
                UserToken token = new UserToken();

                // receive pool
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                    arg.UserToken = token;
                    _bufferManager.SetBuffer(arg);
                    _recvArgsPool.Push(arg);
                }
                // send pool
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
                    arg.UserToken = token;
                    _bufferManager.SetBuffer(arg);
                    _sendArgsPool.Push(arg);
                }
            }
        }

        /// <summary>
        /// Listener 생성 및 데이터 전달
        /// </summary>
        public void Listen(string host, int port, int backlog)
        {
            _listener = new Listener();
            _listener.callbackOnNewClient += OnNewClient;
            _listener.Init(host, port, backlog);
        }

        /// <summary>
        /// 송,수신 SocketAsyncEventArgs 생성 및 할당 후 데이터 수신 대기 시작
        /// ※ 클라이언트는 서버 하나와의 통신을 하기에 Pool이 필요하지 않음
        /// </summary>
        public void OnConnectCompleted(Socket socket, UserToken token)
        {
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
            recvArgs.UserToken = token;
            recvArgs.SetBuffer(new byte[BufferSize], 0, BufferSize);

            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
            sendArgs.UserToken = token;
            sendArgs.SetBuffer(new byte[BufferSize], 0, BufferSize);

            BeginReceive(socket, recvArgs, sendArgs);
        }

        /// <summary>
        /// 클라이언트 접속 작업 후 데이터 수신 함수 호출
        /// </summary>
		void OnNewClient(Socket clientSocket)
        {
            Interlocked.Increment(ref _count);

            // 스레드Id 와 소켓 메모리 주소(포인터), 연결 수 확인
            Console.WriteLine(string.Format("[{0}] 클라이언트 연결. handle {1},  count {2}",
                Thread.CurrentThread.ManagedThreadId, clientSocket.Handle,
                _count));

            // Pool에서 꺼내온다.
            SocketAsyncEventArgs recvArgs = _recvArgsPool.Pop();
            SocketAsyncEventArgs sendArgs = _sendArgsPool.Pop();

            // 데이터 수신 함수 호출
            BeginReceive(clientSocket, recvArgs, sendArgs);
        }

        /// <summary>
        /// UserToken 에 송,수신 SocketAsyncEventArgs 객체를 저장하고 세션 생성. 데이터 수신 대기 시작.
        /// </summary>
        void BeginReceive(Socket socket, SocketAsyncEventArgs recvArgs, SocketAsyncEventArgs sendArgs)
        {
            UserToken userToken = recvArgs.UserToken as UserToken;
            userToken.SetEventArgs(recvArgs, sendArgs);
            userToken.socket = socket;

            // 세션 생성 후 S_Connected 패킷 보냄
            sessionFactory.Invoke(userToken);

            // 데이터 수신 대기
            if (!socket.ReceiveAsync(recvArgs))
                ReceiveCompleted(null, recvArgs);
        }

        /// <summary>
        /// 데이터 수신 후 데이터 처리 함수 호출
        /// </summary>
        void ReceiveCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.LastOperation != SocketAsyncOperation.Receive)
                return;

            UserToken token = args.UserToken as UserToken;
            // 수신된 데이터 크기가 0 이하이고 성공이 아니라면 연결 끊기
            if (args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
            {
                Console.WriteLine(string.Format("receive error {0},  transferred {1}", args.SocketError, args.BytesTransferred));
                CloseClientsocket(token);
            }

            // 데이터 처리
            token.OnRecv(args.Buffer, args.Offset, args.BytesTransferred);

            // 데이터가 계속해서 즉시 수신되어 false 가 된다면 재귀호출 시 스택이 쌓여 오버플로우 발생 가능
            // 재귀 호출 없이 같은 함수 내에서 바로 처리되도록 함
            while (token.socket != null && token.socket.Connected && !token.socket.ReceiveAsync(args))
            {
                if (args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
                {
                    CloseClientsocket(token);
                    return;
                }

                token.OnRecv(args.Buffer, args.Offset, args.BytesTransferred);
            }
        }

        /// <summary>
        /// 데이터 송신 후 데이터 처리 함수 호출
        /// </summary>
        void SendCompleted(object sender, SocketAsyncEventArgs args)
        {
            UserToken token = args.UserToken as UserToken;
            token.ProcessSend(args);
        }

        /// <summary>
        /// 연결 해제 시 데이터 초기화 및 반환
        /// </summary>
        public void CloseClientsocket(UserToken token)
        {
            token.OnDisconnected();

            if (_recvArgsPool != null)
                _recvArgsPool.Push(token.recvArgs);

            if (_sendArgsPool != null)
                _sendArgsPool.Push(token.sendArgs);
        }
    }
}
