
import styles from "./SidePanel.module.css";

export const c = {
    container:          styles["container"],
    titleArea:          styles["titleArea"],
    title:              styles["title"],
    contentArea:        styles["contentArea"],
} as const;

type SidePanelProps = {
    title: string;
    children: React.ReactNode;
};


export default function SidePanel({ title, children } : SidePanelProps) {
    return (
        <div className={c.container}>
            <div className={c.titleArea}>
                <div className={c.title}>{title}</div>
            </div>
            <div className={c.contentArea}>
                {children}
            </div>
        </div>
    );
}