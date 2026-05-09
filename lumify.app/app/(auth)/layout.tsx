// Provider
import ToastProvider from "@/components/Toast/ToastProvider";
import TooltipProvider from "@/components/Tooltip/TooltipProvider";
import AlertProvider from "@/components/AlertModal/AlertProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";
import MusicProvider from "@/components/_Audio/MusicProvider";

// Components
import Header from "@/components/Header/Header";



export default function AuthLayout({ children }: { children: React.ReactNode }) {
    return (
        <ToastProvider>
            <TooltipProvider>
                <AlertProvider>
                    <AccountModalProvider>
                        <MusicProvider>
                            <Header />
                            <main>{children}</main>
                        </MusicProvider>
                    </AccountModalProvider>
                </AlertProvider>
            </TooltipProvider>
        </ToastProvider>
    );
}