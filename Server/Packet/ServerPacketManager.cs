using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	public void OnRecvPacket(Session session,  ArraySegment<byte> buffer)
	{
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        switch ((MsgId)id)
        {		
            case MsgId.CLogin:
                {
                    C_Login packet = new C_Login();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_LoginHandler(session, packet);
                }
                break;		
            case MsgId.CCreateRoom:
                {
                    C_CreateRoom packet = new C_CreateRoom();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_CreateRoomHandler(session, packet);
                }
                break;		
            case MsgId.CCancelCreateRoom:
                {
                    C_CancelCreateRoom packet = new C_CancelCreateRoom();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_CancelCreateRoomHandler(session, packet);
                }
                break;		
            case MsgId.CJoinRoom:
                {
                    C_JoinRoom packet = new C_JoinRoom();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_JoinRoomHandler(session, packet);
                }
                break;		
            case MsgId.CQuickMatch:
                {
                    C_QuickMatch packet = new C_QuickMatch();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_QuickMatchHandler(session, packet);
                }
                break;		
            case MsgId.CCancelQuickMatch:
                {
                    C_CancelQuickMatch packet = new C_CancelQuickMatch();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_CancelQuickMatchHandler(session, packet);
                }
                break;		
            case MsgId.CPlayerEnterRoom:
                {
                    C_PlayerEnterRoom packet = new C_PlayerEnterRoom();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_PlayerEnterRoomHandler(session, packet);
                }
                break;		
            case MsgId.CReadyGame:
                {
                    C_ReadyGame packet = new C_ReadyGame();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_ReadyGameHandler(session, packet);
                }
                break;		
            case MsgId.CPlaceStone:
                {
                    C_PlaceStone packet = new C_PlaceStone();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_PlaceStoneHandler(session, packet);
                }
                break;		
            case MsgId.CRestartGame:
                {
                    C_RestartGame packet = new C_RestartGame();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_RestartGameHandler(session, packet);
                }
                break;		
            case MsgId.CLeaveRoom:
                {
                    C_LeaveRoom packet = new C_LeaveRoom();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_LeaveRoomHandler(session, packet);
                }
                break;		
            case MsgId.CGetRanking:
                {
                    C_GetRanking packet = new C_GetRanking();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_GetRankingHandler(session, packet);
                }
                break;		
            case MsgId.CChat:
                {
                    C_Chat packet = new C_Chat();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_ChatHandler(session, packet);
                }
                break;		
            case MsgId.CChangeName:
                {
                    C_ChangeName packet = new C_ChangeName();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_ChangeNameHandler(session, packet);
                }
                break;		
            case MsgId.CPong:
                {
                    C_Pong packet = new C_Pong();
                    packet.MergeFrom(buffer.Array, buffer.Offset + count, buffer.Count - count);
                    PacketHandler.C_PongHandler(session, packet);
                }
                break;
            default:
                Console.WriteLine($"Unknown MsgId: {id}");
                break;
        }
    }
}