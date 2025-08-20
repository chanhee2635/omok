using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server;
using Server.DB;
using Server.Game;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public static class DbManager
{
    /// <summary>
    /// 플레이어 점수를 DB에 업데이트한다.
    /// </summary>
    public static async void UpdatePlayerScore(int sessionId, int changeScore, bool winner)
    {
        ClientSession session = SessionManager.GetSession(sessionId);
        if (session == null) return;

        session.Info.Score += changeScore;
        session.Info.TotalRecord.PlayCount += 1;
        if (winner)
            session.Info.TotalRecord.WinCount += 1;
        else
            session.Info.TotalRecord.LoseCount += 1;
        session.Send(new S_ChangeInfo { Info = session.Info });

        PlayerDb playerDb = new PlayerDb()
        {
            PlayerDbId = session.PlayerDbId,
            Score = session.Info.Score,
            TotalPlayCount = session.Info.TotalRecord.PlayCount,
            TotalWinCount = session.Info.TotalRecord.WinCount,
            TotalDrawCount = session.Info.TotalRecord.DrawCount,
            TotalLoseCount = session.Info.TotalRecord.LoseCount
        };

        RankingManager.Instance.UpdateRanking(session);

        using (AppDbContext db = new AppDbContext())
        {
            db.Entry(playerDb).State = EntityState.Unchanged;
            db.Entry(playerDb).Property(nameof(PlayerDb.Score)).IsModified = true;
            db.Entry(playerDb).Property(nameof(PlayerDb.TotalPlayCount)).IsModified = true;
            db.Entry(playerDb).Property(nameof(PlayerDb.TotalWinCount)).IsModified = true;
            db.Entry(playerDb).Property(nameof(PlayerDb.TotalDrawCount)).IsModified = true;
            db.Entry(playerDb).Property(nameof(PlayerDb.TotalLoseCount)).IsModified = true;

            db.SaveChanges();
        }
    }

    /// <summary>
    /// 플레이어 이름을 DB에 업데이트한다.
    /// </summary>
    public static void UpdatePlayerName(ClientSession session, string name)
    {
        // 띄어쓰기 제거
        string changeName = name.Replace(" ", "");

        string beforeName = session.Info.Name;

        using (AppDbContext db = new AppDbContext())
        {
            // 다른 사람이 사용하고 있는지
             PlayerDb account = db.Players
                        .Where(a => a.Name == changeName)
                        .FirstOrDefault();

            S_ChangeName resPacket = new S_ChangeName();

            if (account == null)
            {
                session.Info.Name = changeName;

                RankingManager.Instance.UpdateRanking(session, beforeName);

                PlayerDb playerDb = new PlayerDb()
                {
                    PlayerDbId = session.PlayerDbId,
                    Name = changeName
                };

                db.Entry(playerDb).State = EntityState.Unchanged;
                db.Entry(playerDb).Property(nameof(PlayerDb.Name)).IsModified = true;
                db.SaveChanges();

                resPacket.Success = true;
                resPacket.Info = session.Info;
            }
            else
            {
                resPacket.Success = false;
                resPacket.Message = "이미 사용중인 이름입니다.";
            }

            session.Send(resPacket);
        }
    }

    /// <summary>
    /// 모든 플레이어 목록을 가져온다.
    /// </summary>
    public static List<PlayerInfo> GetAllPlayers()
    {
        List<PlayerInfo> players = new List<PlayerInfo>();
        using (AppDbContext db = new AppDbContext())
        {
            foreach (PlayerDb p in db.Players)
            {
                players.Add(new PlayerInfo
                {
                    PlayerId = p.PlayerDbId,
                    Name = p.Name,
                    Score = p.Score
                });
            }
        }
        return players;
    }

    /// <summary>
    /// PlayerDbId의 가장 큰 수를 가져온다.
    /// </summary>
    public static int GetSessionNumber()
    {
        using (AppDbContext db = new AppDbContext())
        {
            if (!db.Players.Any()) return 0;
            return db.Players.Max(p => p.PlayerDbId);
        }
    }
}
