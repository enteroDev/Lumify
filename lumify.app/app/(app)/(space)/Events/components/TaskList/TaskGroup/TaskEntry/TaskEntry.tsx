import styles from "./TaskEntry.module.css";

export const c = {
    container: styles["container"],
    label: styles["label"],
} as const;

export type TaskItemData = {
    id: string;
    date: string;
    label: string;
};

type TaskEntryProps = {
    task: TaskItemData;
};

export default function TaskEntry({ task }: TaskEntryProps) {
    return (
        <div className={c.container}>
            <div className={c.label}>{task.label}</div>
        </div>
    );
}