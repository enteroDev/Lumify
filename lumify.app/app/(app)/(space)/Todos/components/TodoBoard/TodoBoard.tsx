

// -------------- //
// --- Import --- //
// -------------- //

// Components
import TodoList from "./TodoList/TodoList";
import AddCard from "./AddCard/AddCard";
// Models
import type { TodoListDTO, TodoEntryDTO } from "@/models/Todos";
// Styles
import styles from "./TodoBoard.module.css";




// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
} as const;

export type TodoBoardProps = {
    todoLists: TodoListDTO[];
    todoEntries: TodoEntryDTO[];
    editingTodoListID: string | null;
    editingTodoEntryID: string | null;

    onStartEditTodoList: (todoListID: string) => void;
    onCancelEditTodoList: (todoListID: string) => void;
    onCancelEditTodoEntry: (todoEntryID: string) => void;

    onCreateDraftTodoList: () => void;
    onAddTodoList: (todoListID: string, name: string) => void;
    onSaveTodoList: (todoListID: string, name: string) => Promise<void> | void;

    onCreateDraftTodoEntry: (todoListID: string) => void;
    onAddTodoEntry: (todoEntryID: string, todoListID: string, name: string, description?: string) => void;
    onSaveTodoEntry: (todoEntry: TodoEntryDTO) => Promise<void> | void;

    ondeleteTodoList: (todoListID: string) => void;
    onDeleteTodoEntry: (todoEntryID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function TodoBoard({
    todoLists,
    todoEntries,
    editingTodoListID,
    editingTodoEntryID,

    onStartEditTodoList,
    onCancelEditTodoList,
    onCancelEditTodoEntry,

    onCreateDraftTodoList,
    onAddTodoList,
    onSaveTodoList,

    onCreateDraftTodoEntry,
    onAddTodoEntry,
    onSaveTodoEntry,

    ondeleteTodoList,
    onDeleteTodoEntry,
}: TodoBoardProps) {


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const todoListPackage = todoLists.map((todoList) => ({
        todoList,
        entries: todoEntries.filter((entry) => entry.todoListID === todoList.id),
    }));


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
    <div className={c.container}>
        {todoListPackage.map(({ todoList, entries }) => (
            <TodoList
                key={todoList.id}
                todoList={todoList}
                todoEntries={entries}

                isEditing={editingTodoListID === todoList.id}
                editingTodoEntryID={editingTodoEntryID}

                onStartEdit={onStartEditTodoList}
                onCancelEdit={onCancelEditTodoList}
                onCancelEditTodoEntry={onCancelEditTodoEntry}

                onAddTodoList={onAddTodoList}
                onSaveTodoList={onSaveTodoList}

                onCreateDraftTodoEntry={onCreateDraftTodoEntry}
                onAddTodoEntry={onAddTodoEntry}
                onSaveTodoEntry={onSaveTodoEntry}

                ondeleteTodoList={ondeleteTodoList}
                onDeleteTodoEntry={onDeleteTodoEntry}
            />
        ))}
        <AddCard onCreateDraftTodoList={onCreateDraftTodoList} />
    </div>
);
}