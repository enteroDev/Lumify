export const CONFIG = {
    API: {
        API_BASE: "http://localhost:5331",
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