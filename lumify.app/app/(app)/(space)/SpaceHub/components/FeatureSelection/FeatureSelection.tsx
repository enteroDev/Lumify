

// -------------- //
// --- Styles --- //
// -------------- //

// Components
import FeatureCard from "./FeatureCard/FeatureCard";

// Icons
import ToDoIcon from "../../../../../src/svg/todo.svg";
import EventIcon from "../../../../../src/svg/calendar.svg";
import NoteIcon from "../../../../../src/svg/folder.svg";

import styles from "./FeatureSelection.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
} as const;

type FeatureSelectionProps = {
    noteCount: number;
    todoListCount: number;
    eventCount: number;
};


// ----------------- //
// --- Component --- //
// ----------------- //
export default function FeatureSelection({
    noteCount,
    todoListCount,
    eventCount,
}:FeatureSelectionProps) {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            <FeatureCard
                infoText="ToDo's: "
                value={todoListCount}
                PreviewImage={ToDoIcon}
                link="/Todos"
            />

            <FeatureCard
                infoText="Events: "
                value={eventCount}
                PreviewImage={EventIcon}
                link="/Events"
            />

            <FeatureCard
                infoText="Notes: "
                value={noteCount}
                PreviewImage={NoteIcon}
                link="/Notes" />
        </div>
    );
}