namespace Aevatar.Account;

public class AccountOptions
{
    public string ResetPasswordUrl { get; set; }
    public int RegisterCodeDuration { get; set; } = 10; // 10 minutes
    public int MailSendingInterval { get; set; } = 1; // 1 minutes
}