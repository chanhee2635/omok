using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Server.Data;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Server
{
    public class FirebaseGoogleAuthenticator
    {
        private static readonly FirebaseGoogleAuthenticator _instance = new FirebaseGoogleAuthenticator();

        public static FirebaseGoogleAuthenticator Instance { get {  return _instance; } }

        public void Initialize()
        {
            // 이미 초기화되었는지 확인 (다중 호출 방지)
            if (FirebaseApp.DefaultInstance != null)
            {
                Console.WriteLine("Firebase Admin SDK는 이미 초기화되었습니다.");
                return;
            }

            string serviceAccountPath = ConfigManager.Config.googlePath;

            if (!File.Exists(serviceAccountPath))
            {
                Console.Error.WriteLine($"오류: Firebase 서비스 계정 파일이 없습니다. 경로를 확인하세요: {serviceAccountPath}");
                throw new FileNotFoundException("Firebase 서비스 계정 파일을 찾을 수 없습니다.", serviceAccountPath);
            }

            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(serviceAccountPath),
                });

                Console.WriteLine("Firebase Admin SDK가 성공적으로 초기화되었습니다.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Firebase Admin SDK 초기화 중 오류 발생: {ex.Message}");
                throw; // 초기화 실패 시 애플리케이션이 시작되지 않도록 예외를 다시 던집니다.
            }
        }

        public async Task<string> VerifyFirebaseIdTokenAsync(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                Console.WriteLine("구글 Id 토큰이 null로 인증할 수 없습니다.");
                return null;
            }

            try
            {
                FirebaseToken token = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

                string uid = token.Uid;
                Console.WriteLine("토큰 검증 성공, 사용자 UID: " + uid);

                return uid;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}
