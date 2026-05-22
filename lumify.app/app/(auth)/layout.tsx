
// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import MusicProvider from "@/components/_Audio/MusicProvider";
import ToastProvider from "@/components/Toast/ToastProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";

// Components
import Header from "@/components/Header/Header";


// -------------- //
// --- Layout --- //
// -------------- //
export default function AuthLayout({ children }: { children: React.ReactNode }) {
    return (
        <MusicProvider>
            <ToastProvider>
                <AccountModalProvider>
                    <Header />
                    <main>{children}</main>
                </AccountModalProvider>
            </ToastProvider>
        </MusicProvider>
    );
}