namespace Aegis.SharedKernel.Configuration
{
    public sealed class AuthOptions
    {
        public List<DemoUserOptions> DemoUsers { get; set; } = [];
    }

    public sealed class DemoUserOptions
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string TenantId { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = [];
    }
}