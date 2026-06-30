"use client";


// --------------- //
// --- Imports --- //
// --------------- //
import React, {
    createContext,
    useContext,
    useEffect,
    useMemo,
    useRef,
    useState,
} from "react";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
/** The music API exposed via {@link useMusic}. */
export type MusicContextValue = {
    /** Whether the background music is currently playing. */
    isEnabled: boolean;
    /** Toggles the background music on/off. */
    toggle: () => void;
};

const MusicContext = createContext<MusicContextValue | null>(null);



// ----------------- //
// --- Component --- //
// ----------------- //
/**
 * Provides app-wide ambient lo-fi background music. Creates one looping, low-volume audio element
 * and exposes play/pause via {@link MusicContextValue}. Wrap the app once; consumers call
 * {@link useMusic}.
 * @param props Standard React children.
 */
export default function MusicProvider({
    children,
    }: {
    children: React.ReactNode;
    }) {


    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [isEnabled, setIsEnabled] = useState(false);


    // --------------- //
    // --- Effects --- //
    // --------------- //

    // Create audio once
    useEffect(() => {
        const audio = new Audio("/sounds/lofi.mp3");
        audio.loop = true;
        audio.volume = 0.06;

        audioRef.current = audio;

        return () => {
            audio.pause();
            audio.src = "";
            audioRef.current = null;
        };
    }, []);


    // --------------- //
    // --- Handler --- //
    // --------------- //
    const toggle = () => {
        const audio = audioRef.current;
        if (!audio) return;

        if (audio.paused) {
        audio.play().then(() => {
            setIsEnabled(true);
        });
        } else {
        audio.pause();
        setIsEnabled(false);
        }
    };


    const value = useMemo(
        () => ({
        isEnabled,
        toggle,
        }),
        [isEnabled]
    );


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <MusicContext.Provider value={value}>
        {children}
        </MusicContext.Provider>
    );
}



// ------------ //
// --- Hook --- //
// ------------ //
/**
 * Accesses the music {@link MusicContextValue} (`isEnabled`, `toggle`).
 * @returns The music API.
 * @throws Error if used outside a {@link MusicProvider}.
 */
export function useMusic() {
    const ctx = useContext(MusicContext);
    if (!ctx) {
        throw new Error("useMusic must be used within MusicProvider");
    }
    return ctx;
}
