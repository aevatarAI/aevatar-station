namespace Aevatar.GodGPT.Options;

public class AwsS3Option
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
    public string S3Key { get; set; } = "GodGPT";
    public string ServiceURL { get; set; }
    
    // Front end
    public string AccessKeyFrontEnd { get; set; }
    public string SecretKeyFrontEnd { get; set; }
    public string BucketNameFrontEnd { get; set; }
    public string S3KeyFrontEnd { get; set; }
}