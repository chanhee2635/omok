using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class GameServer
    {
        // 패킷 처리하는 함수를 담당할 스레드
        Thread logicThread;
        // 실행 흐름을 동기화하기 위한 객체
        AutoResetEvent loopEvent;
        // 패킷을 저장할 큐
        Queue<Packet> packetQueue;
        object _lock;

        public GameServer()
        {
            _lock = new object();
            loopEvent = new AutoResetEvent(false);
            packetQueue = new Queue<Packet>();

            logicThread = new Thread(gameloop);
            logicThread.Start();
        }

        /// <summary>
        /// 큐에 패킷이 있다면 반복해서 처리한다.
        /// </summary>
        void gameloop()
        {
            while (true)
            {
                Packet packet = null;

                lock (_lock)  // Enqueue 와 Dequeue 로 동시에 접근하지 못하도록 lock
                {
                    if (packetQueue.Count > 0)
                    {
                        packet = packetQueue.Dequeue();
                    }
                }

                if (packet != null)
                {
                    ProcessReceive(packet);
                }

                if (packetQueue.Count <= 0)
                    loopEvent.WaitOne();
            }
        }

        /// <summary>
        /// 패킷에 저장된 ClientSession 에 패킷을 처리하도록 함
        /// </summary>
        /// <param name="packet"></param>
        void ProcessReceive(Packet packet)
        {
            packet.Session.ProcessClientOperation(packet);
        }

        /// <summary>
        /// 패킷을 큐에 추가하고 AutoResetEvent가 대기 상태라면 작업을 다시 시작하도록 한다.
        /// </summary>
        public void EnqueuePacket(Packet packet)
        {
            lock (_lock)
            {
                packetQueue.Enqueue(packet);
                loopEvent.Set();
            }
        }
    }
}
