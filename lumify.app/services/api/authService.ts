
import { saveFetch } from "@/services/utils/auth";
import { CONFIG } from "@/app/(app)/config/config";

const API_BASE = CONFIG.API.API_BASE;


export const AuthService = {


    async login(data: {
        identifier: string;
        password: string
    }) {

        const res = await saveFetch(`${API_BASE}/account/loginUser`, {
            method: "POST",
            body: JSON.stringify(data),
        });

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


    async logout() {
        const res = await saveFetch(`${API_BASE}/account/logoutUser`, {
            method: "POST",
        });

        if (!res.ok) {
            throw new Error("Logout failed on server");
        }

        return true;
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
