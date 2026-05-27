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

type ShowTooltipOptions = {
    text: string;
    x: number;
    y: number;
};

type TooltipContextValue = {
    showTooltip: (options: ShowTooltipOptions) => void;
    hideTooltip: () => void;
};

type TooltipProviderProps = {
    children: React.ReactNode;
};

const TooltipContext = createContext<TooltipContextValue | null>(null);



// ---------------- //
// --- Provider --- //
// ---------------- //
export default function TooltipProvider({ children }: TooltipProviderProps) {
    const [tooltip, setTooltip] = useState<TooltipState>({
        text: "",
        x: 0,
        y: 0,
        isVisible: false,
    });

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

    const value = useMemo(() => ({
        showTooltip,
        hideTooltip,
    }), [showTooltip, hideTooltip]);

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
export function useTooltip() {
    const context = useContext(TooltipContext);

    if (!context) {
        throw new Error("useTooltip must be used within TooltipProvider");
    }

    return context;
}