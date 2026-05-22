"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { createContext, useContext } from "react";

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

    const value = null;

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