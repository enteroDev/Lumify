namespace lumify.api.Models.DTO.Requests
{
    /// <summary>
    /// Request body for the second login step with 2FA
    /// (see <see cref="Controllers.AccountController.VerifyTotpLogin"/>).
    /// </summary>
    public class VerifyTotpLoginRequest
    {
        /// <summary>The short-lived MFA challenge token from login step 1.</summary>
        public string MfaToken { get; set; } = "";
        /// <summary>The 6-digit code from the authenticator app.</summary>
        public string Code { get; set; } = "";
    }

    /// <summary>
    /// Request body carrying a single TOTP code, used to confirm 2FA setup and to disable it again
    /// (see <see cref="Controllers.AccountController.ConfirmTotp"/> /
    /// <see cref="Controllers.AccountController.DisableTotp"/>).
    /// </summary>
    public class TotpCodeRequest
    {
        /// <summary>The 6-digit code from the authenticator app.</summary>
        public string Code { get; set; } = "";
    }
}
