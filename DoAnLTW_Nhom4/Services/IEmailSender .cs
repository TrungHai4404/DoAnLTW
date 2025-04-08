using System.Threading.Tasks;

namespace DoAnLTW_Nhom4.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }



}