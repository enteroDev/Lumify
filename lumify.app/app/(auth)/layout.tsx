
// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import MusicProvider from "@/components/_Audio/MusicProvider";
import ToastProvider from "@/components/Toast/ToastProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";

// Components
import HeaderReduced from "@/components/HeaderReduced/HeaderReduced";


// -------------- //
// --- Layout --- //
// -------------- //
export default function AuthLayout({ children }: { children: React.ReactNode }) {
    return (
        <MusicProvider>
            <ToastProvider>
                <AccountModalProvider>

                    <HeaderReduced />
                    <main>{children}</main>

                </AccountModalProvider>
            </ToastProvider>
        </MusicProvider>
    );
}