"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useRef } from "react";
// Styles
import styles from "./SidePanel.module.css";
// Icons
import ProfileIcon from "@/app/src/svg/account.svg";
import SettingsIcon from "@/app/src/svg/settings.svg";
import ImageIcon from "@/app/src/svg/image.svg";
import DeleteIcon from "@/app/src/svg/trash.svg";
import LogoutIcon from "@/app/src/svg/logout.svg";
// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],

    userBadge:          styles["userBadge"],
    avatarArea:         styles["avatarArea"],
    avatar:             styles["avatar"],
    userInfos:          styles["userInfos"],
    infoLine:           styles["infoLine"],
    infoLineLabel:      styles["infoLineLabel"],
    infoLineValue:      styles["infoLineValue"],

    navigationArea:     styles["navigationArea"],
    section:            styles["section"],
    sectionHeader:      styles["sectionHeader"],
    sectionBody:        styles["sectionBody"],
    navigationEntry:    styles["navigationEntry"],
    viewEntry:          styles["viewEntry"],
    deleteEntry:        styles["deleteEntry"],
    entryIcon:          styles["entryIcon"],
    entryText:          styles["entryText"],

    footer:             styles["footer"],
    logoutBtn:          styles["logoutBtn"],
    logoutIcon:         styles["logoutIcon"],
} as const;

type TabView = "account" | "profile";

type SidePanelProps = {
    setActiveTab: (tab: TabView) => void;
    onLogout: () => void;
    isLoggingOut: boolean;

    avatarUrl: string | null;
    isChangingAvatar?: boolean;
    onChangeAvatar: (file: File) => void;

    email: string | null;
    displayName: string | null;

    onDeleteAccount: () => void;
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function SidePanel({
    setActiveTab,
    onLogout,
    isLoggingOut,

    avatarUrl,
    isChangingAvatar = false,
    onChangeAvatar,
    email,
    displayName,
    onDeleteAccount,
}:SidePanelProps) {

    const fileInputRef = useRef<HTMLInputElement>(null);
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

    function openProfileView() {
        setActiveTab("profile");
    }

    function openAccountView() {
        setActiveTab("account");
    }


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* USERBADGE */}
            <div className={c.userBadge}>

                {/* Avatar */}
                <div className={c.avatarArea}>
                    <div
                    className={c.avatar}
                    style={{
                        backgroundImage: avatarUrl ? `url(${avatarUrl})` : undefined,
                        opacity: isChangingAvatar ? 0.5 : 1, // Slight fade while uploading
                    }}
                    ></div>
                </div>

                {/* UserInfos */}
                <div className={c.userInfos}>
                    <div className={c.infoLine}>
                        <div className={c.infoLineLabel}>Name: </div>
                        <div className={c.infoLineValue}>{displayName || "[LEER]"}</div>
                    </div>

                    <div className={c.infoLine}>
                        <div className={c.infoLineLabel}>Email: </div>
                        <div
                            className={c.infoLineValue}
                            onMouseEnter={(e) => handleTooltipMove(e, email ?? "[LEER]")}
                            onMouseMove={(e) => handleTooltipMove(e, email ?? "[LEER]")}
                            onMouseLeave={hideTooltip}
                        >
                            {email ?? "[LEER]"}
                        </div>
                    </div>

                    <div className={c.infoLine}>
                        <div className={c.infoLineLabel}>Role: </div>
                        <div className={c.infoLineValue}>User</div>
                    </div>
                </div>
            </div>


            {/* NAVIGATION */}
            <div className={c.navigationArea}>

                {/* Section: Profil */}
                <div className={c.section}>
                    {/* SectionHeader */}
                    <div className={c.sectionHeader}>Profil</div>
                    <div className={c.sectionBody}>
                        <div className={`${c.navigationEntry} ${c.viewEntry}`}  onClick={openProfileView}>
                            <div className={c.entryIcon}><ProfileIcon /></div>
                            <div className={c.entryText}>Profil ansehen/bearbeiten</div>
                        </div>
                        <div className={c.navigationEntry} onClick={() => fileInputRef.current?.click()}>
                            <div className={c.entryIcon}><ImageIcon /></div>
                            <div className={c.entryText}>Avatar wechseln</div>

                            <input
                                ref={fileInputRef}
                                type="file"
                                accept="image/*"
                                style={{ display: "none" }}
                                onChange={(e) => {
                                    const file = e.target.files?.[0];
                                    if (file) onChangeAvatar(file);
                                }}
                            />
                        </div>
                    </div>
                </div>

                {/* Section: Account */}
                <div className={c.section}>
                    {/* SectionHeader */}
                    <div className={c.sectionHeader}>Account</div>
                    <div className={c.sectionBody}>
                        <div className={`${c.navigationEntry} ${c.viewEntry}`} onClick={openAccountView}>
                            <div className={c.entryIcon}><SettingsIcon /></div>
                            <div className={c.entryText}>Account verwalten</div>
                        </div>
                        <div className={`${c.navigationEntry} ${c.deleteEntry}`} onClick={onDeleteAccount}>
                            <div className={c.entryIcon}><DeleteIcon /></div>
                            <div className={c.entryText}>Account löschen</div>
                        </div>
                    </div>
                </div>

            </div>


            {/* FOOTER */}
            <div className={c.footer}>
                <button className={c.logoutBtn} onClick={onLogout} disabled={isLoggingOut}>
                    <div className={c.logoutIcon}>
                        <LogoutIcon />
                    </div>
                    Ausloggen
                </button>
            </div>
        </div>
    );
}