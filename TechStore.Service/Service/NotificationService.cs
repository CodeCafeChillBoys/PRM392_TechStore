using System;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using TechStore.Service.IService;

namespace TechStore.Service.Service
{
    public class NotificationService : INotificationService
    {
        public NotificationService(IConfiguration configuration)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                var credentialPath = configuration["Firebase:CredentialsPath"] ?? "firebase-adminsdk.json";
                if (File.Exists(credentialPath))
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(credentialPath)
                    });
                    Console.WriteLine("Khởi tạo Firebase thành công!");
                }
                else
                {
                    Console.WriteLine($"[Firebase Error] Không tìm thấy file cấu hình tại đường dẫn tuyệt đối: {Path.GetFullPath(credentialPath)}");
                }
            }
        }

        public async Task SendNotificationAsync(string fcmToken, string title, string body)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                // Nếu chưa cấu hình credentials thì bỏ qua để tránh crash
                Console.WriteLine("Firebase App chưa được khởi tạo. Bỏ qua gửi thông báo.");
                return;
            }

            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                }
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine($"Gửi thông báo thành công: {response}");
            }
            catch (FirebaseMessagingException ex)
            {
                Console.WriteLine($"Lỗi gửi FCM: {ex.Message}");
            }
        }
    }
}
