"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// Components
import TodoCollectionEntry from "./CollectionEntry/CollectionEntry";
// Models
import type { TodoListDTO} from "@/models/Todos";
import { TODO_STATUS } from "@/models/Todos";
// Styles
import styles from "./CollectionGroup.module.css";


// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    title:              styles["title"],
    body:               styles["body"],
} as const;

export type Props = {
    title: string;
    todoLists: TodoListDTO[];
}


// ----------------- //
// --- Component --- //
// ----------------- //
export default function TodoCollectionGroup({
    title,
    todoLists,
}: Props) {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.title}>{ title }</div>
            <div className={c.body}>
                {todoLists.map((todoList) => (
                    <TodoCollectionEntry
                        key={todoList.id}
                        id={todoList.id}
                        title={todoList.name}
                        status={todoList.status}
                    />
                ))}
            </div>
        </div>
    );
}