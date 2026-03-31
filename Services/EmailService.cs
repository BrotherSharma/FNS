using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FNS.Services
{
    public interface IEmailService
    {
        Task<bool> SendPaymentApprovalEmailAsync(string userEmail, string userName, string orderId);
        Task<bool> SendApprovalNotificationAsync(string userEmail, bool approved);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPaymentApprovalEmailAsync(string userEmail, string userName, string orderId)
        {
            try
            {
                string smtpServer = _configuration["Email:SmtpServer"];
                int smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                string senderEmail = _configuration["Email:SenderEmail"];
                string senderPassword = _configuration["Email:SenderPassword"];
                string adminEmail = _configuration["Email:AdminEmail"];

                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    MailMessage mailMessage = new MailMessage(senderEmail, adminEmail)
                    {
                        Subject = "🔔 New Payment Request - Admin Approval Required",
                        IsBodyHtml = true
                    };

                    string approveLink = $"https://localhost:7079/Payment/ApprovePayment?orderId={orderId}&approve=true";
                    string rejectLink = $"https://localhost:7079/Payment/ApprovePayment?orderId={orderId}&approve=false";

                    mailMessage.Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
                            .container {{ max-width: 600px; margin: 20px auto; background-color: white; padding: 20px; border-radius: 8px; }}
                            .header {{ background-color: #667eea; color: white; padding: 15px; border-radius: 5px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .user-info {{ background-color: #f9f9f9; padding: 15px; border-left: 4px solid #667eea; margin: 15px 0; }}
                            .buttons {{ display: flex; gap: 10px; margin: 20px 0; }}
                            .btn {{ padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block; font-weight: bold; }}
                            .btn-approve {{ background-color: #28a745; color: white; }}
                            .btn-reject {{ background-color: #dc3545; color: white; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>New Payment Request</h2>
                            </div>
                            <div class='content'>
                                <p>A new user has completed payment and is waiting for your approval.</p>
                                
                                <div class='user-info'>
                                    <strong>User Details:</strong><br/>
                                    Name: {userName}<br/>
                                    Email: {userEmail}<br/>
                                    Order ID: {orderId}<br/>
                                    Amount: ₹1 INR<br/>
                                    Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                                </div>

                                <p><strong>Please review and take action:</strong></p>
                                
                                <div class='buttons'>
                                    <a href='{approveLink}' class='btn btn-approve'>✓ Approve Access</a>
                                    <a href='{rejectLink}' class='btn btn-reject'>✗ Reject Request</a>
                                </div>

                                <hr/>
                                <p style='color: #666; font-size: 12px;'>
                                    This is an automated message from ReserveEase. Please do not reply to this email.
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Approval email sent to admin for user: {userEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending approval email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendApprovalNotificationAsync(string userEmail, bool approved)
        {
            try
            {
                string smtpServer = _configuration["Email:SmtpServer"];
                int smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                string senderEmail = _configuration["Email:SenderEmail"];
                string senderPassword = _configuration["Email:SenderPassword"];

                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    MailMessage mailMessage = new MailMessage(senderEmail, userEmail)
                    {
                        Subject = approved ? "✓ Your Payment Approved!" : "✗ Your Payment Request Rejected",
                        IsBodyHtml = true
                    };

                    string message = approved
                        ? "Your payment has been approved! You can now access the ReserveEase application."
                        : "Unfortunately, your payment request has been rejected. Please contact support for more information.";

                    string statusColor = approved ? "#28a745" : "#dc3545";
                    string statusIcon = approved ? "✓" : "✗";

                    mailMessage.Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
                            .container {{ max-width: 600px; margin: 20px auto; background-color: white; padding: 20px; border-radius: 8px; }}
                            .header {{ background-color: {statusColor}; color: white; padding: 15px; border-radius: 5px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .message {{ background-color: {statusColor}22; padding: 15px; border-left: 4px solid {statusColor}; margin: 15px 0; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>{statusIcon} Payment Status Update</h2>
                            </div>
                            <div class='content'>
                                <div class='message'>
                                    <p>{message}</p>
                                </div>
                                
                                {(approved ? "<p><a href='https://localhost:7079/Home/Index' style='display: inline-block; background-color: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold;'>Access Application</a></p>" : "")}
                                
                                <hr/>
                                <p style='color: #666; font-size: 12px;'>
                                    This is an automated message from ReserveEase. Please do not reply to this email.
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Status notification sent to user: {userEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending notification email: {ex.Message}");
                return false;
            }
        }
    }
}
