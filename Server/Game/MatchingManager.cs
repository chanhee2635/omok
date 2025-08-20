using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Game
{
    public static class MatchingManager
    {
        private static HashSet<int> matchingPlayers = new HashSet<int>();

        /// <summary>
        /// 대기열에 플레이어를 등록한다.
        /// </summary>
        public static async Task AddMatchQueue(ClientSession session)
        {
            if (matchingPlayers.Add(session.SessionId))
            {
                await RedisManager.Instance.EnqueuePlayer(session.SessionId);
                S_QuickMatch packet = new S_QuickMatch();
                session.Send(packet);
            }
        }

        /// <summary>
        /// 대기열에서 플레이어를 제거한다.
        /// </summary>
        public static async Task RemoveMatchQueue(ClientSession session)
        {
            if (matchingPlayers.Remove(session.SessionId))
            {
                await RedisManager.Instance.RemovePlayer(session.SessionId);
                S_CancelQuickMatch cancelPacket = new S_CancelQuickMatch();
                session.Send(cancelPacket);
            }
        }

        /// <summary>
        /// 타이머를 사용하여 5초마다 플레이어 매칭을 한다.
        /// </summary>
        public static async void StartMatchingProcess()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(5000);
                    await TryMatchPlayers();
                }
                catch (Exception e)
                {
                    Console.WriteLine("매칭 프로세스 중 오류 발생: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Redis 매칭 큐에 두명 이상의 플레이어가 있다면 게임룸을 만들고 게임을 시작한다.
        /// </summary>
        private static async Task TryMatchPlayers()
        {
            long queueLength = await RedisManager.Instance.GetQueueLength();

            while (queueLength >= 2)
            {
                List<int> matchedPlayers = await RedisManager.Instance.DequeuePlayers(2);

                if (matchedPlayers.Count == 2)
                {
                    GameRoom room = GameRoomManager.CreateRoom();

                    matchingPlayers.Remove(matchedPlayers[0]);
                    matchingPlayers.Remove(matchedPlayers[1]);

                    GameRoomManager.JoinRoom(room.RoomId, matchedPlayers[0], true);
                    GameRoomManager.JoinRoom(room.RoomId, matchedPlayers[1], true);

                    queueLength = await RedisManager.Instance.GetQueueLength(); // 다시 길이 확인
                }
                else
                {
                    break;
                }
            }
        }
    }
}
