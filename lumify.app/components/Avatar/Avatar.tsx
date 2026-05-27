// --------------- //
// --- Imports --- //
// --------------- //

// React
import { MouseEvent } from "react";

// Provider
import { useTooltip } from "@/components/Tooltip/TooltipProvider";

// Utils
import { CONFIG } from "@/app/(app)/config/config";

// Models
import { PresenceStatus } from "@/models/User";

// Icons
import PresenceOnlineIcon from "@/app/src/svg/accept.svg";
import PresenceOfflineIcon from "@/app/src/svg/close.svg";
import PresenceAwayIcon from "@/app/src/svg/idle.svg";
import PresenceDoNotDisturbIcon from "@/app/src/svg/abort.svg";

// Styles
import styles from "./Avatar.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:         styles["container"],

    avatar:             styles["avatar"],
    presenceIndicator:  styles["presenceIndicator"],
    indicatorOnline:    styles["indicator-online"],
    indicatorOffline:   styles["indicator-offline"],
    indicatorIdle:      styles["indicator-idle"],
    indicatorDnd:       styles["indicator-dnd"],
} as const;

type AvatarProps = {
    avatarUrl?: string | null;
    displayName: string;
    presenceStatus: PresenceStatus;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function Avatar({
    avatarUrl,
    displayName,
    presenceStatus,
}: AvatarProps) {

    const { showTooltip, hideTooltip } = useTooltip();


    // --------------- //
    // --- Handler --- //
    // --------------- //
    const handleTooltipMove = (e: MouseEvent<HTMLElement>, text: string) => {
        showTooltip({
            text,
            x: e.clientX,
            y: e.clientY,
        });
    };


    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //
    const renderPresenceIndicator = () => {
        switch (presenceStatus) {
            case PresenceStatus.Online:
                return (
                    <div
                        className={c.indicatorOnline}

                        onMouseEnter={(e) => handleTooltipMove(e, "Online")}
                        onMouseMove={(e) => handleTooltipMove(e, "Online")}
                        onMouseLeave={hideTooltip}
                    >
                        <PresenceOnlineIcon />
                    </div>
                );

            case PresenceStatus.Away:
                return (
                    <div
                        className={c.indicatorIdle}

                        onMouseEnter={(e) => handleTooltipMove(e, "Abwesend")}
                        onMouseMove={(e) => handleTooltipMove(e, "Abwesend")}
                        onMouseLeave={hideTooltip}
                    >
                        <PresenceAwayIcon />
                    </div>
                );

            case PresenceStatus.DoNotDisturb:
                return (
                    <div
                        className={c.indicatorDnd}

                        onMouseEnter={(e) => handleTooltipMove(e, "Bitte nicht stören")}
                        onMouseMove={(e) => handleTooltipMove(e, "Bitte nicht stören")}
                        onMouseLeave={hideTooltip}
                    >
                        <PresenceDoNotDisturbIcon />
                    </div>
                );

            case PresenceStatus.Offline:
            default:
                return (
                    <div
                        className={c.indicatorOffline}

                        onMouseEnter={(e) => handleTooltipMove(e, "Offline")}
                        onMouseMove={(e) => handleTooltipMove(e, "Offline")}
                        onMouseLeave={hideTooltip}
                    >
                        <PresenceOfflineIcon />
                    </div>
                );
        }
    };


    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const avatarSrc = avatarUrl || CONFIG.ASSETS.DEFAULT_AVATAR_URL;



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.avatar}>
                <img src={avatarSrc} alt={displayName} />

                <div className={c.presenceIndicator}>
                    {renderPresenceIndicator()}
                </div>
            </div>
        </div>
    );
}