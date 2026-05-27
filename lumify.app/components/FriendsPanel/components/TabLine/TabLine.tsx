"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Types
import type { FriendsPanelTab } from "../../FriendsPanel";
// Styles
import styles from "./TabLine.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:  styles["container"],

    tabs:       styles["tabs"],
    tabsLeft:   styles["tabsLeft"],
    tabsRight:  styles["tabsRight"],

    tab:        styles["tab"],
    activeTab:  styles["activeTab"],
} as const;

type TabLineProps = {
    activeTab: FriendsPanelTab;
    onTabChange: (tab: FriendsPanelTab) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function TabLine({
    activeTab,
    onTabChange,
}: TabLineProps) {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.tabs}>
                <div className={c.tabsLeft}>
                    <button
                        type="button"
                        className={`${c.tab} ${activeTab === "friendList" ? c.activeTab : ""}`}
                        onClick={() => onTabChange("friendList")}
                    >
                        Freunde
                    </button>

                    <button
                        type="button"
                        className={`${c.tab} ${activeTab === "requests" ? c.activeTab : ""}`}
                        onClick={() => onTabChange("requests")}
                    >
                        Anfragen
                    </button>
                </div>

                <div className={c.tabsRight}>
                    <button
                        type="button"
                        className={`${c.tab} ${activeTab === "userList" ? c.activeTab : ""}`}
                        onClick={() => onTabChange("userList")}
                    >
                        Userliste
                    </button>
                </div>
            </div>
        </div>
    );
}