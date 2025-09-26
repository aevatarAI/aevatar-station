namespace Aevatar.Cli;

public static class AevatarCliConstants
{
    public const string PackageId = "Aevatar.Cli";
    public const string HttpClientName = "AevatarHttpClient";
    public const string GithubHttpClientName = "GithubHttpClient";

    public static class Paths
    {
        private static readonly string AbpRootPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aevatar");

        public static string Log => Path.Combine(AbpRootPath, "cli", "logs");

        public static string Memory =>
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
                "memory.bin");
    }

    public static class MemoryKeys
    {
        public const string LatestCliVersionCheckDate = "LatestCliVersionCheckDate";
    }
}