

// --------------- //
// --- Imports --- //
// --------------- //

// Styles
import styles from "./Heading.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    headingBig:         styles["heading-big"],
    headingSmall:       styles["heading-small"],
    authLink:           styles["authLink"],
} as const;


// ----------------- //
// --- Component --- //
// ----------------- //
export default function Heading() {
    return (
        <div className={c.container}>
            <div className={c.headingBig}>Lumify</div>
            <div className={c.headingSmall}>Bring clarity into your life</div>
        </div>
    );
}