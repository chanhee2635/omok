using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Game;
using ServerCore;
using VirusWarGameServer;

namespace Server
{
    /// <summary>
    /// 하나의 session객체를 나타낸다.
    /// </summary>
    public partial class ClientSession : Session
    {
        public int SessionId { get; set; }
        UserToken token;

        public ClientSession(UserToken token)
        {
            this.token = token;
            this.token.session = this;
            _pongTime = DateTime.UtcNow;
        }

        DateTime _pongTime;

        public void SendPing()
        {
            if (DateTime.UtcNow - _pongTime > TimeSpan.FromSeconds(15))
            {
                OnDisconnected();
                return;
            }

            Send(new S_Ping());
        }

        public async void HandlePong()
        {
            _pongTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 패킷을 바이트 배열로 변환
        /// [사이즈][패킷ID][본문]
        /// </summary>
        public void Send(IMessage packet)
        {
            string megName = packet.Descriptor.Name.Replace("_", string.Empty);  // SChat
            MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), megName);  // 지정한 Enum 타입의 값 중에 있는 개체로 변환

            ushort size = (ushort)packet.CalculateSize();  // 패킷 사이즈
            byte[] sendBuffer = new byte[size + 4];  // sendBuffer에 4 더해 만들기(size와 id를 보내기 위해)
            Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));  // size를 먼저 복사
            Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));  // id를 복사
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);  // 패킷을 바이트배열로 변환하여 복사

            token.Send(new ArraySegment<byte>(sendBuffer));
        }

        private Timer _pingTimer;
        /// <summary>
        /// S_Connected 패킷 송신 및 연결 확인
        /// </summary>
        public async void OnConnected()
        {
            S_Connected connectedPacket = new S_Connected();
            Send(connectedPacket);
        }

        /// <summary>
        /// 세션 연결 해제
        /// </summary>
        public async void OnDisconnected()
        {
            try
            {
                // 매칭 대기열 제거
                await RedisManager.Instance.RemovePlayer(SessionId);

                // 방에 있었다면 나가기
                if (CurrentRoomId != null)
                    GameRoomManager.PlayerLeftRoom(SessionId, CurrentRoomId);
            }
            catch (Exception e)
            {
                Console.WriteLine("ClientSession.OnDisconnected 오류 발생: " + e.Message);
            }

            SessionManager.Remove(this);
            Console.WriteLine($"[SessionId:{SessionId}] Disconnect");
        }

        /// <summary>
        /// 패킷을 처리할 게임서버 객체에 클라이언트 세션과 패킷을 담아 전달
        /// </summary>
        /// <param name="buffer"></param>
        public void OnRecv(ArraySegment<byte> buffer)
        {
            Packet packet = Packet.Create();
            packet.SetPacket(this, buffer);
            Program.gameServer.EnqueuePacket(packet);
        }

        public void OnSend(int numOfBytes)
        {
            Console.WriteLine("SessionId: {0}, 수신 Byte: {1}", SessionId, numOfBytes);
        }

        /// <summary>
        /// 게임서버 객체에서 패킷이 있다면 루프로 실행될 함수
        /// </summary>
        /// <param name="packet"></param>
        public void ProcessClientOperation(Packet packet)
        {
            PacketManager.Instance.OnRecvPacket(this, packet.PacketBuffer);
            Packet.Destroy(packet);
        }
    }
}
