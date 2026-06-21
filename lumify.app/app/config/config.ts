// API-Adressen env-gesteuert (fuer Deployment), mit Dev-Fallback.
// NEXT_PUBLIC_API_BASE wird beim Build ins Browser-Bundle eingebacken.
// API_BASE_INTERNAL wird zur Laufzeit serverseitig gelesen (SSR / Middleware).
const BROWSER_API_BASE = process.env.NEXT_PUBLIC_API_BASE ?? "http://localhost:5331";
const SERVER_API_BASE = process.env.API_BASE_INTERNAL ?? BROWSER_API_BASE;

export const CONFIG = {
    API: {
        API_BASE: BROWSER_API_BASE,
        SERVER_API_BASE: SERVER_API_BASE,
    },

    ASSETS: {
        AVATAR_BASE_URL: "/Data/avatars",
        DEFAULT_AVATAR_URL: "/Data/avatars/default_avatar.png",
    },

    NOTE_MODAL: {
        TEXTAREA_MAX_HEIGHT: 230,
        TEXTAREA_MIN_HEIGHT: 100,
        BLOCK_OPEN_DURATION: 380,
        BLOCK_CLOSE_DURATION: 320,
    },

    FILE_BROWSER: {
        COLLAPSE_OPEN_DURATION: 380,
        COLLAPSE_CLOSE_DURATION: 320,
    },
} as const;