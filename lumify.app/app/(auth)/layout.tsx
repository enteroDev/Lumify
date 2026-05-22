// Provider
import MusicProvider from "@/components/_Audio/MusicProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";

// Components
import Header from "@/components/Header/Header";



export default function AuthLayout({ children }: { children: React.ReactNode }) {
    return (
        <MusicProvider>
            <AccountModalProvider>
                <Header />
                <main>{children}</main>
            </AccountModalProvider>
        </MusicProvider>
    );
}