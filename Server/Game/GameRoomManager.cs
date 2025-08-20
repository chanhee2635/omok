using Google.Protobuf.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
    public static class GameRoomManager
    {
        private static ConcurrentDictionary<string, GameRoom> _rooms = new ConcurrentDictionary<string, GameRoom>();
        private static Random _random = new Random();

        /// <summary>
        /// 배틀룸을 생성
        /// </summary>
        public static GameRoom CreateRoom()
        {
            string roomId;
            do
            {
                roomId = _random.Next(0, 10000).ToString("0000");
            } while (_rooms.ContainsKey(roomId));

            GameRoom room = new GameRoom(roomId);
            _rooms.TryAdd(roomId, room);
            return room;
        }

        /// <summary>
        /// 방 번호로 방을 찾는다.
        /// </summary>
        public static GameRoom GetRoom(string roomId)
        {
            if (roomId == null) return null;
            _rooms.TryGetValue(roomId, out GameRoom room);
            return room;
        }

        /// <summary>
        /// 방 번호로 방에 입장한다.
        /// </summary>
        public static bool JoinRoom(string roomId, int sessionId, bool isPlayer)
        {
            if (roomId == null) return false;
            GameRoom room = GetRoom(roomId);
            if (room == null) return false;

            // 관전자는 시작한 후에 들어올 수 있도록
            if (!room.IsGameStarted && !isPlayer) return false;

            if (room.AddPlayer(sessionId, isPlayer))
            {
                return true;
            }
            else
            {
                return false;
            }    
        }

        /// <summary>
        /// 플레이어가 방을 나가면 게임을 종료한다.
        /// </summary>
        public static bool PlayerLeftRoom(int sessionId, string roomId)
        {
            if (roomId == null) return false;

            if (_rooms.TryGetValue(roomId, out GameRoom room))
            {
                room.RemovePlayer(sessionId);

                if (room.Players.Count == 0 && room.Spectators.Count == 0)
                {
                    _rooms.TryRemove(roomId, out _);
                }
                else if (room.Players.Count == 1 && room.IsGameStarted)
                {
                    room.EndGame(room.Players.Keys.FirstOrDefault(), sessionId);
                }
                return true;
            }
            return false;
        }
    }
}
