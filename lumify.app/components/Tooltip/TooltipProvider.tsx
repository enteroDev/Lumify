"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useCallback, useContext, useMemo, useState } from "react";
// Components
import Tooltip from "./Tooltip";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
type TooltipState = {
    text: string;
    x: number;
    y: number;
    isVisible: boolean;
};

/** Arguments for showing a tooltip at a viewport position. */
export type ShowTooltipOptions = {
    /** The tooltip text. */
    text: string;
    /** X position in pixels. */
    x: number;
    /** Y position in pixels. */
    y: number;
};

/** The tooltip API exposed via {@link useTooltip}. */
export type TooltipContextValue = {
    /** Shows the shared tooltip with the given text at the given position. */
    showTooltip: (options: ShowTooltipOptions) => void;
    /** Hides the shared tooltip. */
    hideTooltip: () => void;
};

type TooltipProviderProps = {
    children: React.ReactNode;
};

const TooltipContext = createContext<TooltipContextValue | null>(null);



// ---------------- //
// --- Provider --- //
// ---------------- //
/**
 * Provides a single shared `Tooltip` positioned anywhere in the viewport. Holds its text,
 * position and visibility, and exposes {@link TooltipContextValue} so any component can show/hide it
 * imperatively (e.g. on hover). Wrap the app once; consumers call {@link useTooltip}.
 * @param props Standard React children.
 */
export default function TooltipProvider({
    children 
}: TooltipProviderProps) {


    const [tooltip, setTooltip] = useState<TooltipState>({
        text: "",
        x: 0,
        y: 0,
        isVisible: false,
    });


    // ----------------- //
    // --- Callbacks --- //
    // ----------------- //
    const showTooltip = useCallback(({ text, x, y }: ShowTooltipOptions) => {
        setTooltip({
            text,
            x,
            y,
            isVisible: true,
        });
    }, []);

    const hideTooltip = useCallback(() => {
        setTooltip((prev) => ({
            ...prev,
            isVisible: false,
        }));
    }, []);



    // ------------- //
    // --- Memos --- //
    // ------------- //
    const value = useMemo(() => ({
        showTooltip,
        hideTooltip,
    }), [showTooltip, hideTooltip]);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <TooltipContext.Provider value={value}>
            {children}

            <Tooltip
                text={tooltip.text}
                x={tooltip.x}
                y={tooltip.y}
                isVisible={tooltip.isVisible}
            />
        </TooltipContext.Provider>
    );
}



// ------------ //
// --- Hook --- //
// ------------ //
/**
 * Accesses the tooltip {@link TooltipContextValue} (`showTooltip`, `hideTooltip`).
 * @returns The tooltip API.
 * @throws Error if used outside a {@link TooltipProvider}.
 */
export function useTooltip() {
    const context = useContext(TooltipContext);

    if (!context) {
        throw new Error("useTooltip must be used within TooltipProvider");
    }

    return context;
}