namespace lumify.api.Models.Settings
{
    /// <summary>
    /// Options bound from the <c>App</c> configuration section. Used to build user-facing links
    /// (e.g. the password-reset link that points at the frontend, not the API).
    /// </summary>
    public class AppSettings
    {
        /// <summary>Base URL of the frontend (e.g. <c>http://localhost:5333</c> in dev).</summary>
        public string FrontendBaseUrl { get; set; } = "";

        /// <summary>Frontend route that handles the reset form and reads the token from the query string.</summary>
        public string PasswordResetPath { get; set; } = "/Auth/reset";
    }
}
