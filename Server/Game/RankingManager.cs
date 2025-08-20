using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game
{
    public class RankingManager
    {

        private static readonly RankingManager _instance = new RankingManager();

        public static RankingManager Instance { get {  return _instance; } }

        public async void GetRankingData(ClientSession session)
        {
            try
            {
                var myRankingTask = RedisManager.Instance.GetPlayerRankingFromRedis(session);
                var rankingsTask = RedisManager.Instance.GetRankingsFromRedis();

                // 비동기 작업 병렬 실행으로 시간 단축
                await Task.WhenAll(myRankingTask, rankingsTask);

                S_GetRanking resPacket = new S_GetRanking();
                resPacket.MyRanking = myRankingTask.Result;
                resPacket.Rankings.AddRange(rankingsTask.Result);
                session.Send(resPacket);
            }
            catch (Exception e)
            {
                Console.WriteLine($"랭킹 조회 중 오류 발생: {e.Message}");
            }
        }

        public async void UpdateRanking(ClientSession session, string beforeName = null)
        {
            try
            {
                await RedisManager.Instance.UpdateRanking(session.Info.Name, session.Info.Score, beforeName);

                Console.WriteLine($"랭킹 업데이트 완료!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"랭킹 업데이트 중 오류 발생: {e.Message}");
            }
        }
    }
}
