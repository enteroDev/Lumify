
import { saveFetch } from "@/services/utils/auth";
import { CONFIG } from "@/app/config/config";

const API_BASE = CONFIG.API.API_BASE;


/**
 * Client for the account/authentication endpoints: login (incl. 2FA), logout, registration,
 * password reset, e-mail verification and TOTP management. Session cookies are handled by
 * {@link saveFetch}; methods throw an `Error` (sometimes carrying a machine-readable `code`)
 * on failure so the UI can react.
 */
export const AuthService = {


    // ---------------- //
    // --- REGISTER --- //
    // ---------------- //

    /**
     * Registers a new account. No automatic login — the e-mail must be verified first.
     * @param data Registration fields (name, e-mail, username, password, optional avatar).
     * @returns The server response (carries `verificationRequired`).
     */
    async register(data: {
        firstName: string;
        lastName: string;
        email: string;
        username: string;
        password: string;
        avatarBase64?: string | null;
    }) {
        const res = await saveFetch(`${API_BASE}/account/registerUser`, {
            method: "POST",
            body: JSON.stringify(data),
        });

        if (!res.ok) throw new Error("Registration failed");
        return await res.json();
    },

    /**
     * Confirms the account via the one-time token from the verification mail.
     * @param token The token from the verification link.
     * @returns `true` on success.
     */
    async verifyEmail(token: string) {
        const res = await saveFetch(`${API_BASE}/account/verifyEmail`, {
            method: "POST",
            body: JSON.stringify({ token }),
        }, { redirectOn401: false });

        if (!res.ok) {
            let msg = "Bestätigung fehlgeschlagen.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return true;
    },

    /**
     * Resends the verification mail (backend answers generically).
     * @param identifier Username or e-mail.
     * @returns `true` once the request is accepted.
     */
    async resendVerification(identifier: string) {
        const res = await saveFetch(`${API_BASE}/account/resendVerification`, {
            method: "POST",
            body: JSON.stringify({ identifier }),
        }, { redirectOn401: false });

        if (!res.ok) {
            let msg = "Senden fehlgeschlagen.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return true;
    },



    // ------------- //
    // --- LOGIN --- //
    // ------------- //

    /**
     * Logs in with username/e-mail and password.
     * @param data Credentials (`identifier` = username or e-mail, plus `password`).
     * @returns The server response; when 2FA is enabled it carries `mfaRequired` and an MFA token.
     * @throws Error with an optional `code` (e.g. `email_not_confirmed`) on failure.
     */
    async login(data: {
        identifier: string;
        password: string
    }) {

        const res = await saveFetch(`${API_BASE}/account/loginUser`, {
            method: "POST",
            body: JSON.stringify(data),
        }, { redirectOn401: false });

        if (!res.ok) {
            let msg = "Login failed";
            let code: string | undefined;

            try {
                const j = await res.json();
                msg = j?.message ?? j?.error ?? msg;
                code = j?.error;
            } catch {}

            // Surface a machine-readable code (e.g. "email_not_confirmed") so the UI can react.
            const err = new Error(msg) as Error & { code?: string };
            err.code = code;
            throw err;
        }

        return await res.json();
    },

    /**
     * Login step 2: exchanges the short-lived MFA token and 6-digit code for a session.
     * @param data The MFA token from step 1 and the authenticator `code`.
     * @returns The authenticated user payload.
     * @throws Error if the code or token is invalid/expired.
     */
    async verifyTotpLogin(data: { mfaToken: string; code: string }) {
        const res = await saveFetch(`${API_BASE}/account/verifyTotpLogin`, {
            method: "POST",
            body: JSON.stringify(data),
        }, { redirectOn401: false });

        if (!res.ok) {
            let msg = "Code konnte nicht bestätigt werden.";
            try {
                const j = await res.json();
                msg = j?.message ?? j?.error ?? msg;
            } catch {}
            throw new Error(msg);
        }

        return await res.json();
    },



    // -------------- //
    // --- LOGOUT --- //
    // -------------- //

    /**
     * Logs the user out on the server (clears the session cookies).
     * @returns `true` on success.
     */
    async logout() {
        const res = await saveFetch(`${API_BASE}/account/logoutUser`, {
            method: "POST",
        });

        if (!res.ok) {
            throw new Error("Logout failed on server");
        }

        return true;
    },



    // ---------------------- //
    // --- Password reset --- //
    // ---------------------- //    (no active session needed)

    /**
     * Password reset step 1: requests a reset link. The backend always answers generically
     * (no user enumeration).
     * @param identifier Username or e-mail.
     * @returns `true` once the request is accepted.
     */
    async requestPasswordReset(identifier: string) {
        const res = await saveFetch(`${API_BASE}/account/requestPasswordReset`, {
            method: "POST",
            body: JSON.stringify({ identifier }),
        }, { redirectOn401: false });

        if (!res.ok) {
            let msg = "Anfrage fehlgeschlagen.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return true;
    },

    /**
     * Password reset step 2: sets a new password using the one-time token from the e-mail link.
     * @param token The raw token from the reset link.
     * @param newPassword The new password.
     * @returns `true` on success.
     */
    async resetPassword(token: string, newPassword: string) {
        const res = await saveFetch(`${API_BASE}/account/resetPassword`, {
            method: "POST",
            body: JSON.stringify({ token, newPassword }),
        }, { redirectOn401: false });

        if (!res.ok) {
            let msg = "Zurücksetzen fehlgeschlagen.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return true;
    },



    // ------------------------------------- //
    // --- Two-factor (TOTP) management  --- //
    // ------------------------------------- //     (requires an active session)

    /**
     * Returns whether 2FA is currently enabled for the logged-in user.
     * @returns An object with the `enabled` flag.
     */
    async getMfaStatus(): Promise<{ enabled: boolean }> {
        const res = await saveFetch(`${API_BASE}/account/mfaStatus`, { method: "GET" });
        if (!res.ok) throw new Error("Failed to load 2FA status");
        return await res.json();
    },

    /**
     * Starts 2FA setup.
     * @returns The `secret` (for manual entry) and a `qrCodeDataUri` to scan.
     */
    async setupTotp(): Promise<{ secret: string; qrCodeDataUri: string }> {
        const res = await saveFetch(`${API_BASE}/account/setupTotp`, { method: "POST" });
        if (!res.ok) {
            let msg = "2FA-Einrichtung fehlgeschlagen.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },

    /**
     * Confirms 2FA setup with a valid code, activating two-factor authentication.
     * @param code The 6-digit authenticator code.
     * @returns An object with the resulting `enabled` flag.
     */
    async confirmTotp(code: string): Promise<{ enabled: boolean }> {
        const res = await saveFetch(`${API_BASE}/account/confirmTotp`, {
            method: "POST",
            body: JSON.stringify({ code }),
        });
        if (!res.ok) {
            let msg = "Code ungültig.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },

    /**
     * Disables 2FA (requires a valid current code).
     * @param code The 6-digit authenticator code.
     * @returns An object with the resulting `enabled` flag.
     */
    async disableTotp(code: string): Promise<{ enabled: boolean }> {
        const res = await saveFetch(`${API_BASE}/account/disableTotp`, {
            method: "POST",
            body: JSON.stringify({ code }),
        });
        if (!res.ok) {
            let msg = "Deaktivieren fehlgeschlagen.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },


};
