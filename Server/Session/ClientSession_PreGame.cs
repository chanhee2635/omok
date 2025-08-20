using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.DB;
using Server.Game;
using ServerCore;

namespace Server
{
    /// <summary>
    /// 하나의 session객체를 나타낸다.
    /// </summary>
    public partial class ClientSession : Session
    {
        public int PlayerDbId { get; set; }
        public PlayerInfo Info { get; set; }
        public string CurrentRoomId { get; set; }

        /// <summary>
        /// 구글 정보로 데이터를 생성 및 가져온다.
        /// </summary>
        /// <param name="loginPacket"></param>
        public async Task HandleLogin(C_Login loginPacket)
        {
            // 구글 idToken 인증
            string uid = await FirebaseGoogleAuthenticator.Instance.VerifyFirebaseIdTokenAsync(loginPacket.GoogleId);
            if (uid == null)
            {
                Console.WriteLine($"잘못된 토큰 정보로 로그인을 시도했습니다.");
                return;
            }

            using (AppDbContext db =  new AppDbContext())
            {
                AccountDb account = db.Accounts
                    .Include(a => a.Player)
                    .Where(a => a.GoogleId == uid)
                    .FirstOrDefault();

                // 계정이 없다면 생성
                if (account == null)
                {
                    account = new AccountDb() { GoogleId = uid };
                    db.Accounts.Add(account);  
                    db.SaveChanges();
                }

                PlayerDb playerDb = account.Player;

                // 플레이어 정보가 없다면 생성
                if (playerDb == null)
                {
                    playerDb = new PlayerDb()
                    {
                        Name = "Player_" + SessionId,
                        AccountDbId = account.AccountDbId,
                        Score = 1000,
                    };
                    db.Players.Add(playerDb);  
                    db.SaveChanges();
                }

                PlayerDbId = playerDb.PlayerDbId;

                // 패킷 전송
                S_Login resPacket = new S_Login();
                PlayerInfo playerInfo = new PlayerInfo()
                {
                    PlayerId = SessionId,
                    Name = playerDb.Name,
                    Score = playerDb.Score,
                    TotalRecord = new PlayRecord()
                    {
                        PlayCount = playerDb.TotalPlayCount,
                        WinCount = playerDb.TotalWinCount,
                        DrawCount = playerDb.TotalDrawCount,
                        LoseCount = playerDb.TotalLoseCount
                    }
                };
                Info = playerInfo;
                resPacket.Info = playerInfo;
                Send(resPacket);

            }

            // 랭킹 데이터 업데이트
            RankingManager.Instance.UpdateRanking(this);
        }
    }
}
