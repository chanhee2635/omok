using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VirusWarGameServer;

namespace Server.Game
{
    public class GameRoom
    {
        // 게임 입장 및 관전을 하기 위한 RoomId(4자리 숫자)
        public string RoomId { get; private set; }
        // 플레이어 목록 (2명 제한)
        public Dictionary<int, ClientSession> Players { get; private set; } = new Dictionary<int, ClientSession>();
        // 관전자 목록
        public Dictionary<int, ClientSession> Spectators { get; private set; } = new Dictionary<int, ClientSession>();

        // 오목판
        public OmokBoard Board { get; private set; } 

        // 게임 시작 여부
        public bool IsGameStarted { get; private set; } = false;

        // 현재 턴 플레이어
        public int CurrentTurnPlayerId { get; private set; }
        // 현재 턴 돌타입
        public StoneType CurrentPlayerTurnStone;

        private Random random = new Random();
        private DateTime _turnTime;
        private const int TURN_SEC = 30;

        // 재대결 요청 저장
        public Dictionary<ClientSession, bool> CheckRestart = new Dictionary<ClientSession, bool>();
        // 준비 요청 저장
        public Dictionary<ClientSession, bool> CheckReady = new Dictionary<ClientSession, bool>();

        public GameRoom(string roomId)
        {
            RoomId = roomId;
            Board = new OmokBoard(Define.BOARDSIZE);
        }

        /// <summary>
        /// 방에 플레이어 또는 관전자를 추가한다.
        /// </summary>
        public bool AddPlayer(int sessionId, bool isPlayer)
        {
            if (Players.ContainsKey(sessionId) || Spectators.ContainsKey(sessionId))
                return false;

            ClientSession session = SessionManager.GetSession(sessionId);
            if (session == null) return false;

            // 관전자
            if (!isPlayer)
            {
                session.CurrentRoomId = RoomId;
                Spectators.Add(sessionId, session);
                BroadcastChat(ChatType.Noti, $"{session.Info.Name}님이 입장하셨습니다.");

                S_PlayerEnterRoom enterRoomPacket = new S_PlayerEnterRoom();
                enterRoomPacket.IsPlayer = isPlayer;
                enterRoomPacket.RoomId = RoomId;
                enterRoomPacket.CurrentPlayerId = CurrentTurnPlayerId;
                enterRoomPacket.CurrentPlayerStone = CurrentPlayerTurnStone;
                foreach (ClientSession p in Players.Values){
                    enterRoomPacket.Players.Add(p.Info);
                }
                enterRoomPacket.MovePoints.AddRange(Board.GetRecord());
                session.Send(enterRoomPacket);
            }
            // 플레이어
            else
            {
                if (Players.Count >= 2) return false;

                session.CurrentRoomId = RoomId;
                Players.Add(sessionId, session);
                CheckReady.Add(session, false);
                CheckRestart.Add(session, false);
                if (Players.Count == 2 && !IsGameStarted)
                    ReadyGame();
            }

            return true;
        }

        /// <summary>
        /// 플레이어 또는 관전자가 나갔을 때 호출
        /// </summary>
        public void RemovePlayer(int sessionId)
        {
            ClientSession session = SessionManager.GetSession(sessionId);
            if (session == null) return;

            session.CurrentRoomId = null;
            
            // 플레이어
            if (Players.Remove(sessionId))
            {
                if (CheckRestart.TryGetValue(session, out _))
                    CheckRestart[session] = false;

                BroadcastChat(ChatType.Noti, $"{session.Info.Name}님이 방을 나갔습니다.");
                if (IsGameStarted && Players.Count < 2)
                {
                    int remainingPlayerId = Players.Keys.FirstOrDefault(); // 남은 플레이어 ID
                    EndGame(remainingPlayerId, sessionId); // 나간 사람이 패배
                }
            }
            // 관전자
            else if (Spectators.Remove(sessionId))
            {
                BroadcastChat(ChatType.Noti, $"{session.Info.Name}님이 방을 나갔습니다.");
            }
        }

