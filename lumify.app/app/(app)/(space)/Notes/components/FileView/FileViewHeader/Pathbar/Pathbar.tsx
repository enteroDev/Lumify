"use client";


// --------------- //
// --- Imports --- //
// --------------- //
import { useEffect, useMemo, useState } from "react";
import styles from "./Pathbar.module.css";
import ArrowBack from "../../../../../../../src/svg/arrow_left.svg";
import { useAnimateWidthTransition } from "./animateWidthTransition";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
    backArrow: styles["back-arrow"],
    breadcrumb: styles["breadcrumb"],
    breadcrumbVisible: styles["breadcrumbVisible"],
    measure: styles["measure"],
} as const;

export type PathbarProps = {
    path: string;
    onBack?: () => void;
    canGoBack?: boolean;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Pathbar({ path, onBack, canGoBack }: PathbarProps) {

    const text = useMemo(() => path, [path]);
    const { measureRef, width: textWidth } = useAnimateWidthTransition(text);
    const [pillWidth, setPillWidth] = useState<number | null>(null);     // Optimize container width
    const disabled = canGoBack === false || !onBack;

    // Style-Set
    const FIXED = 15 + 15 /* padding left/right */ + 10 /* gap */ + 31 /* icon box width */ + 2 /* little fudge */;


    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        const next = FIXED + textWidth;
        requestAnimationFrame(() => setPillWidth(next));    // rAF sorgt dafür, dass Transition zuverlässig triggert
    });


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const containerWidth = pillWidth != null ? { width: pillWidth }: undefined;


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container} style={containerWidth}>

            {/* Hidden measuring element - must share same font styles as breadcrumb */}
            <span ref={measureRef} className={c.measure}>
                {text}
            </span>

            {/* BackArrow */}
            <div className={c.backArrow} onClick={disabled ? undefined : onBack} aria-disabled={disabled} tabIndex={disabled ? -1 : 0}>
                <ArrowBack />
            </div>

            {/* Breadcrumb */}
            <div className={`${c.breadcrumb} ${pillWidth ? c.breadcrumbVisible : ""}`}>
                {text}
            </div>
        </div>
    );
}