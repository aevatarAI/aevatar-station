namespace Aevatar.Email;

public class AwsEmailOptions
{
    public string From { get; set; }
    public string FromName { get; set; }
    public string SmtpUsername { get; set; }
    public string SmtpPassword { get; set; }
    public string ConfigSet { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public string Image { get; set; }
}