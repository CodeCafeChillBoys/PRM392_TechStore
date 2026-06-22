namespace TechStore.Service.IService
{
    public interface IFirebaseNotificationService
    {
        Task SendNotificationAsync(string fcmToken, string title, string body);
    }
}