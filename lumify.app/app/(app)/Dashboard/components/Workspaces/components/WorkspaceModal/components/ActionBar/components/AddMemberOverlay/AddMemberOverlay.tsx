"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import UserListItem from "./components/UserListItem/UserListItem";
// Models
import type { RelatedUserDTO } from "@/models/User";
// Icons
import SearchIcon from "@/app/src/svg/search.svg";
import CloseIcon from "@/app/src/svg/close.svg";
// Styles
import styles from "./AddMemberOverlay.module.css";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],
    containerOpen:      styles["containerOpen"],

    header:             styles["header"],
    title:              styles["title"],
    closeButton:        styles["closeButton"],

    body:               styles["body"],
    searchBar:          styles["searchBar"],
    searchIcon:         styles["searchIcon"],
    input:              styles["input"],

    userListScroll:     styles["userListScroll"],
    userList:           styles["userList"],
    userListItem:       styles["userListItem"],

    footer:             styles["footer"],

} as const;

export type AddMemberOverlayProps = {
    isOpen: boolean;
    relatedUsers: RelatedUserDTO[];

    onClose: () => void;
    onAddMember: (userID: string) => void;

    userSearchValue: string;
    onUserSearchChange: (value: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AddMemberOverlay({
    isOpen,
    relatedUsers,
    onClose,
    onAddMember,
    userSearchValue,
    onUserSearchChange,
}: AddMemberOverlayProps) {



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const containerClass = `${c.container} ${isOpen ? c.containerOpen : ""}`;



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={containerClass}>
            <div className={c.header}>
                <div className={c.title}>Member hinzufügen</div>
                <button className={c.closeButton} onClick={onClose}><CloseIcon /></button>
            </div>

            <div className={c.body}>
                <div className={c.searchBar}>
                    <div className={c.searchIcon}><SearchIcon /></div>
                    <input
                        className={c.input}
                        placeholder="Suche User"
                        value={userSearchValue}
                        onChange={(e) => onUserSearchChange(e.target.value)}
                    />
                </div>

                <div className={c.userListScroll}>
                    <div className={c.userList}>
                        {relatedUsers.map((user) => (
                            <UserListItem
                                key={user.userID}
                                userID={user.userID}
                                avatarUrl={user.avatarUrl}
                                displayName={user.displayName}
                                username={user.username}
                                email={user.email}
                                presenceStatus={user.presenceStatus}
                                onAddMember={onAddMember}
                            />
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}