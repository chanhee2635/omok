using System.Net;

namespace ServerCore
{
    public interface Session
    {
        void OnConnected();
        void OnRecv(ArraySegment<byte> buffer);
        void OnSend(int numOfBytes);
        void OnDisconnected();
        void ProcessClientOperation(Packet packet);
    }
}
