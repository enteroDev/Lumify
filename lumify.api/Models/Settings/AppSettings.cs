namespace lumify.api.Models.Settings
{
    // Bound from the "App" section in appsettings. Used to build user-facing links (e.g. the
    // password-reset link that points at the frontend, not the API).
    public class AppSettings
    {
        // Base URL of the frontend, e.g. "http://localhost:5333" in dev or "https://lumify.at" in prod.
        public string FrontendBaseUrl { get; set; } = "";

        // Frontend route that handles the reset form and reads the token from the query string.
        public string PasswordResetPath { get; set; } = "/Auth/reset";
    }
}
