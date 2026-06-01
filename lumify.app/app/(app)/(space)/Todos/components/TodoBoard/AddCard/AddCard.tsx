

// --------------- //
// --- Imports --- //
// --------------- //
import AddIcon from "@/app/src/svg/add.svg";
import styles from "./AddCard.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:  styles["container"],
} as const;

type AddCardProps = {
    onCreateDraftTodoList: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AddCard({
    onCreateDraftTodoList,
}:AddCardProps) {

    return (
        <div className={c.container} onClick={onCreateDraftTodoList}>
            <AddIcon />
        </div>
    );
}
