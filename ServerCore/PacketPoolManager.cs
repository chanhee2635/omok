namespace ServerCore
{
    public class PacketPoolManager
    {
        static int _count = 0;
        static object _lock = new object();
        static Stack<Packet> pool;

        /// <summary>
        /// capacity 만큼 패킷을 생성하여 풀에 저장
        /// </summary>
        /// <param name="capacity"></param>
        public static void Initialize(int capacity)
        {
            pool = new Stack<Packet>();

            for (int i = 0; i < capacity; i++)
            {
                pool.Push(new Packet());
            }
        }

        /// <summary>
        /// 풀에서 Packet 을 꺼냄
        /// </summary>
        /// <returns></returns>
        public static Packet Pop()
        {
            lock (_lock)
            {
                if (pool.Count <= 0)
                {
                    Console.WriteLine($"[{++_count}]PacketPool Push");
                    pool.Push(new Packet());
                }

                return pool.Pop();
            }
        }

        /// <summary>
        /// 패킷을 추가
        /// </summary>
        /// <param name="packet"></param>
        public static void Push(Packet packet)
        {
            lock (_lock)
            {
                pool.Push(packet);
            }
        }
    }
}
