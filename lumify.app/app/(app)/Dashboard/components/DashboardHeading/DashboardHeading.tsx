

// --------------- //
// --- Imports --- //
// --------------- //

// Styles
import styles from "./DashboardHeading.module.css";



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
export default function DashboardHeading() {
    return (
        <div className={c.container}>
            <div className={c.headingBig}>Lumify</div>
            <div className={c.headingSmall}>Bring clarity into your life</div>
        </div>
    );
}