using Google.Protobuf.Protocol;
using Server;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class RedisManager
{
    private static RedisManager _instance = new RedisManager();

    public static RedisManager Instance { get { return _instance; } }

    private static ConnectionMultiplexer _redis;
    private static IDatabase _db;

    private const string RANKING_KEY = "omok:ranking";
    private const string WAITING_KEY = "omok:waiting";

    public void Init(string connectionString = "localhost:6379")
    {
        // Redis에 연결하고 데이터베이스를 가져온다.
        try
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to Connect Redis: " + ex.Message);
        }

        if (_db == null) return;

        // 데이터 초기화
        _db.KeyDelete(RANKING_KEY);
        _db.KeyDelete(WAITING_KEY);

        GetAllRankingData();
    }

    public async void GetAllRankingData()
    {
        try
        {
            // DB에서 모든 플레이어 로드
            List<PlayerInfo> allPlayers = DbManager.GetAllPlayers();

            // 플레이어 정보를 ITransaction를 사용하여 한번에 Redis에 추가
            ITransaction tran = _db.CreateTransaction();
            foreach (var player in allPlayers)
            {
                tran.SortedSetAddAsync(RANKING_KEY, player.Name, player.Score);
            }
            tran.Execute();
        }
        catch (Exception e)
        {
            Console.WriteLine($"DB -> Redis Score 가져오는 중 오류 발생: {e.Message}");
        }
    }

    public void Dispose()
    {
        if (_redis != null)
        {
            _redis.Dispose();
            Console.WriteLine("Redis 연결 해제");
        }
    }

    public async Task<string> GetString(string key)
    {
        return await _db.StringGetAsync(key);
    }

    public async Task<bool> SetString(string key, string value)
    {
        return await _db.StringSetAsync(key, value);
    }

    /// <summary>
    /// 대기열에 플레이어 추가
    /// </summary>
    public async Task EnqueuePlayer(int playerId)
    {
        await _db.SetAddAsync(WAITING_KEY, playerId);
    }

    /// <summary>
    /// 대기열에서 플레이어 제거
    /// </summary>
    public async Task RemovePlayer(int playerId)
    {
        await _db.SetRemoveAsync(WAITING_KEY, playerId, 0);
    }

    /// <summary>
    /// count 만큼 Redis List 에서 가져옴
    /// </summary>
    public async Task<List<int>> DequeuePlayers(int count)
    {
        List<int> players = new List<int>();
        for (int i = 0; i < count; i++)
        {
            RedisValue player = await _db.SetPopAsync(WAITING_KEY);
            if (player.HasValue)
                players.Add((int)player);
            else
                break;
        }
        return players;
    }

    /// <summary>
    /// 대기열의 플레이어 갯수를 가져온다.
    /// </summary>
    public async Task<long> GetQueueLength()
    {
        return await _db.SetLengthAsync(WAITING_KEY);
    }

    /// <summary>
    /// 랭킹목록에 플레이어 추가. 이름을 바꿨다면 변경된 이름을 제거하고 추가
    /// 비동기 작업을 Transaction으로 묶어 원자성 보장
    /// </summary>
    public async Task UpdateRanking(string name, int score, string beforeName)
    {
        if (_db == null) return;

        ITransaction tran = _db.CreateTransaction();

        if (beforeName != null)
             tran.SortedSetRemoveAsync(RANKING_KEY, beforeName);

        tran.SortedSetAddAsync(RANKING_KEY, name, score);

        tran.Execute();
    }

    /// <summary>
    /// count 만큼 랭킹 목록을 가져온다.
    /// </summary>
    public async Task<List<RankingData>> GetRankingsFromRedis(int count = 100)
    {
        if (_db == null) return new List<RankingData>();

        SortedSetEntry[] entries = 
            await _db.SortedSetRangeByRankWithScoresAsync(RANKING_KEY, 0, count - 1, Order.Descending);

        List<RankingData> rankings = new List<RankingData>();
        int rank = 1;
        foreach (var entry in entries)
        {
            rankings.Add(new RankingData
            {
                Name = entry.Element.ToString(),
                Score = (int)entry.Score,
                Rank = rank++
            });
        }
        return rankings;
    }

    /// <summary>
    /// 해당 플레이어의 랭킹을 가져온다.
    /// </summary>
    public async Task<RankingData> GetPlayerRankingFromRedis(ClientSession session)
    {
        if (_db == null) return null;

        // 플레이어의 점수 가져오기
        double? score = await _db.SortedSetScoreAsync(RANKING_KEY, session.Info.Name);
        if (!score.HasValue)
        {
            return null;
        }

        // 플레이어의 랭크 가져오기 (내림차순)
        long? rank = await _db.SortedSetRankAsync(RANKING_KEY, session.Info.Name, Order.Descending);

        if (rank.HasValue)
        {
            return new RankingData
            {
                Name = session.Info.Name,
                Score = (int)score.Value,
                Rank = (int)rank.Value + 1 
            };
        }
        return null;
    }

    public void Close()
    {
        _redis.Close();
    }
}
