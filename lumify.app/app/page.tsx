// app/page.tsx  (Server Component)
import { headers as nextHeaders } from "next/headers";
import { redirect } from "next/navigation";
import { CONFIG } from "@/app/config/config";

// Server Component -> interne API-Adresse verwenden.
const API_BASE = CONFIG.API.SERVER_API_BASE;

export default async function Home() {

  const h = await nextHeaders();
  const cookieHeader = h.get("cookie") ?? "";


  const res = await fetch(`${API_BASE}/account/whoami`, {
    method: "GET",
    headers: { cookie: cookieHeader },
    cache: "no-store",
  });



  if (res.ok) {
    redirect("/Dashboard");
  } else {
    redirect("/Auth");
  }
}