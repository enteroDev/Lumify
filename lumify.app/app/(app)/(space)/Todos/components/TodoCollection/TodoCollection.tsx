
/** CATEGORIZE TODOS
 * This file sorts todos by their status (Pending / Done)
 * Todos getting pushed to their corresponding group-container
 **/

// Components
import TodoCollectionGroup from "./CollectionGroup/CollectionGroup";
// Models
import type { TodoListDTO} from "@/models/Todos";
import { TODO_STATUS } from "@/models/Todos";
// Styles
import styles from "./TodoCollection.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
} as const;

type TodoCollectionProps = {
    todoLists: TodoListDTO[];
};




// ----------------- //
// --- Component --- //
// ----------------- //
export default function TodoCollection({
    todoLists,
}: TodoCollectionProps) {


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const pendingTodoLists = todoLists.filter((t) => t.status === TODO_STATUS.PENDING);
    const doneTodoLists = todoLists.filter((t) => t.status === TODO_STATUS.DONE);


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <TodoCollectionGroup title="Offen" todoLists={pendingTodoLists} />
            <TodoCollectionGroup title="Erledigt" todoLists={doneTodoLists} />
        </div>
    );
}