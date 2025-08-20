using ServerCore;

namespace ServerCore
{
    public class Packet
    {
        public Session Session { get; private set; }
        public ArraySegment<byte> PacketBuffer { get; private set; }

        public void SetPacket(Session session, ArraySegment<byte> packetBuffer)
        {
            Session = session;
            PacketBuffer = packetBuffer;
        }

        public static Packet Create()
        {
            return PacketPoolManager.Pop();
        }

        public static void Destroy(Packet packet)
        {
            PacketPoolManager.Push(packet);
        }
    }
}