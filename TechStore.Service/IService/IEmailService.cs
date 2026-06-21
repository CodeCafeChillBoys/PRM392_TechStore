using System.Threading.Tasks;

namespace TechStore.Service.IService
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
