"use client";

// --------------- //
// --- Imports --- //
// --------------- //


// Components
import FriendsPanel from "@/components/FriendsPanel/FriendsPanel";
// Models
import type { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./FriendsSection.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
} as const;

type FriendsSectionProps = {
    onOpenChat: (selectedUser: SelectedChatUserVM) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function FriendsSection({onOpenChat}:FriendsSectionProps) {


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <FriendsPanel onOpenChat={onOpenChat}/>
        </div>
    );
}