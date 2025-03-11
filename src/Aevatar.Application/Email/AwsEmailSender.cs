using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Emailing;
using Volo.Abp.MultiTenancy;

namespace Aevatar.Email;

public class AwsEmailSender : EmailSenderBase
{
    private readonly AwsEmailOptions _awsEmailOptions;
    private readonly ILogger<AwsEmailSender> _logger;
    
    public AwsEmailSender(IOptions<AwsEmailOptions> awsEmailOptions,
        ILogger<AwsEmailSender> logger,
        ICurrentTenant currentTenant,
        IEmailSenderConfiguration configuration,
        IBackgroundJobManager backgroundJobManager) : base(currentTenant, configuration,
        backgroundJobManager)
    {
        _logger = logger;
        _awsEmailOptions = awsEmailOptions.Value;
    }

    public override async Task SendAsync(string to, string subject, string body, bool isBodyHtml = true,
        AdditionalEmailSendingArgs additionalEmailSendingArgs = null)
    {
        await SendAsync(_awsEmailOptions.From, to, subject, body, isBodyHtml);
    }

    public override async Task SendAsync(string from, string to, string subject, string body, bool isBodyHtml = true,
        AdditionalEmailSendingArgs additionalEmailSendingArgs = null)
    {
        var mail = new MailMessage();
        mail.IsBodyHtml = true;
        mail.From = new MailAddress(from, _awsEmailOptions.FromName);
        mail.To.Add(new MailAddress(to));
        mail.Subject = subject;
        mail.Body = body;
        await SendEmailAsync(mail);
    }

    protected override async Task SendEmailAsync(MailMessage mail)
    {
        using var client = new SmtpClient(_awsEmailOptions.Host, _awsEmailOptions.Port);
        client.Credentials =
            new NetworkCredential(_awsEmailOptions.SmtpUsername, _awsEmailOptions.SmtpPassword);
        client.EnableSsl = true;

        try
        {
            _logger.LogInformation("Attempting to send email to {To} via aws.", mail.To);
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send aws email failed, To: {To}", mail.To);
            throw;
        }
    }
}