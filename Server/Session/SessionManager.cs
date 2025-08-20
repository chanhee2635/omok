using ServerCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server
{
    public static class SessionManager
    {
        static int _sessionId = 0;
        private static ConcurrentDictionary<int, ClientSession> _sessions = new ConcurrentDictionary<int, ClientSession>();

        /// <summary>
        /// sessionId 를 가장 큰 PlayerDbId 로 설정
        /// </summary>
        public static void InitSessionNumber()
        {
            _sessionId = DbManager.GetSessionNumber();
        }

        /// <summary>
        /// UserToken 으로 ClientSession 을 생성
        /// </summary>
        public static ClientSession Generate(UserToken token)
        {
            int sessionId = ++_sessionId;
            ClientSession session = new ClientSession(token);
            session.SessionId = sessionId;
            _sessions.TryAdd(sessionId, session);
            return session;
        }

        /// <summary>
        /// sessionId 로 ClientSession 을 가져온다.
        /// </summary>
        public static ClientSession GetSession(int sessionId)
        {
            _sessions.TryGetValue(sessionId, out ClientSession session);
            return session;
        }

        /// <summary>
        /// 연결된 모든 ClientSession 을 가져온다.
        /// </summary>
        /// <returns></returns>
        public static List<ClientSession> GetSessions()
        {
            return _sessions.Values.ToList();
        }

        /// <summary>
        /// ClientSession 을 제거
        /// </summary>
        public static void Remove(ClientSession session)
        {
            _sessions.TryRemove(session.SessionId, out _);
        }
    }
}
