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
/**
 * The account-modal API exposed via {@link useAccountModal}. Besides opening/closing the modal it
 * caches the current user's avatar and display name so the header can show them without re-fetching.
 */
export type AccountModalContextValue = {
    /** Opens the account modal. */
    showModal: () => void;
    /** Closes the account modal. */
    closeModal: () => void;
    /** Whether the account modal is currently open. */
    isOpen: boolean;

    /** The current user's avatar URL, or `null` if none/not loaded. */
    avatarUrl: string | null;
    /** Updates the cached avatar URL (e.g. after an upload). */
    setAvatarUrl: (url: string | null) => void;

    /** The current user's display name (falls back to username), or `null`. */
    displayName: string | null;
    /** Updates the cached display name. */
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
/**
 * Hosts the global `AccountModal` and exposes {@link AccountModalContextValue}. On every
 * non-auth route it eagerly loads the current user's avatar, profile and account info so the cached
 * `avatarUrl`/`displayName` are ready for the header. Wrap the app once; consumers call
 * {@link useAccountModal}.
 * @param props Standard React children.
 */
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
/**
 * Accesses the account-modal {@link AccountModalContextValue} (open/close plus cached
 * avatar/display name).
 * @returns The account-modal API.
 * @throws Error if used outside an {@link AccountModalProvider}.
 */
export function useAccountModal() {
    const context = useContext(AccountModalContext);
    if (!context) {
        throw new Error("useAccountModal must be used within AccountModalProvider.");
    }

    return context;
}