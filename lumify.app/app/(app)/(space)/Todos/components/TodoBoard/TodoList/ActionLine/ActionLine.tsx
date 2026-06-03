

// --------------- //
// --- Imports --- //
// --------------- //

// Icons
import AddIcon from "@/app/src/svg/add.svg";
import TrashIcon from "@/app/src/svg/trash.svg"
import HeartIcon from "@/app/src/svg/heart.svg"
import MaximizeIcon from "@/app/src/svg/maximize.svg"
// Styles
import styles from "./ActionLine.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
    customArea:     styles["custom-area"],
    deleteArea:     styles["delete-area"],
    actionButton:   styles["action-button"],
} as const;

export type ActionLineProps = {
    onCreateDraftTodoEntry: () => void;
    ondeleteTodoList: () => void;
};





// ----------------- //
// --- Component --- //
// ----------------- //
export default function ActionLine({
    onCreateDraftTodoEntry,
    ondeleteTodoList,
}:ActionLineProps) {


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.customArea}>
                <button className={c.actionButton} onClick={onCreateDraftTodoEntry}><AddIcon /></button>
                {/*<button className={c.actionButton}><MaximizeIcon /></button>*/}
                {/*<button className={c.actionButton}><HeartIcon /></button>*/}
            </div>

            <div className={c.deleteArea}>
                <button className={c.actionButton} onClick={ondeleteTodoList}><TrashIcon /></button>
            </div>
        </div>
    );
}
