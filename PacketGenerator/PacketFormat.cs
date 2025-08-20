using System;
using System.Collections.Generic;
using System.Text;

namespace PacketGenerator
{
    class PacketFormat
    {
        // {0} 패킷 등록
        public static string managerFormat =
@"using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance {{ get {{ return _instance; }} }}
	#endregion

	public void OnRecvPacket(Session session,  ArraySegment<byte> buffer)
	{{
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        switch ((MsgId)id)
        {{{0}
            default:
                Console.WriteLine($""Unknown MsgId: {{id}}"");
                break;
        }}
    }}
}}";

        // {0} MsgId
        // {1} 패킷 이름
        public static string managerRegisterFormat =
@"		
            case MsgId.{0}:
                {{
                    {1} packet = new {1}();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.{1}Handler(session, packet);
                }}
                break;";
    }
}
