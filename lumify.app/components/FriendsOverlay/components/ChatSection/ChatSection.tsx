"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import ChatPanel from "@/components/ChatPanel/ChatPanel";
// Models
import type { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./ChatSection.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
} as const;

type ChatSectionProps = {
    selectedChatUser: SelectedChatUserVM | null;
    onCloseChat: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ChatSection({
    selectedChatUser,
    onCloseChat,
}: ChatSectionProps) {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <ChatPanel selectedChatUser={selectedChatUser} onCloseChat={onCloseChat} />
        </div>
    );
}