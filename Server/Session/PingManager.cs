using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Server
{
    public static class PingManager
    {
        private static Timer _pingTimer;

        public static void StartPing()
        {
            _pingTimer = new Timer(BroadcastPing, null, 5000, 5000);
        }

        private static void BroadcastPing(object obj)
        {
            foreach (ClientSession session in SessionManager.GetSessions())
            {
                session.SendPing();
            }
        }
    }
}
