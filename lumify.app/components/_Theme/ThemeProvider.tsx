"use client";


// --------------- //
// --- Imports --- //
// --------------- //
import React, {
    createContext,
    useContext,
    useEffect,
    useMemo,
    useState,
} from "react";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
/**
 * The two available colour themes. The value is written to `<html data-theme="...">` and is exactly
 * the key `globals.css` switches the colour palette on.
 */
export type Theme = "chill" | "beach";

/** The theme API exposed via {@link useTheme}. */
export type ThemeContextValue = {
    /** The currently active theme. */
    theme: Theme;
    /** Toggles between the two themes. */
    toggle: () => void;
    /** Sets a specific theme. */
    setTheme: (theme: Theme) => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

const STORAGE_KEY = "lumify-theme";
const DEFAULT_THEME: Theme = "chill";



// ----------------- //
// --- Component --- //
// ----------------- //
/**
 * Provides the active colour theme. Restores the last choice from `localStorage` on mount, applies
 * the theme to `<html data-theme>` (re-colouring the whole document) and persists every change.
 * Wrap the app once; consumers call {@link useTheme}.
 * @param props Standard React children.
 */
export default function ThemeProvider({
    children,
    }: {
    children: React.ReactNode;
    }) {

    const [theme, setThemeState] = useState<Theme>(DEFAULT_THEME);


    // Restore the last chosen theme once on mount (client only).
    useEffect(() => {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored === "chill" || stored === "beach") {
            setThemeState(stored);
        }
    }, []);


    // Apply the theme to <html> (so the whole document re-colors) and remember it.
    useEffect(() => {
        document.documentElement.dataset.theme = theme;
        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch {
            /* storage may be unavailable (private mode) - theme still works for this session */
        }
    }, [theme]);


    const setTheme = (next: Theme) => setThemeState(next);
    const toggle = () => setThemeState((prev) => (prev === "chill" ? "beach" : "chill"));

    const value = useMemo(
        () => ({
            theme,
            toggle,
            setTheme,
        }),
        [theme]
    );

    return (
        <ThemeContext.Provider value={value}>
            {children}
        </ThemeContext.Provider>
    );
}



// ------------ //
// --- Hook --- //
// ------------ //
/**
 * Accesses the theme {@link ThemeContextValue} (`theme`, `toggle`, `setTheme`).
 * @returns The theme API.
 * @throws Error if used outside a {@link ThemeProvider}.
 */
export function useTheme() {
    const ctx = useContext(ThemeContext);
    if (!ctx) {
        throw new Error("useTheme must be used within ThemeProvider");
    }
    return ctx;
}
