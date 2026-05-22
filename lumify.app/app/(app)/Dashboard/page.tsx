
// --------------- //
// --- Imports --- //
// --------------- //

// Components
import DashboardHeading from "./components/DashboardHeading/DashboardHeading";
import Workspaces from "./components/Workspaces/Workspaces";
// Styles
import styles from "./Dashboard.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    headingArea:        styles["heading-area"],
    dashboardGrid:      styles["dashboard-grid"],
    allSpan:            styles["grid-all-span"],
    twoSpan:            styles["grid-2-span"],
    oneSpan:            styles["grid-1-span"],
    test:               styles["test"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Dashboard() {
    return (
        <div className="scrollView">
            <div className="bg-overlay" aria-hidden="true" />

            <div className="content-fullHeight">
                <div className={c.headingArea}><DashboardHeading /></div>

                <div className={c.dashboardGrid}>
                    <div className={c.allSpan}>
                        <Workspaces />
                    </div>
                    <div className={c.oneSpan}>

                    </div>
                    <div className={c.oneSpan}>

                    </div>
                </div>
            </div>
        </div>
    );
}
