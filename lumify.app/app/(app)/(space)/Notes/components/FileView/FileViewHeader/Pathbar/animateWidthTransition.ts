"use client";

import { useLayoutEffect, useRef, useState } from "react";

export function useAnimateWidthTransition(text: string) {
    const measureRef = useRef<HTMLSpanElement | null>(null);
    const [width, setWidth] = useState(0);

    useLayoutEffect(() => {
        const el = measureRef.current;
        if (!el) { return; }

        // set text (falls React noch nicht upgedatet hat)
        el.textContent = text;

        const read = () => {
            const next = Math.ceil(el.getBoundingClientRect().width);
            setWidth(next);
        };

        read();

        // Reagiert auf Font-Load / Zoom / etc.
        const ro = new ResizeObserver(() => read());
        ro.observe(el);

        return () => ro.disconnect();
    }, [text]);

    return { measureRef, width };
}