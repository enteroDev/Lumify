// services/utils/auth.ts
// Small framework for the api-authentication in fetches

import { CONFIG } from "@/app/config/config";

const API_BASE = CONFIG.API.API_BASE;



export function getCsrfFromCookie(): string {
    if (typeof document === "undefined") return "";

    const cookieValue = document.cookie
        .split("; ")
        .find(c => c.startsWith("XSRF-TOKEN="))
        ?.split("=")[1] ?? "";

    return decodeURIComponent(cookieValue);
}



export type SaveFetchOptions = {
    // The login/auth endpoints legitimately answer 401 for wrong credentials.
    // In those cases the caller wants to handle the error (e.g. show a toast),
    // not trigger the global "session expired" redirect to /Auth.
    redirectOn401?: boolean;
};

export async function saveFetch(
    input: RequestInfo | URL,
    init: RequestInit = {},
    opts: SaveFetchOptions = {}
) {
    const { redirectOn401 = true } = opts;
    const method = (init.method ?? "GET").toUpperCase();

    const headers: Record<string, string> = {
        ...(init.headers as Record<string, string> | undefined),
    };

    if (init.body && !headers["Content-Type"]) {
        headers["Content-Type"] = "application/json";
    }

    const csrf = getCsrfFromCookie();
    if (["POST", "PUT", "PATCH", "DELETE"].includes(method) && csrf) {
        headers["X-CSRF-Token"] = csrf;
    }

    const res = await fetch(input, { ...init, headers, credentials: "include" });

    if (res.status === 401 && redirectOn401) {

        try {
        await fetch(`${API_BASE}/account/logoutUser`, {
            method: "POST",
            credentials: "include",
        });
        } catch {}
        if (typeof window !== "undefined") window.location.href = "/Auth";
    }

    return res;
}