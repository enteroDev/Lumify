// services/utils/auth.ts
// Small framework for the api-authentication in fetches

import { CONFIG } from "@/app/config/config";

const API_BASE = CONFIG.API.API_BASE;



/**
 * Reads the CSRF token from the `XSRF-TOKEN` cookie (URL-decoded). Returns an empty string on the
 * server (no `document`) or when the cookie is absent.
 * @returns The CSRF token, or an empty string.
 */
export function getCsrfFromCookie(): string {
    if (typeof document === "undefined") return "";

    const cookieValue = document.cookie
        .split("; ")
        .find(c => c.startsWith("XSRF-TOKEN="))
        ?.split("=")[1] ?? "";

    return decodeURIComponent(cookieValue);
}



/** Options for {@link saveFetch}. */
export type SaveFetchOptions = {
    /**
     * Whether a 401 response triggers the global "session expired" logout + redirect to `/Auth`.
     * Defaults to `true`; set `false` on auth endpoints that legitimately answer 401 for wrong
     * credentials, so the caller can handle the error itself (e.g. show a toast).
     */
    redirectOn401?: boolean;
};

/**
 * Wrapper around `fetch` that adds the credentials and CSRF handling every API call needs: sends
 * cookies, defaults the JSON content type, attaches the `X-CSRF-Token` header on mutating requests,
 * and (unless disabled) logs out and redirects to `/Auth` on a 401.
 * @param input The request URL or `Request`.
 * @param init Standard `fetch` init options.
 * @param opts Extra behaviour, e.g. {@link SaveFetchOptions.redirectOn401}.
 * @returns The raw `Response` (callers check `res.ok` themselves).
 */
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