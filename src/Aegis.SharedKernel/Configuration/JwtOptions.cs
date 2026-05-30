namespace Aegis.SharedKernel.Configuration
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = "Aegis";

        public string Audience { get; set; } = "Aegis.Client";

        public string Secret { get; set; } = string.Empty;

        public int AccessTokenMinutes { get; set; } = 60;

        public int RefreshTokenDays { get; set; } = 7;

        public int MaxActiveRefreshSessionsPerUser { get; set; } = 5;

        public int RefreshSessionRetentionDays { get; set; } = 30;
    }
}
