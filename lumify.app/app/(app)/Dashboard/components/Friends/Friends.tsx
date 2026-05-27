"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Components
import FriendsPanel from "@/components/FriendsPanel/FriendsPanel";
// Models
import type { SelectedChatUserVM } from "@/models/Chat";
// Styles
import styles from "./Friends.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    header:             styles["header"],
    body:               styles["body"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Friends() {


    const [selectedChatUser, setSelectedChatUser] = useState<SelectedChatUserVM | null>(null);


    // --------------- //
    // --- Handler --- //
    // --------------- //
    function handleOpenChat(selectedUser: SelectedChatUserVM) {
        setSelectedChatUser(selectedUser);
        // Logic to open chat panel here.
    }


    return (
        <div className={c.container}>
            <div className={c.header}>Freunde</div>

            <div className={c.body}>
                <FriendsPanel onOpenChat={handleOpenChat}/>
            </div>
        </div>
    );
}