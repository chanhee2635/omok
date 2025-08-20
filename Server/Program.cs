using System;
using Server.Data;
using Server;
using ServerCore;
using System.Threading;
using Server.Game;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;

namespace VirusWarGameServer
{
    class Program
	{
		public static GameServer gameServer = new GameServer();

        static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
            PacketPoolManager.Initialize(2000);
            RedisManager.Instance.Init();
			MatchingManager.StartMatchingProcess();
			SessionManager.InitSessionNumber();
			PingManager.StartPing();
			FirebaseGoogleAuthenticator.Instance.Initialize();

            //var summary = BenchmarkRunner.Run<BenchmarkTest>();
            //         Console.WriteLine("\n--- Benchmark Summary ---");
            //         Console.WriteLine(summary.Table.ToString());

            NetworkService service = new NetworkService();
			service.sessionFactory += CreateSession;
			service.Init();
			service.Listen("0.0.0.0", 7979, 100);

			while (true)
			{
				string input = Console.ReadLine();
				Thread.Sleep(1000);
			}

			Console.ReadKey();
		}

		/// <summary>
		/// 클라이언트가 접속 완료되었을 때 호출
		/// </summary>
		static void CreateSession(UserToken token)
		{
			ClientSession user = SessionManager.Generate(token);
			user.OnConnected();
		}
	}
}
