

import styles from "./OverlayContainer.module.css";

export const c = {
    container:      styles["container"],
    image:          styles["overlay-image"],
    chatBubble:     styles["chat-bubble"],
    bubbleText:     styles["bubble-text"],
} as const;

/* Animation timing configuration */
const ANIMATION_DELAY_STEP = 0.06;
const ANIMATION_BASE_DURATION = 0.45;
const ANIMATION_DURATION_VARIATION = 0.1;

/* Builds animated character spans */
function buildAnimatedChars(text: string) {
    return text.split("").map((char, i) => {
        const animationDelay = `${i * ANIMATION_DELAY_STEP}s`;
        const animationDuration = `${ANIMATION_BASE_DURATION + (i % 3) * ANIMATION_DURATION_VARIATION}s`;

        return (
            <span key={i} style={{ animationDelay, animationDuration }}> 
                {char === " " ? "\u00A0" : char}
            </span>
        );
    });
}



export default function OverlayContainer() {
    return (
        <div className={c.container}>
            <img className={c.image} src="/src/cat-sleep.png" alt="" />
            <img className={c.chatBubble} src="/src/chat-bubble.png" alt="" />

            <div className={c.bubbleText}>
                {buildAnimatedChars("Schnurr, Schnurr")}
            </div>
        </div>
    );
}