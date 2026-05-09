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
type MusicContextValue = {
    isEnabled: boolean;
    toggle: () => void;
};

const MusicContext = createContext<MusicContextValue | null>(null);



// ----------------- //
// --- Component --- //
// ----------------- //
export default function MusicProvider({
    children,
    }: {
    children: React.ReactNode;
    }) {
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [isEnabled, setIsEnabled] = useState(false);

    // Create audio once
    useEffect(() => {
        const audio = new Audio("/sounds/lofi.mp3");
        audio.loop = true;
        audio.volume = 0.03;
        audioRef.current = audio;

        // Start ON
        audio.play().then(() => {
        setIsEnabled(true);
        }).catch(() => {
        // Autoplay blocked – stays off until user clicks
        setIsEnabled(false);
        });

        return () => {
        audio.pause();
        audio.src = "";
        audioRef.current = null;
        };
    }, []);

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

    return (
        <MusicContext.Provider value={value}>
        {children}
        </MusicContext.Provider>
    );
}



// ------------ //
// --- Hook --- //
// ------------ //
export function useMusic() {
    const ctx = useContext(MusicContext);
    if (!ctx) {
        throw new Error("useMusic must be used within MusicProvider");
    }
    return ctx;
}
