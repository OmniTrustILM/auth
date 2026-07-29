namespace Auth.Models.Config
{
    public class AuthOptions
    {
        public const string Section = "AuthOptions";

        public bool CreateUnknownUsers { get; set; } = false;

        public bool CreateUnknownRoles { get; set; } = false;

        public SyncPolicy SyncPolicy { get; set; } = SyncPolicy.CreateOnly;

        public static SyncPolicy GetSyncPolicy(string? syncPolicy)
        {
            if (!string.IsNullOrWhiteSpace(syncPolicy) && syncPolicy.ToLower().Equals("sync-data")) return SyncPolicy.SyncData;
            return SyncPolicy.CreateOnly;
        }
    }
}
