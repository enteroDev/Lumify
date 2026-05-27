
// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import ToastProvider from "@/components/Toast/ToastProvider";
import TooltipProvider from "@/components/Tooltip/TooltipProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";

// Components
import HeaderReduced from "@/components/HeaderReduced/HeaderReduced";


// -------------- //
// --- Layout --- //
// -------------- //
export default function AuthLayout({ children }: { children: React.ReactNode }) {
    return (
        <ToastProvider>
            <TooltipProvider>
                <AccountModalProvider>

                    <HeaderReduced />
                    <main>{children}</main>

                </AccountModalProvider>
            </TooltipProvider>
        </ToastProvider>
    );
}