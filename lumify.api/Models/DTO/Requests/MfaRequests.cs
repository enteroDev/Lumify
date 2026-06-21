namespace lumify.api.Models.DTO.Requests
{
    // Second login step: the short-lived MFA token from step 1 + the 6-digit code.
    public class VerifyTotpLoginRequest
    {
        public string MfaToken { get; set; } = "";
        public string Code { get; set; } = "";
    }

    // Used to confirm 2FA setup and to disable it again (both need a valid current code).
    public class TotpCodeRequest
    {
        public string Code { get; set; } = "";
    }
}
