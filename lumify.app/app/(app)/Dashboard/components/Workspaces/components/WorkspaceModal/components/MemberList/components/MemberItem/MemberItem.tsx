"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";
// Icons & Images
import RemoveMemberIcon from "@/app/src/svg/user_remove_3.svg";
import AvatarImage from "@/public/src/Avatar.png";
// Styles
import styles from "./MemberItem.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:      styles["container"],

    avatarArea:     styles["avatarArea"],
    avatar:         styles["avatar"],
    avatarImage:    styles["avatarImage"],

    infoArea:       styles["infoArea"],
    username:       styles["username"],
    email:          styles["email"],

    actionArea:     styles["actionArea"],
    button:         styles["button"],
} as const;


export type MemberItemProps = {
    avatar: string | null;
    displayName: string;
    username: string;
    email: string;
    onRemove: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function MemberItem({
    avatar,
    displayName,
    username,
    email,
    onRemove,
}: MemberItemProps) {

    const { showTooltip, hideTooltip } = useTooltip();


    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //
    const handleTooltipMove = (e: React.MouseEvent<HTMLElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };

    // Render DisplayName. If displayName not available use the fallback username.
    const renderName = () => {
        if (!displayName){
            return username;
        }

        return displayName;
    }

    // Render userAvatar if available or display genericImage.
    const renderAvatar = () => {
        if (!avatar || avatar === "") {
            return (
                <img
                    src={AvatarImage.src}
                    alt={`${displayName} avatar`}
                    className={c.avatarImage}
                />
            );
        }

        return (
            <img
                src={avatar}
                alt={`${displayName} avatar`}
                className={c.avatarImage}
            />
        );
    };




    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* AvatarArea */}
            <div className={c.avatarArea}>
                <div className={c.avatar}>
                    {renderAvatar()}
                </div>
            </div>

            {/* InfoArea */}
            <div className={c.infoArea}>
                <div className= {c.username}>{renderName()}</div>
                <div className= {c.email}>{email}</div>
            </div>

            {/* ActionArea */}
            <div className={c.actionArea}>
                <button 
                    className= {c.button} 
                    onClick={onRemove}

                    onMouseEnter={(e) => handleTooltipMove(e, "Member aus Space entfernen")}
                    onMouseMove={(e) => handleTooltipMove(e, "Member aus Space entfernen")}
                    onMouseLeave={hideTooltip}
                >
                    <RemoveMemberIcon/>
                </button>
            </div>
        </div>
    );
}