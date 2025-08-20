using System.Net.Sockets;

namespace ServerCore
{
    public class UserToken
    {
        // 연결된 클라이언트와 통신할 Socket
        public Socket socket { get; set; }
        // 비동기 송,수신 작업 객체
        public SocketAsyncEventArgs recvArgs { get; private set; }
        public SocketAsyncEventArgs sendArgs { get; private set; }

        // 바이트를 패킷 형식으로 해석해주는 해석기.
        MessageResolver _messageResolver;

        // session객체. 어플리케이션 딴에서 구현하여 사용.
        public Session session { get; set; }

        // 전송할 패킷을 보관해놓는 큐. 1-Send로 처리하기 위한 큐이다.
        Queue<ArraySegment<byte>> _sendingQueue;
        Queue<ArraySegment<byte>> _tempQueue;
        // sending_queue lock처리에 사용되는 객체.
        private object _lock;

        public UserToken()
        {
            _lock = new object();
            _messageResolver = new MessageResolver(65535);
            session = null;
            _sendingQueue = new Queue<ArraySegment<byte>>();
            _tempQueue = new Queue<ArraySegment<byte>>();
        }

        /// <summary>
        /// 송,수신 SocketAsyncEventArgs 객체 설정
        /// </summary>
        /// <param name="recvArgs"></param>
        /// <param name="sendArgs"></param>
        public void SetEventArgs(SocketAsyncEventArgs recvArgs, SocketAsyncEventArgs sendArgs)
        {
            this.recvArgs = recvArgs;
            this.sendArgs = sendArgs;
        }

        /// <summary>
        ///	이 매소드에서 직접 바이트 데이터를 해석해도 되지만 Message resolver클래스를 따로 둔 이유는
        ///	추후에 확장성을 고려하여 다른 resolver를 구현할 때 UserToken클래스의 코드 수정을 최소화 하기 위함이다.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="transfered"></param>
        public void OnRecv(byte[] buffer, int offset, int transfered)
        {
            _messageResolver.OnRecv(buffer, offset, transfered, OnRecv);
        }

        /// <summary>
        /// 패킷 처리가 가능한 배열을 앱 딴으로 넘긴다.
        /// </summary>
        /// <param name="buffer"></param>
        void OnRecv(ArraySegment<byte> buffer)
        {
            if (session != null)
            {
                session.OnRecv(buffer);
            }
        }

        /// <summary>
        /// 연결 해제 시 초기화
        /// </summary>
        public void OnDisconnected()
        {
            _sendingQueue.Clear();
            _tempQueue.Clear();
            _messageResolver.Init();

            if (session != null)
                session.OnDisconnected();

            try
            {
                socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception e) 
            {
                Console.WriteLine(e.Message);
            }
            socket.Close();
        }

        /// <summary>
        /// 패킷을 전송한다.
        /// 큐가 비어 있을 경우에는 큐에 추가한 뒤 바로 SendAsync매소드를 호출하고,
        /// 데이터가 들어있을 경우에는 새로 추가만 한다.
        /// 
        /// 큐잉된 패킷의 전송 시점 :
        ///		현재 진행중인 SendAsync가 완료되었을 때 큐를 검사하여 나머지 패킷을 전송한다.
        /// </summary>
        /// <param name="msg"></param>
        public void Send(ArraySegment<byte> packet)
        {
            lock (_lock)
            {
                // 큐에 데이터가 있다면 데이터를 전송 중으로 큐에 추가만 한다.
                if (_sendingQueue.Count > 0)
                {
                    _sendingQueue.Enqueue(packet);
                    return;
                }

                // 큐에 데이터가 없다면 추가 후 전송 작업 시작
                _sendingQueue.Enqueue(packet);
                StartSend();
            }
        }

        /// <summary>
        /// 비동기 전송을 시작한다.
        /// </summary>
        void StartSend()
        {
            if (!socket.Connected) return;

            lock (_lock)
            {
                int byteCount = 0;
                while (_sendingQueue.Count > 0)
                {
                    ArraySegment<byte> packet = _sendingQueue.Peek();

                    if (byteCount + packet.Count > sendArgs.Buffer.Length) break;

                    _sendingQueue.Dequeue();
                    _tempQueue.Enqueue(packet);
                    byteCount += packet.Count;
                }
                sendArgs.SetBuffer(sendArgs.Offset, byteCount);
                int copyIdx = 0;
                foreach (ArraySegment<byte> packet in _tempQueue)
                {
                    Buffer.BlockCopy(packet.Array, packet.Offset, sendArgs.Buffer, sendArgs.Offset + copyIdx, packet.Count);
                    copyIdx += packet.Count;
                }

                Console.WriteLine("패킷 갯수: {0}, 총 전송 Byte: {1}", _tempQueue.Count, byteCount);
                // 비동기 전송 시작.
                try
                {
                    if (!socket.SendAsync(sendArgs))
                        ProcessSend(sendArgs);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// 비동기 전송 완료시 호출되는 콜백 매소드.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessSend(SocketAsyncEventArgs args)
        {
            // 데이터 크기가 0보다 작거나 실패했다면 return
            if (args.BytesTransferred <= 0 || args.SocketError != SocketError.Success)
            {
                OnDisconnected();
                return;
            }

            Console.WriteLine("전송 완료 Byte: {0}", args.BytesTransferred);

            lock (_lock)
            {
                // 전송한 패킷를 제거한다.
                _tempQueue.Clear();

                // 아직 전송하지 않은 대기중인 패킷이 있다면 다시한번 전송을 요청한다.
                if (_sendingQueue.Count > 0)
                    StartSend();
            }
        }
    }
}
