
import { saveFetch } from "@/services/utils/auth";
import { CONFIG } from "@/app/config/config";

const API_BASE = CONFIG.API.API_BASE;


export const AuthService = {

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

            try {
                const j = await res.json();
                msg = j?.message ?? j?.error ?? msg;
            } catch {}

            throw new Error(msg);
        }

        return await res.json();
    },


    // Login step 2: exchange the short-lived MFA token + 6-digit code for a session.
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


    // --- Password reset (no active session needed) --- //

    // Step 1: request a reset link. The backend always answers generically (no user enumeration).
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

    // Step 2: set a new password using the one-time token from the email link.
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


    async logout() {
        const res = await saveFetch(`${API_BASE}/account/logoutUser`, {
            method: "POST",
        });

        if (!res.ok) {
            throw new Error("Logout failed on server");
        }

        return true;
    },


    // --- Two-factor (TOTP) management (requires an active session) --- //

    async getMfaStatus(): Promise<{ enabled: boolean }> {
        const res = await saveFetch(`${API_BASE}/account/mfaStatus`, { method: "GET" });
        if (!res.ok) throw new Error("Failed to load 2FA status");
        return await res.json();
    },

    // Starts setup: returns the secret (manual entry) + a QR-code data URI to scan.
    async setupTotp(): Promise<{ secret: string; qrCodeDataUri: string }> {
        const res = await saveFetch(`${API_BASE}/account/setupTotp`, { method: "POST" });
        if (!res.ok) {
            let msg = "2FA-Einrichtung fehlgeschlagen.";
            try { const j = await res.json(); msg = j?.message ?? j?.error ?? msg; } catch {}
            throw new Error(msg);
        }
        return await res.json();
    },

    // Confirms setup with a valid code -> activates 2FA.
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

    // Disables 2FA (requires a valid current code).
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
    }
};
