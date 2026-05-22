
import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { CONFIG } from "@/app/(app)/config/config";

const API_BASE = CONFIG.API.API_BASE;

export async function proxy(request: NextRequest) {
    const p = request.nextUrl.pathname;

    const isProtected =
        p.startsWith("/Dashboard") ||
        p.startsWith("/Notes")     ||
        p.startsWith("/Todos")     ||
        p.startsWith("/Events")    ||
        p.startsWith("/Profile");

    if (!isProtected) return NextResponse.next();

    const cookieHeader = request.headers.get("cookie") ?? "";

    const res = await fetch(`${API_BASE}/account/whoami`, {
        method: "GET",
        headers: { cookie: cookieHeader },
        cache: "no-store",
    });

    if (res.ok) return NextResponse.next();

    const loginUrl = new URL("/Auth", request.url);
    loginUrl.searchParams.set("callbackUrl", p);
    return NextResponse.redirect(loginUrl);
}

export const config = {
    matcher: [
        "/Dashboard/:path*",
        "/Notes/:path*",
        "/Todos/:path*",
        "/Events/:path*",
        "/Profile/:path*",
    ],
};