        /// <summary>
        /// 두명의 플레이어가 매칭이 되었을 때 호출
        /// 선 플레이어를 정하고 클라이언트에 준비 요청
        /// </summary>
        public void ReadyGame()
        {
            // 게임 준비를 위해 필요한 것
            // 플레이어, 룸, 선플레이어 정보

            // 플레이어 수가 두명보다 작으면 안됨 이미 시작한 게임이면 X
            if (Players.Count < 2 || IsGameStarted) return;

            // 플레이어 정보를 가져옴
            List<ClientSession> players = Players.Values.ToList();

            Board.BoardReset();

            // 랜덤으로 선 플레이어 지정
            if (random.Next(2) == 0)
            {
                ClientSession temp = players[0];
                players[0] = players[1];
                players[1] = temp;
            }

            players[0].Info.Stone = StoneType.ColorBlack;
            players[1].Info.Stone = StoneType.ColorWhite;

            CurrentTurnPlayerId = players[0].SessionId;
            CurrentPlayerTurnStone = StoneType.ColorBlack;

            // 클라이언트에게 준비 요청
            S_ReadyGame readyGamePacket = new S_ReadyGame();
            readyGamePacket.RoomId = RoomId;
            readyGamePacket.StartPlayerId = CurrentTurnPlayerId;
            readyGamePacket.StartPlayerStone = CurrentPlayerTurnStone;
            readyGamePacket.Players.Add(players[0].Info);
            readyGamePacket.Players.Add(players[1].Info);
            readyGamePacket.IsPlayer = true;
            Broadcast(readyGamePacket);
        }

        public void ReadyGameHandler(ClientSession session)
        {
            CheckReady[session] = true;

            bool ready = true;
            foreach (bool chk in CheckReady.Values)
            {
                ready &= chk;
            }

            if (ready)
                StartGame();
        }

        public void SpectatorReadyGameHandler(ClientSession session) 
        {
            S_StartGame startGamePacket = new S_StartGame();
            startGamePacket.CurrentPlayerId = CurrentTurnPlayerId;
            startGamePacket.CurrentStone = CurrentPlayerTurnStone;
            startGamePacket.UTC = DateTime.UtcNow.Ticks;
            startGamePacket.Seconds = TURN_SEC - (int)(DateTime.UtcNow - _turnTime).TotalSeconds;
            session.Send(startGamePacket);
        }

        /// <summary>
        /// 게임을 시작
        /// </summary>
        public void StartGame()
        {
            if (Players.Count < 2 || IsGameStarted) return;

            IsGameStarted = true;

            S_StartGame packet = new S_StartGame();
            packet.CurrentPlayerId = CurrentTurnPlayerId;
            packet.CurrentStone = CurrentPlayerTurnStone;
            packet.UTC = DateTime.UtcNow.Ticks;
            packet.Seconds = TURN_SEC;
            Broadcast(packet);

            // 턴 타이머 시작
            StartTurnTimer(TURN_SEC);
        }

        // 비동기 작업을 취소하기 위한 객체
        private CancellationTokenSource _turnTimerCancel;

