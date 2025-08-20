using System.Net.Sockets;

namespace ServerCore
{
    internal class BufferManager
    {
        // 버퍼의 총 크기
        int _totalBytes;
        // SocketAsyncEventArgs에 _bufferSize만큼씩 분할하여 할당할 버퍼
        byte[] _buffer;
        // 반환된 버퍼의 Index Pool (SocketAsyncEventArgs를 반환하지 않는 이상 사용되지 않는다)
        Stack<int> _freeIndexPool;     
        // 현재 Index (버퍼를 분할하기 위해 사용가능한 Index)
        int _currentIndex;
        // SocketAsyncEventArgs에 할당할 크기
        int _bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            _totalBytes = totalBytes;
            _currentIndex = 0;
            _bufferSize = bufferSize;
            _freeIndexPool = new Stack<int>();
        }

        /// <summary>
        /// 총 크기만큼의 버퍼를 생성
        /// </summary>
        public void Init()
        {
            _buffer = new byte[_totalBytes];
        }

        /// <summary>
        /// SocketAsyncEventArgs에 버퍼를 분할하여 할당
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
            }
            else
            {
                if ((_totalBytes - _bufferSize) < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
            return true;
        }

        /// <summary>
        /// SocketAsyncEventArgs에 사용됐던 버퍼를 사용가능한 상태로 변경
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
