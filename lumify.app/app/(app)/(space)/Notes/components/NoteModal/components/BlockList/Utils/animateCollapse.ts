export type CollapseAnimationOptions = {
    durationOpen?: number;
    durationClose?: number;
    easing?: string;
};

export function animateCollapse(
    element: HTMLElement,
    collapsed: boolean,
    animationRef: { current: Animation | null },
    options?: CollapseAnimationOptions
) {
    const durationOpen = options?.durationOpen ?? 380;
    const durationClose = options?.durationClose ?? 320;
    const easing = options?.easing ?? "cubic-bezier(0.22, 1, 0.36, 1)";

    animationRef.current?.cancel();
    animationRef.current = null;

    const startHeight = element.getBoundingClientRect().height;

    if (collapsed) {
        animationRef.current = element.animate(
            [
                {
                    height: `${startHeight}px`,
                    opacity: 1,
                    paddingTop: "2px",
                    paddingBottom: "12px"
                },
                {
                    height: "0px",
                    opacity: 0,
                    paddingTop: "0px",
                    paddingBottom: "0px"
                }
            ],
            {
                duration: durationClose,
                easing,
                fill: "forwards"
            }
        );

        animationRef.current.onfinish = () => {
            element.style.height = "0px";
            element.style.opacity = "0";
            element.style.paddingTop = "0px";
            element.style.paddingBottom = "0px";

            animationRef.current?.cancel();
            animationRef.current = null;
        };

        return;
    }

    element.style.height = "auto";
    element.style.opacity = "1";
    element.style.paddingTop = "2px";
    element.style.paddingBottom = "12px";

    const endHeight = element.getBoundingClientRect().height;

    element.style.height = "0px";
    element.style.opacity = "0";
    element.style.paddingTop = "0px";
    element.style.paddingBottom = "0px";

    void element.offsetHeight;

    animationRef.current = element.animate(
        [
            {
                height: "0px",
                opacity: 0,
                paddingTop: "0px",
                paddingBottom: "0px"
            },
            {
                height: `${endHeight}px`,
                opacity: 1,
                paddingTop: "2px",
                paddingBottom: "12px"
            }
        ],
        {
            duration: durationOpen,
            easing,
            fill: "forwards"
        }
    );

    animationRef.current.onfinish = () => {
        element.style.height = "auto";
        element.style.opacity = "1";
        element.style.paddingTop = "2px";
        element.style.paddingBottom = "12px";

        animationRef.current?.cancel();
        animationRef.current = null;
    };
}