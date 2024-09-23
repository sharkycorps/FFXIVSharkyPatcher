namespace SharkyPatcher.Common
{
    public class DalamudVersionInfo
    {
        public string AssemblyVersion { get; set; }
        public string SupportedGameVer { get; set; }
        public string RuntimeVersion { get; set; }
        public bool RuntimeRequired { get; set; }
        public string Key { get; set; }
        public string DownloadUrl { get; set; }
        public string Hash { get; set;}
    }
}