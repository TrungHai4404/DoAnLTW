using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var emailSettings = _config.GetSection("EmailSettings");

        string senderEmail = emailSettings["SenderEmail"];
        string senderPassword = emailSettings["SenderPassword"];
        string smtpServer = emailSettings["SmtpServer"];
        int smtpPort = int.TryParse(emailSettings["SmtpPort"], out int port) ? port : 587;

        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword) || string.IsNullOrEmpty(smtpServer))
        {
            throw new InvalidOperationException("Cấu hình email không hợp lệ. Vui lòng kiểm tra file cấu hình.");
        }

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress("Shop", senderEmail));
        mimeMessage.To.Add(new MailboxAddress("", email)); // Đặt tên trống nếu không có

        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("html") { Text = message };

        try
        {
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(senderEmail, senderPassword);
            await smtpClient.SendAsync(mimeMessage);
            await smtpClient.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi gửi email: {ex.Message}");
            throw;
        }
    }
}
