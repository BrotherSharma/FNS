using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FNS.Services
{
    public interface IEmailService
    {
        Task<bool> SendPaymentApprovalEmailAsync(string userEmail, string userName, string orderId);
        Task<bool> SendApprovalNotificationAsync(string userEmail, bool approved);

        Task<bool> SendRegistrationWelcomeEmailAsync(string userEmail, string userName, string loginUrl);
        Task<bool> SendPasswordResetEmailAsync(string userEmail, string resetUrl);
        Task<bool> SendReferralInviteEmailAsync(string friendEmail, string senderName, string signupUrl);
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

        public async Task<bool> SendRegistrationWelcomeEmailAsync(string userEmail, string userName, string loginUrl)
        {
            try
            {
                string smtpServer = _configuration["Email:SmtpServer"];
                int smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                string senderEmail = _configuration["Email:SenderEmail"];
                string senderPassword = _configuration["Email:SenderPassword"];

                if (string.IsNullOrWhiteSpace(senderEmail) ||
                    string.IsNullOrWhiteSpace(senderPassword) ||
                    senderPassword == "your_app_password_here")
                {
                    _logger.LogWarning("Registration welcome email skipped because Email:SenderEmail or Email:SenderPassword is not configured.");
                    return false;
                }

                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    MailMessage mailMessage = new MailMessage(senderEmail, userEmail)
                    {
                        Subject = "Welcome to Nitringo - Your Nutrition Tracker account is ready",
                        IsBodyHtml = true
                    };

                    mailMessage.Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ margin: 0; padding: 0; background-color: #f4f8f6; font-family: Arial, sans-serif; color: #263b4a; }}
                            .container {{ max-width: 620px; margin: 28px auto; background: #ffffff; border-radius: 14px; overflow: hidden; border: 1px solid #dfeae4; }}
                            .header {{ background: linear-gradient(135deg, #2e5942 0%, #3d7a58 100%); color: #ffffff; padding: 28px; text-align: center; }}
                            .header h1 {{ margin: 0; font-size: 26px; }}
                            .header p {{ margin: 8px 0 0; opacity: 0.92; }}
                            .content {{ padding: 28px; line-height: 1.6; }}
                            .panel {{ background: #f7fbf8; border-left: 4px solid #2e5942; padding: 16px; margin: 20px 0; border-radius: 8px; }}
                            .feature-list {{ padding-left: 18px; margin: 14px 0 20px; }}
                            .feature-list li {{ margin-bottom: 8px; }}
                            .btn {{ display: inline-block; background: #2e5942; color: #ffffff !important; padding: 12px 22px; text-decoration: none; border-radius: 8px; font-weight: bold; }}
                            .footer {{ color: #6c8291; font-size: 12px; padding: 0 28px 24px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Welcome to NutrInfo</h1>
                                <p>Your personal nutrition tracking companion</p>
                            </div>
                            <div class='content'>
                                <p>Hello {WebUtility.HtmlEncode(userName)},</p>
                                <p>Thanks for joining us. Your NutrInfo account has been created successfully, and we are glad to have you with us.</p>
                                <div class='panel'>
                                    NutrInfo helps you understand your daily nutrition habits, stay consistent with your goals, and make healthier choices with clear tracking.
                                </div>
                                <p>With NutrInfo, you can:</p>
                                <ul class='feature-list'>
                                    <li>Complete your personal health profile for better nutrition guidance.</li>
                                    <li>Track meals, calories, protein, carbs, fats, and hydration.</li>
                                    <li>Review your daily summary and progress toward nutrition goals.</li>
                                    <li>Maintain streaks, notes, checklists, mood tracking, and progress charts.</li>
                                    <li>Get suggestions that help you improve your daily routine.</li>
                                </ul>
                                <p>
                                    <a class='btn' href='{loginUrl}'>Open Nutrition Tracker</a>
                                </p>
                                <p>Start by completing your health information so NutrInfo can personalize your experience.</p>
                                <p>If you did not create this account, please ignore this email.</p>
                            </div>
                            <div class='footer'>
                                This email was sent automatically from NutrInfo. Please do not reply to this email.
                            </div>
                        </div>
                    </body>
                    </html>";

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Registration welcome email sent to {UserEmail}", userEmail);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending registration welcome email to {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string userEmail, string resetUrl)
        {
            try
            {
                string smtpServer = _configuration["Email:SmtpServer"];
                int smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                string senderEmail = _configuration["Email:SenderEmail"];
                string senderPassword = _configuration["Email:SenderPassword"];

                if (string.IsNullOrWhiteSpace(senderEmail) ||
                    string.IsNullOrWhiteSpace(senderPassword) ||
                    senderPassword == "your_app_password_here")
                {
                    _logger.LogWarning("Password reset email skipped because Email:SenderEmail or Email:SenderPassword is not configured.");
                    return false;
                }

                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    MailMessage mailMessage = new MailMessage(senderEmail, userEmail)
                    {
                        Subject = "Reset your NutrInfo password",
                        IsBodyHtml = true
                    };

                    mailMessage.Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ margin: 0; padding: 0; background-color: #f4f8f6; font-family: Arial, sans-serif; color: #263b4a; }}
                            .container {{ max-width: 620px; margin: 28px auto; background: #ffffff; border-radius: 14px; overflow: hidden; border: 1px solid #dfeae4; }}
                            .header {{ background: linear-gradient(135deg, #2e5942 0%, #3d7a58 100%); color: #ffffff; padding: 28px; text-align: center; }}
                            .header h1 {{ margin: 0; font-size: 24px; }}
                            .content {{ padding: 28px; line-height: 1.6; }}
                            .notice {{ background: #f7fbf8; border-left: 4px solid #2e5942; padding: 16px; margin: 20px 0; border-radius: 8px; }}
                            .btn {{ display: inline-block; background: #2e5942; color: #ffffff !important; padding: 12px 22px; text-decoration: none; border-radius: 8px; font-weight: bold; }}
                            .footer {{ color: #6c8291; font-size: 12px; padding: 0 28px 24px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1>Password Reset Request</h1>
                            </div>
                            <div class='content'>
                                <p>Hello,</p>
                                <p>We received a request to reset the password for your NutrInfo account.</p>
                                <div class='notice'>
                                    This reset link is valid for 30 minutes. For your security, it can be used only once.
                                </div>
                                <p>
                                    <a class='btn' href='{resetUrl}'>Reset Password</a>
                                </p>
                                <p>If you did not request this, you can safely ignore this email and your password will remain unchanged.</p>
                            </div>
                            <div class='footer'>
                                This email was sent automatically from NutrInfo. Please do not reply to this email.
                            </div>
                        </div>
                    </body>
                    </html>";

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Password reset email sent to {UserEmail}", userEmail);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {UserEmail}", userEmail);
                return false;
            }
        }
        public async Task<bool> SendReferralInviteEmailAsync(string friendEmail, string senderName, string signupUrl)
        {
            try
            {
                string smtpServer = _configuration["Email:SmtpServer"];
                int smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
                string senderEmail = _configuration["Email:SenderEmail"];
                string senderPassword = _configuration["Email:SenderPassword"];

                if (string.IsNullOrWhiteSpace(senderEmail) ||
                    string.IsNullOrWhiteSpace(senderPassword) ||
                    senderPassword == "your_app_password_here")
                {
                    _logger.LogWarning("Referral email skipped: Email credentials not configured.");
                    return false;
                }

                using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(senderEmail, senderPassword);

                    MailMessage mailMessage = new MailMessage(senderEmail, friendEmail)
                    {
                        Subject = $"{WebUtility.HtmlEncode(senderName)} invited you to join NutrInfo 🌿",
                        IsBodyHtml = true
                    };

                    mailMessage.Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ margin: 0; padding: 0; background-color: #f4f8f6; font-family: Arial, sans-serif; color: #263b4a; }}
                            .container {{ max-width: 620px; margin: 28px auto; background: #ffffff; border-radius: 14px; overflow: hidden; border: 1px solid #dfeae4; }}
                            .header {{ background: linear-gradient(135deg, #0f4c3a 0%, #1a7a5e 100%); color: #ffffff; padding: 36px 28px; text-align: center; }}
                            .header h1 {{ margin: 0 0 8px; font-size: 28px; letter-spacing: -0.5px; }}
                            .header p {{ margin: 0; opacity: 0.85; font-size: 15px; }}
                            .badge {{ display: inline-block; background: rgba(255,255,255,0.18); border: 1px solid rgba(255,255,255,0.35); color: #fff; font-size: 12px; font-weight: bold; padding: 5px 14px; border-radius: 20px; margin-bottom: 16px; letter-spacing: 0.06em; text-transform: uppercase; }}
                            .content {{ padding: 32px 28px; line-height: 1.7; }}
                            .panel {{ background: #eef8f3; border-left: 4px solid #0f4c3a; padding: 16px 20px; margin: 22px 0; border-radius: 8px; font-size: 14px; }}
                            .feature-row {{ display: flex; gap: 12px; margin: 18px 0; }}
                            .feature {{ flex: 1; background: #f7fbf9; border: 1px solid #dfeae4; border-radius: 10px; padding: 14px; text-align: center; font-size: 13px; }}
                            .feature strong {{ display: block; color: #0f4c3a; margin-bottom: 4px; font-size: 20px; }}
                            .cta-wrap {{ text-align: center; margin: 28px 0 16px; }}
                            .btn {{ display: inline-block; background: linear-gradient(135deg, #0f4c3a, #1a7a5e); color: #ffffff !important; padding: 14px 32px; text-decoration: none; border-radius: 10px; font-weight: bold; font-size: 15px; }}
                            .footer {{ color: #6c8291; font-size: 12px; padding: 0 28px 24px; text-align: center; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <div class='badge'>🌿 Personal Invitation</div>
                                <h1>You've Been Invited!</h1>
                                <p>{WebUtility.HtmlEncode(senderName)} wants you to join NutrInfo</p>
                            </div>
                            <div class='content'>
                                <p>Hi there,</p>
                                <p><strong>{WebUtility.HtmlEncode(senderName)}</strong> thinks you'd love <strong>NutrInfo</strong> — a smart nutrition tracker that helps you reach your health goals with ease.</p>
                                <div class='panel'>
                                    🎁 <strong>Special Referral Bonus:</strong> When you sign up using this invitation, both you and {WebUtility.HtmlEncode(senderName)} earn <strong>100 reward points</strong> to unlock premium features!
                                </div>
                                <p>Here's what NutrInfo helps you do:</p>
                                <ul style='padding-left:18px; margin:10px 0 20px;'>
                                    <li>Track daily meals, calories, macros &amp; hydration</li>
                                    <li>Get personalised workout videos by your goal</li>
                                    <li>Monitor mood, streaks &amp; daily checklists</li>
                                    <li>View progress charts and AI-powered suggestions</li>
                                </ul>
                                <div class='cta-wrap'>
                                    <a class='btn' href='{signupUrl}'>Join NutrInfo Free →</a>
                                </div>
                                <p style='text-align:center; font-size:13px; color:#6c8291;'>No credit card required. Get started in 60 seconds.</p>
                            </div>
                            <div class='footer'>
                                You received this email because {WebUtility.HtmlEncode(senderName)} sent you a personal invitation via NutrInfo.<br>
                                If you did not expect this, you can safely ignore it.
                            </div>
                        </div>
                    </body>
                    </html>";

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation("Referral invite sent to {FriendEmail} by {Sender}", friendEmail, senderName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending referral invite to {FriendEmail}", friendEmail);
                return false;
            }
        }
    }
}
