using System.Net.Sockets;

namespace ServerCore
{
    class SocketAsyncEventArgsPool
    {
        // SocketAsyncEventArgs Pool
        Stack<SocketAsyncEventArgs> _pool;

        /// <summary>
        /// capacity 크기의 Pool을 생성
        /// </summary>
        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// <summary>
        /// 설정한 SocketAsyncEventArgs을 Pool에 추가
        /// </summary>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("SocketAsyncEventArgs Pool 에 null을 추가"); }
            lock (_pool)
            {
                _pool.Push(item);
            }
        }

        /// <summary>
        /// Pool에서 SocketAsyncEventArgs을 꺼냄
        /// </summary>
        public SocketAsyncEventArgs Pop()
        {
            lock (_pool)
            {
                return _pool.Pop();
            }
        }
    }
}
