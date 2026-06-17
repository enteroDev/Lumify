"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import FriendsPanel from "@/components/FriendsPanel/FriendsPanel";
// Context
import { useFriendsOverlay } from "@/components/FriendsOverlay/FriendsOverlay";
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

    const { openChat, friendshipVersion, notifyFriendshipMutated } = useFriendsOverlay();

    return (
        <div className={c.container}>
            <div className={c.header}>Freunde</div>

            <div className={c.body}>
                <FriendsPanel onOpenChat={openChat} friendshipVersion={friendshipVersion} onFriendshipMutated={notifyFriendshipMutated} />
            </div>
        </div>
    );
}