        /// <summary>
        /// 비동기적으로 delaySeconds 만큼 대기하고 시간이 지나면 턴을 넘긴다.
        /// </summary>
        private async void StartTurnTimer(int delaySeconds)
        {
            _turnTimerCancel?.Cancel();
            _turnTimerCancel?.Dispose();

            _turnTimerCancel = new CancellationTokenSource();
            CancellationToken token = _turnTimerCancel.Token;

            try
            {
                await Task.Delay(delaySeconds * 1000, token); // 비동기적으로 N초 대기
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (DateTime.UtcNow - _turnTime >= TimeSpan.FromSeconds(delaySeconds))
            {
                Console.WriteLine($"{CurrentTurnPlayerId} 플레이어의 착수 시간이 지났습니다.");
                AdvanceTurn();
            }
        }

        /// <summary>
        /// 턴 타이머 취소
        /// </summary>
        public void CancelTurnTimer()
        {
            _turnTimerCancel.Cancel();
        }

        /// <summary>
        /// 턴을 넘김
        /// </summary>
        private void AdvanceTurn()
        {
            CurrentTurnPlayerId = Players.Values.FirstOrDefault(p => p.SessionId != CurrentTurnPlayerId).SessionId;
            CurrentPlayerTurnStone = (CurrentPlayerTurnStone == StoneType.ColorBlack) ? StoneType.ColorWhite : StoneType.ColorBlack;

            _turnTime = DateTime.UtcNow;

            S_ChangeTurn changeTurnPacket = new S_ChangeTurn();
            changeTurnPacket.CurrentPlayerId = CurrentTurnPlayerId;
            changeTurnPacket.CurrentStone = CurrentPlayerTurnStone;
            changeTurnPacket.UTC = DateTime.UtcNow.Ticks;
            changeTurnPacket.Seconds = TURN_SEC;
            Broadcast(changeTurnPacket);

            StartTurnTimer(TURN_SEC); // 새로운 턴 시작
        }

        /// <summary>
        /// 플레이어가 돌을 착수했다면 검증 후 돌을 놓는다.
        /// </summary>
        public void PlaceStone(int sessionId, int c, int r)
        {
            ClientSession player = Players[sessionId];
            S_PlaceStone placeStonePacket = new S_PlaceStone();
            placeStonePacket.Success = false;

            if (!IsGameStarted)
            {
                SendChat(player, ChatType.Warning, "게임이 시작되지 않았습니다.");
                return;
            }

            if (CurrentTurnPlayerId != sessionId)
            {
                SendChat(player, ChatType.Warning, "당신의 턴이 아닙니다.");
                return;
            }
            
            // 만약 보드판을 벗어났다면
            if (c < 0 || c >= Board.BoardSize ||  r < 0 || r >= Board.BoardSize || Board.Board[c,r] != StoneType.ColorNone)
            {
                SendChat(player, ChatType.Warning, "잘못된 위치입니다.");
                return;
            }

            // 흑돌일 때 금수 위치에 착수
            if (player.Info.Stone == StoneType.ColorBlack)
            {
                foreach (ForbiddenInfo point in Board.GetForbiddenPoints())
                {
                    if (point.Row == r && point.Col == c)
                    {
                        SendChat(player, ChatType.Warning, "금수 위치입니다.");
                        return;
                    }
                }
            }

            Board.SetStone(c, r, player.Info.Stone);

            placeStonePacket.Success = true;
            placeStonePacket.Point = new MoveInfo{
                Col = c,
                Row = r,
                Stone = player.Info.Stone,
            };
            Broadcast(placeStonePacket);

            // 오목이 되었는지 확인
            if (Board.CheckWin(c, r, player.Info.Stone))
            {
                EndGame(sessionId, Players.Values.FirstOrDefault(p => p.SessionId != sessionId)?.SessionId ?? 0);
                return;
            }

            // 흑돌에 금수 위치를 보냄
            S_Forbidden forbiddenPacket = new S_Forbidden();
            forbiddenPacket.ForbiddenMovePoints.AddRange(Board.CheckForbiddenMove());
            Players.Values.FirstOrDefault(p => p.Info.Stone == StoneType.ColorBlack)?.Send(forbiddenPacket);

            AdvanceTurn();
        }

        /// <summary>
        /// 게임이 끝났다면 점수와 승패를 DB에 저장
        /// </summary>
        public void EndGame(int winnerId, int loserId)
        {
            if (!IsGameStarted) return;

            IsGameStarted = false;
            CancelTurnTimer();

            int winnerScoreChange = 50;
            int loserScoreChange = -40;

            // 점수 저장, 랭킹 등록
            DbManager.UpdatePlayerScore(winnerId, winnerScoreChange, true);
            DbManager.UpdatePlayerScore(loserId, loserScoreChange, false);

            S_GameOver gameOverPacket = new S_GameOver{
                RoomId = RoomId, 
                WinnerPlayerId = winnerId,
                LosePlayerId = loserId,
            };
            gameOverPacket.MovePoints.AddRange(Board.GetRecord());
            Broadcast(gameOverPacket);
        }

        void PlayerBroadcast(IMessage packet)
        {
            foreach (ClientSession player in Players.Values)
            {
                player.Send(packet);
            }
        }

        void Broadcast(IMessage packet)
        {
            PlayerBroadcast(packet);
            foreach (ClientSession spectator in Spectators.Values)
            {
                spectator.Send(packet);
            }
        }

        /// <summary>
        /// 입력한 채팅을 플레이어와 관전자들 에게 보냄
        /// </summary>
        public void HandleChat(ClientSession session, string message)
        {
            S_Chat chat = new S_Chat{
                ChatType = ChatType.Chat,
                PlayerName = session.Info.Name,
                Message = message
            };
            Broadcast(chat);
        }

        /// <summary>
        /// 재대결 요청 시 채팅으로 보여주고 플레이어 둘 다 재대결을 요청했다면 다시 시작
        /// </summary>
        public void HandleRestart(ClientSession clientSession)
        {
            // 이미 재대결 요청을 한 상태라면
            if (CheckRestart[clientSession]) return;

            // 플레이어들에게 재대결 요청 알림
            foreach (ClientSession player in Players.Values)
            {
                SendChat(player, ChatType.Noti, $"{clientSession.Info.Name}님이 재대결을 요청했습니다.");
            }

            CheckRestart[clientSession] = true;

            bool reStart = true;
            foreach (bool chk in CheckRestart.Values)
            {
                reStart &= chk;
            }

            if (reStart)
            {
                // 다시 시작할 땐 순서를 다시 정하고 패킷을 보내야 한다.
                ReadyGame();

                foreach (ClientSession player in Players.Values)
                { 
                    CheckRestart[player] = false;
                }
            }
        }

        public void BroadcastChat(ChatType chatType, string message)
        {
            S_Chat chat = new S_Chat{
                ChatType = chatType,
                Message = message
            };
            Broadcast(chat);
        }

        public void SendChat(ClientSession session, ChatType chatType, string message)
        {
            S_Chat chat = new S_Chat{
                ChatType = chatType,
                Message = message
            };
            session.Send(chat);
        }
    }
}
