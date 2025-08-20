using System;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Game;
using ServerCore;

internal class PacketHandler
{
    public static async void C_LoginHandler(Session session, IMessage packet)
    {
        Console.WriteLine("C_Login");
        ClientSession clientSession = session as ClientSession;
        C_Login loginPacket = packet as C_Login;

        try
        {
            await clientSession.HandleLogin(loginPacket);
        }
        catch (Exception e)
        {
            Console.WriteLine("C_Login 패킷 처리 중 오류 발생:" + e.Message);
        }
    }

    public static void C_CreateRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        if (clientSession.CurrentRoomId != null)
            return;

        GameRoom room = GameRoomManager.CreateRoom();
        clientSession.CurrentRoomId = room.RoomId;

        S_CreateRoom resPacket = new S_CreateRoom();
        resPacket.RoomId = room.RoomId;
        clientSession.Send(resPacket);

        GameRoomManager.JoinRoom(room.RoomId, clientSession.SessionId, isPlayer: true);
    }

    public static void C_CancelCreateRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        S_CancelCreateRoom resPacket = new S_CancelCreateRoom();
        clientSession.Send(resPacket);

        GameRoomManager.PlayerLeftRoom(clientSession.SessionId, clientSession.CurrentRoomId);
    }

    public static void C_JoinRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_JoinRoom joinRoomPacket = packet as C_JoinRoom;

        if (clientSession.CurrentRoomId != null)
            return;

        bool success = GameRoomManager.JoinRoom(joinRoomPacket.RoomId, clientSession.SessionId, joinRoomPacket.IsPlayer);

        S_JoinRoom resPacket = new S_JoinRoom();
        resPacket.Success = success;
        if (success)
        {
            clientSession.CurrentRoomId = joinRoomPacket.RoomId;
            resPacket.RoomId = joinRoomPacket.RoomId;
            GameRoom room = GameRoomManager.GetRoom(joinRoomPacket.RoomId);
            if (room != null)
            {
                foreach (ClientSession player in room.Players.Values)
                {
                    resPacket.Players.Add(player.Info);
                }
                if (!joinRoomPacket.IsPlayer)
                {
                    resPacket.MovePoints.AddRange(room.Board.GetRecord());
                }
            }
        }
        else
        {
            resPacket.Message = "방을 찾을 수 없거나 입장할 수 없습니다.";
        }
        clientSession.Send(resPacket);
    }

    public static void C_PlayerEnterRoomHandler(Session session, IMessage packet)
    {
        C_PlayerEnterRoom playerEnterRoom = packet as C_PlayerEnterRoom;
        ClientSession clientSession = session as ClientSession;

        GameRoom room = GameRoomManager.GetRoom(playerEnterRoom.RoomId);
        room.SpectatorReadyGameHandler(clientSession);
    }

    public static async void C_QuickMatchHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        if (clientSession.CurrentRoomId != null)
            return;

        try
        {
            await MatchingManager.AddMatchQueue(clientSession);
        }
        catch (Exception e)
        {
            Console.WriteLine("C_Login 패킷 처리 중 오류 발생:" + e.Message);
        }
    }

    public static async void C_CancelQuickMatchHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        if (clientSession.CurrentRoomId != null)
            return;
        try 
        {
            await MatchingManager.RemoveMatchQueue(clientSession);
        }
        catch (Exception e)
        {
            Console.WriteLine("C_Login 패킷 처리 중 오류 발생:" + e.Message);
        }
    }

    public static void C_ReadyGameHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_ReadyGame readyGamePacket = packet as C_ReadyGame;

        GameRoom room = GameRoomManager.GetRoom(readyGamePacket.RoomId);
        room.ReadyGameHandler(clientSession);
    }

    public static void C_PlaceStoneHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_PlaceStone placeStonePacket = packet as C_PlaceStone;

        GameRoom room = GameRoomManager.GetRoom(placeStonePacket.RoomId);
        if (room == null || room.Players.Count < 2)
        {
            clientSession.Send(new S_Chat { ChatType = ChatType.Warning, Message = "게임룸을 찾을 수 없거나 게임이 시작되지 않았습니다." });
            return;
        }

        if (room.CurrentTurnPlayerId != clientSession.SessionId)
        {
            clientSession.Send(new S_Chat { ChatType = ChatType.Warning, Message = "당신의 턴이 아닙니다." });
            return;
        }

        room.PlaceStone(clientSession.SessionId, placeStonePacket.Col, placeStonePacket.Row);
    }

    public static void C_RestartGameHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        GameRoom room = GameRoomManager.GetRoom(clientSession.CurrentRoomId);

        if (room == null || room.Players.Count < 2)
        {
            clientSession.Send(new S_Chat { ChatType = ChatType.Warning, Message = "재대결을 할 수 없습니다." });
            return;
        }

        if (room.IsGameStarted)
        {
            clientSession.Send(new S_Chat { ChatType = ChatType.Warning, Message = "게임이 끝나지 않았습니다." });
            return;
        }

        room.HandleRestart(clientSession);
    }

    public static void C_LeaveRoomHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        GameRoomManager.PlayerLeftRoom(clientSession.SessionId, clientSession.CurrentRoomId);
        clientSession.CurrentRoomId = null;
    }
    
    public static void C_GetRankingHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        RankingManager.Instance.GetRankingData(clientSession);
    }

    public static void C_ChatHandler(Session session, IMessage packet)
    {
        C_Chat chatPacket = packet as C_Chat;
        ClientSession clientSession = session as ClientSession;

        GameRoom room = GameRoomManager.GetRoom(chatPacket.RoomId);
        if (room == null)
            return;

        room.HandleChat(clientSession, chatPacket.Message);
    }

    public static void C_ChangeNameHandler(Session session, IMessage packet)
    {
        C_ChangeName changeNamePacket = packet as C_ChangeName;
        ClientSession clientSession = session as ClientSession;

        DbManager.UpdatePlayerName(clientSession, changeNamePacket.Name);
    }

    public static void C_PongHandler(Session session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        clientSession.HandlePong();
    }
}
