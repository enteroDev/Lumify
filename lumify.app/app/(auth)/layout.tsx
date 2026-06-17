
// --------------- //
// --- Imports --- //
// --------------- //

// Provider
import ToastProvider from "@/components/Toast/ToastProvider";
import TooltipProvider from "@/components/Tooltip/TooltipProvider";

// Components
import HeaderReduced from "@/components/HeaderReduced/HeaderReduced";


// -------------- //
// --- Layout --- //
// -------------- //
export default function AuthLayout({ children }: { children: React.ReactNode }) {
    return (
        <ToastProvider>
            <TooltipProvider>

                <HeaderReduced />
                <main>{children}</main>

            </TooltipProvider>
        </ToastProvider>
    );
}