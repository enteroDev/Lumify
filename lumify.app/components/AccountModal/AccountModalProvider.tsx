"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useCallback, useContext, useMemo, useState, useEffect } from "react";
import { usePathname } from "next/navigation";
// Components
import AccountModal from "./AccountModal";
// Service
import { UserService } from "@/services/api/userService";


// ------------------- //
// --- Types/Props --- //
// ------------------- //
type AccountModalContextValue = {
    showModal: () => void;
    closeModal: () => void;
    isOpen: boolean;

    avatarUrl: string | null;
    setAvatarUrl: (url: string | null) => void;

    displayName: string | null;
    setDisplayName: (name: string | null) => void;
};

type AccountModalState = {
    isOpen: boolean;
};

type AccountModalProps = {
    children: React.ReactNode;
};

const AccountModalContext = createContext<AccountModalContextValue | null>(null);



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AccountModalProvider({ children }: AccountModalProps) {

    const pathname = usePathname();

    const [modalState, setModalState] = useState<AccountModalState | null>(null);
    const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
    const [displayName, setDisplayName] = useState<string | null>(null);



    // -------------- //
    // --- States --- //
    // -------------- //
    const closeModal = useCallback(() => {
        setModalState(null);
    }, []);

    const showModal = useCallback(() => {
        setModalState({
            isOpen: true,
        });
    }, []);



    // ------------ //
    // --- Memo --- //
    // ------------ //
    const value = useMemo(() => ({
        showModal,
        closeModal,
        isOpen: !!modalState?.isOpen,
        avatarUrl,
        setAvatarUrl,
        displayName,
        setDisplayName,
    }), [showModal, closeModal, modalState?.isOpen, avatarUrl, displayName]);



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        async function loadAccountModalData() {
            try {
                const isAuthPage = pathname?.toLowerCase().startsWith("/auth");
                if (isAuthPage) { return; }

                const [avatarUrl, profile, accountInfo] = await Promise.all([
                    UserService.getAvatarOfUser(),
                    UserService.getUserProfile(),
                    UserService.getUserAccountInfo(),
                ]);

                setAvatarUrl(avatarUrl || null);
                setDisplayName(profile?.displayName || accountInfo?.username || null);
            } catch (err) {
                console.warn("AccountModal data could not be loaded.", err);
            }
        }

        void loadAccountModalData();
    }, [pathname]);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <AccountModalContext.Provider value={value}>
            {children}
            <AccountModal />
        </AccountModalContext.Provider>
    );
}



// ------------ //
// --- Hook --- //
// ------------ //
export function useAccountModal() {
    const context = useContext(AccountModalContext);
    if (!context) {
        throw new Error("useAccountModal must be used within AccountModalProvider.");
    }

    return context;
}