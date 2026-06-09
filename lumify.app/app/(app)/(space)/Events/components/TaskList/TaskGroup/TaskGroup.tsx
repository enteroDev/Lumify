import TaskEntry from "./TaskEntry/TaskEntry";
import styles from "./TaskGroup.module.css";

export const c = {
    container: styles["container"],
    header: styles["header"],
    body: styles["body"],
} as const;

type TaskGroupProps = {
    date: string;
    tasks: { id: string; date: string; label: string }[];
};



export default function TaskGroup({ date, tasks }: TaskGroupProps) {
    return (
        <div className={c.container}>
            <div className={c.header}>{date}</div>
            <div className={c.body}>
                {tasks.map(t => (
                    <TaskEntry key={t.id} task={t} />
                ))}
            </div>
        </div>
    );
}
