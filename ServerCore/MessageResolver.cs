namespace ServerCore
{
    class Defines
    {
        public static readonly short HEADERSIZE = 2;
    }

    class MessageResolver
    {
        // 메서드를 매개변수로 받아 실행할 delegate
        public delegate void CompletedMessageCallback(ArraySegment<byte> buffer);
        // 데이터를 받아 처리할 버퍼
        ArraySegment<byte> _buffer;
        // 읽은 데이터 위치
        int _readPos;
        // 받은 데이터 위치
        int _writePos;

        public MessageResolver(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }
        public int DataSize { get { return _writePos - _readPos; } }
        public int FreeSize { get { return _buffer.Count - _writePos; } }

        /// <summary>
        /// 수신된 데이터를 파싱하여 컨텐츠단으로 넘긴다.
        /// </summary>
        public void OnRecv(byte[] buffer, int offset, int transffered, CompletedMessageCallback callback)
        {
            // 버퍼 공간을 확보한다.
            Clean();

            // 버퍼의 남은 공간이 처리할 데이터 크기보다 작다.
            if (transffered > FreeSize)
            {
                Console.WriteLine("OnReceive Error: MessageResolver 데이터가 처리되지 않고 있음.");
                return;
            }

            // 배열 복사
            Buffer.BlockCopy(buffer, offset, _buffer.Array, _writePos, transffered);
            _writePos += transffered;

            while (true)
            {
                // 헤더 파싱 가능 여부 확인
                if (DataSize < Defines.HEADERSIZE) break;

                // 헤더의 크기만큼의 데이터가 있는지 확인
                ushort dataSize = BitConverter.ToUInt16(_buffer.Array, _readPos);
                if (DataSize < dataSize) break;

                // 패킷 처리 가능한 배열을 컨텐츠단으로 전송
                byte[] clone = new byte[dataSize];
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, clone, 0, dataSize);
                callback(new ArraySegment<byte>(clone));

                // 처리한 패킷만큼 읽은 위치 이동
                _readPos += dataSize;
            }
        }

        void Clean()
        {
            int dataSize = DataSize;
            if (dataSize == 0)
                _readPos = _writePos = 0;
            else
            {
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize;
            }
        }

        public void Init()
        {
            _readPos = _writePos = 0;
        }
    }
}
