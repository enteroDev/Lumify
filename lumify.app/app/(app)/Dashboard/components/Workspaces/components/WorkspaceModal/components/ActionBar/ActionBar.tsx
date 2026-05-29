"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import AddMemberOverlay from "./components/AddMemberOverlay/AddMemberOverlay";
// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";
// Models
import type { RelatedUserDTO } from "@/models/User";
// Icons
import SearchIcon from "@/app/src/svg/search.svg";
// Styles
import styles from "./ActionBar.module.css";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],
    titleArea:          styles["titleArea"],
    title:              styles["title"],
    actionArea:         styles["actionArea"],
    searchBar:          styles["searchBar"],
    input:              styles["input"],
    searchIcon:         styles["searchIcon"],
    button:             styles["button"],
} as const;

export type ActionBarProps = {
    memberSearchValue: string;
    onMemberSearchChange: (value: string) => void;
    userSearchValue: string;
    onUserSearchChange: (value: string) => void;

    isAddMemberOpen: boolean;
    relatedUsers: RelatedUserDTO[];

    onShowAddMemberOverlay: () => void;
    onCloseAddMemberOverlay: () => void;
    onAddMember: (userID: string) => void;
};




// ----------------- //
// --- Component --- //
// ----------------- //
export default function ActionBar({
    memberSearchValue,
    onMemberSearchChange,
    userSearchValue,
    onUserSearchChange,

    isAddMemberOpen,
    relatedUsers,

    onShowAddMemberOverlay,
    onCloseAddMemberOverlay,
    onAddMember,
}: ActionBarProps) {

    const { showTooltip, hideTooltip } = useTooltip();



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



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            {/* TitleArea */}
            <div className={c.titleArea}>
                <div className={c.title}>Member:</div>
            </div>

            {/* ActionArea */}
            <div className={c.actionArea}>

                {/* SearchBar */}
                <div className={c.searchBar}>
                    <div className={c.searchIcon}><SearchIcon /></div>
                    <input 
                        className={c.input} 
                        type="text"
                        placeholder="Mitglieder suchen..."

                        value={memberSearchValue}
                        onChange={(e) => onMemberSearchChange(e.target.value)}
                    />
                </div>

                {/* Button */}
                <button 
                    className={c.button}

                    onMouseEnter={(e) => handleTooltipMove(e, "Finde user für diesen Space")}
                    onMouseMove={(e) => handleTooltipMove(e, "Finde user für diesen Space")}
                    onMouseLeave={hideTooltip}

                    onClick={onShowAddMemberOverlay}
                >
                    +
                </button>

                <AddMemberOverlay
                    isOpen={isAddMemberOpen}
                    relatedUsers={relatedUsers}
                    onClose={onCloseAddMemberOverlay}
                    onAddMember={onAddMember}
                    userSearchValue={userSearchValue}
                    onUserSearchChange={onUserSearchChange}
                />
            </div>
        </div>
    );
}