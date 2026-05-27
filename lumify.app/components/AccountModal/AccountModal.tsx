"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState, useEffect } from "react";
// Components
import SidePanel from "@/components/AccountModal/components/SidePanel/SidePanel";
import AccountView from "@/components/AccountModal/components/AccountView/AccountView";
import ProfileView from "@/components/AccountModal/components/ProfileView/ProfileView";
// Provider
import { useAccountModal } from "@/components/AccountModal/AccountModalProvider";
import { useToast } from "@/components/Toast/ToastProvider";
// Services
import { AuthService } from "@/services/api/authService";
import { UserService } from "@/services/api/userService";
// Icons
import CloseIcon from "@/app/src/svg/close.svg";
// Models
import { UserProfile, UserAccountInfo, SaveUserProfileRequest, SaveAccountInfoRequest } from "@/models/User";
// Styles
import styles from "./AccountModal.module.css";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    overlay:        styles["overlay"],
    modal:          styles["modal"],
    sidePanel:      styles["sidePanel"],
    content:        styles["content"],
    close:          styles["close"],
} as const;

type TabView = "account" | "profile" | "workspaces";


// ----------------- //
// --- Component --- //
// ----------------- //
export default function AccountModal() {

    const { isOpen, closeModal, avatarUrl, setAvatarUrl, displayName, setDisplayName } = useAccountModal();
    const toast = useToast();

    const [activeTab, setActiveTab] = useState<TabView>("profile");
    const [userProfile, setUserProfile] = useState<UserProfile | null>(null);
    const [accountInfo, setAccountInfo] = useState<UserAccountInfo | null>(null);

    const [isLoggingOut, setIsLoggingOut] = useState(false);
    const [isChangingAvatar, setIsChangingAvatar] = useState(false);
    const [isSavingProfile, setIsSavingProfile] = useState(false);
    const [isSavingAccountInfo, setIsSavingAccountInfo] = useState(false);



    // ------------------ //
    // --- UI Handler --- //
    // ------------------ //
    function renderContent() {
        if (activeTab === "account") {
            return <AccountView
            firstName={accountInfo?.firstName ?? ""}
            lastName={accountInfo?.lastName ?? ""}
            email={accountInfo?.email ?? ""}
            username={accountInfo?.username ?? ""}
            onSaveAccountInfo={saveAccountInfo}
            isSavingAccountInfo={isSavingAccountInfo} />;
        }

        if (activeTab === "profile") {
            return <ProfileView
            avatarUrl={avatarUrl}
            onChangeAvatar={changeAvatar}
            isChangingAvatar={isChangingAvatar}
            email={accountInfo?.email ?? ""}
            displayName={userProfile?.displayName ?? ""}
            bio={userProfile?.bio ?? ""}
            onSaveProfile={saveUserProfile}
            isSavingProfile={isSavingProfile} />;
        }

        return <ProfileView
        avatarUrl={avatarUrl}
        onChangeAvatar={changeAvatar}
        isChangingAvatar={isChangingAvatar}
        email={accountInfo?.email ?? ""}
        displayName={userProfile?.displayName ?? ""}
        bio={userProfile?.bio ?? ""}
        onSaveProfile={saveUserProfile}
        isSavingProfile={isSavingProfile} />; // Backfall
    }



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //

    // User-Profile
    useEffect(() => {
        if (!isOpen) { return; }
        if (userProfile) { return; }

        void getUserProfile();
    }, [isOpen, userProfile]);

    // User-AccountInfo
    useEffect(() => {
        if (!isOpen) { return; }
        if (accountInfo) { return; }

        void getAccountInfo();
    }, [isOpen, accountInfo]);



    // ------------- //
    // --- Logic --- //
    // ------------- //

    // ### GET ### //
    async function getUserProfile() {
        try {
            const userProfile = await UserService.getUserProfile();
            setUserProfile(userProfile);
        } catch (err) {
            console.error("Failed to fetch profile:", err);
        }
    }

    async function getAccountInfo() {
        try {
            const userAccountInfo = await UserService.getUserAccountInfo();
            setAccountInfo(userAccountInfo);
        } catch (err) {
            console.error("Failed to fetch account info:", err);
        }
    }


    // ### SAVE ### //
    async function changeAvatar(file: File) {
        try {
            setIsChangingAvatar(true);

            // 0) Show preview immediately
            const previewUrl = URL.createObjectURL(file);
            setAvatarUrl(previewUrl);

            // 1) Save Avatar and receive relative path from API
            const avatarUrl = await UserService.saveUserAvatar(file);

            // 2) Add cache-busting query param so browser reloads image
            const avatarUrlWithCacheBust = `${avatarUrl}?t=${Date.now()}`;

            // 3) Update UI with new avatar
            setAvatarUrl(avatarUrlWithCacheBust);
        } catch (err) {
            console.error("Failed to change avatar of user:", err);
            toast.error("Fehler beim Speichern des Avatars.");
        } finally {
            setIsChangingAvatar(false);
        }
    }

    async function saveUserProfile(data: SaveUserProfileRequest) {
        try {
            setIsSavingProfile(true);

            const savedProfile = await UserService.saveUserProfile(data);

            setUserProfile((prev) => {
                if (!prev) { return savedProfile; }

                return {
                    ...prev,
                    ...savedProfile,
                };
            });

            setDisplayName(savedProfile.displayName || savedProfile.username || data.displayName || null);

            toast.success("Profil erfolgreich gespeichert.");
        } catch (err) {
            console.error("Failed to save user profile:", err);
            toast.error("Fehler beim Speichern des Profils.");
        } finally {
            setIsSavingProfile(false);
        }
    }

    async function saveAccountInfo(data: SaveAccountInfoRequest) {
        try {
            setIsSavingAccountInfo(true);

            const savedAccountInfo = await UserService.saveAccountInfo(data);

            setAccountInfo((prev) => {
                if (!prev) { return savedAccountInfo; }

                return {
                    ...prev,
                    ...savedAccountInfo,
                };
            });

            toast.success("Konto-Informationen erfolgreich gespeichert.");
        } catch (err) {
            console.error("Failed to save user account info:", err);
            toast.error("Fehler beim Speichern der Kontoinformationen.");
        } finally {
            setIsSavingAccountInfo(false);
        }
    }


    // ### LOGOUT ### //
    async function logout() {
        try {
            setIsLoggingOut(true);

            // Clear Cookies, close modals and redirect to login-page
            await AuthService.logout();
            closeModal();
            window.location.href = "/Auth";
        } catch (error) {
            console.error("Logout failed:", error);
            toast.error("Fehler beim Abmelden.");
        } finally {
            setIsLoggingOut(false);
        }
    }


    // If not open, escape and do not render anyhting
    if (!isOpen) { return null; }



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.overlay} onClick={closeModal}>
            <div className={c.modal} onClick={(e) => e.stopPropagation()}>

                {/* SidePanel */}
                <div className={c.sidePanel}>
                    <SidePanel
                        setActiveTab={setActiveTab}
                        onLogout={logout}
                        isLoggingOut={isLoggingOut}
                        avatarUrl={avatarUrl}
                        onChangeAvatar={changeAvatar}
                        isChangingAvatar={isChangingAvatar}
                        email={accountInfo?.email ?? ""}
                        displayName={userProfile?.displayName ?? ""}
                    />
                </div>

                {/* Content */}
                <div className={c.content}>
                    {renderContent()}
                </div>

                <div className={c.close} onClick={closeModal}><CloseIcon /></div>
            </div>
        </div>
    );
}