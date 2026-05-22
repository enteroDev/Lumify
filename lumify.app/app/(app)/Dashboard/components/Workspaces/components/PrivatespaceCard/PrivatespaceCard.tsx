// --------------- //
// --- Imports --- //
// --------------- //

// Icons
import CupIcon from "../../../../../../src/svg/cup.svg";
import TodoIcon from "../../../../../../src/svg/todo.svg";
import NoteIcon from "../../../../../../src/svg/folder.svg";
import EventIcon from "../../../../../../src/svg/calendar.svg";
// Styles
import styles from "./PrivatespaceCard.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],

    header:             styles["header"],
    title:              styles["title"],
    info:               styles["info"],
    infoLabel:          styles["infoLabel"],
    infoValue:          styles["infoValue"],

    body:               styles["body"],

    featureBox:         styles["featureBox"],
    item:               styles["item"],
    itemLabel:          styles["itemLabel"],
    itemIcon:           styles["itemIcon"],

    icon:               styles["icon"],
} as const;

export type PrivatespaceCardProps = {
    name: string | null;
    ownerName: string | null;

    onOpenSpace?: () => void;
    onOpenTodos?: () => void;
    onOpenEvents?: () => void;
    onOpenNotes?: () => void;
};


// ----------------- //
// --- Component --- //
// ----------------- //
export default function PrivateSpaceCard({
    name,
    ownerName,
    onOpenSpace,
    onOpenTodos,
    onOpenEvents,
    onOpenNotes,
}: PrivatespaceCardProps) {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div
            className={c.container}
            onClick={() => {
                onOpenSpace?.();
            }}
        >

            {/* HEADER */}
            <div className={c.header}>

                {/* Title */}
                <div className={c.title}>{name}</div>

                {/* Infos */}
                <div className={c.info}>
                    <div className={c.infoLabel}>Owner:</div>
                    <div className={c.infoValue}>{ownerName}</div>
                </div>
            </div>


            {/* BODY */}
            <div className={c.body}>
                <div className={c.icon}><CupIcon /></div>
            </div>


            {/* FEATURE-BOX */}
            <div className={c.featureBox}>

                {/* Item: Todos */}
                <div
                    className={c.item}
                    onClick={(e) => {
                        e.stopPropagation();
                        onOpenTodos?.();
                    }}
                >
                    <div className={c.itemLabel}>Todos</div>
                    <div className={c.itemIcon}><TodoIcon /></div>
                </div>

                {/* Item: Events */}
                <div
                    className={c.item}
                    onClick={(e) => {
                        e.stopPropagation();
                        onOpenEvents?.();
                    }}
                >
                    <div className={c.itemLabel}>Events</div>
                    <div className={c.itemIcon}><EventIcon /></div>
                </div>

                {/* Item: Notes */}
                <div
                    className={c.item}
                    onClick={(e) => {
                        e.stopPropagation();
                        onOpenNotes?.();
                    }}
                >
                    <div className={c.itemLabel}>Notes</div>
                    <div className={c.itemIcon}><NoteIcon /></div>
                </div>
            </div>
        </div>
    );
}