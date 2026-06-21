namespace TechStore.Service.IService
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string fcmToken, string title, string body);
    }
}