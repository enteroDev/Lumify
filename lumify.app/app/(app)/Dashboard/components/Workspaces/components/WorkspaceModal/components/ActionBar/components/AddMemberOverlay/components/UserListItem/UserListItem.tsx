"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";

// Styles
import styles from "./UserListItem.module.css";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],

    infoArea:           styles["infoArea"],
    name:               styles["name"],
    info:               styles["info"],

    actionArea:         styles["actionArea"],
    button:             styles["button"],
} as const;

export type UserListItemProps = {
    userID: string;
    displayName: string;
    username: string;
    email: string;
    onAddMember: (userID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function UserListItem({
    userID,
    displayName,
    username,
    email,
    onAddMember,
}: UserListItemProps) {

    const { showTooltip, hideTooltip } = useTooltip();


    // ------------------ //
    // --- UI-Helpers --- //
    // ------------------ //
    const renderName = () => {
        if (!displayName){
            return username;
        }

        return displayName;
    } 


    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //
    const handleTooltipMove = (e: React.MouseEvent<HTMLElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };


    // ------------------- //
    // --- Handlers --- //
    // ------------------- //
    const handleAddMember = () => {
        onAddMember(userID);
    };


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.infoArea}>
                <div className={c.name}>{renderName()}</div>
                <div className={c.info}>{email}</div>
            </div>
            <div className={c.actionArea}>
                <button 
                    className={c.button} 

                    onMouseEnter={(e) => handleTooltipMove(e, "Member hinzufügen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Member hinzufügen")}
                    onMouseLeave={hideTooltip}

                    onClick={handleAddMember}
                >
                    +
                </button>
            </div>
        </div>
    );
}