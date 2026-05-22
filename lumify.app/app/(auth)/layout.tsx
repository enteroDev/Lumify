// Provider
import MusicProvider from "@/components/_Audio/MusicProvider";

// Components
import Header from "@/components/Header/Header";



export default function AuthLayout({ children }: { children: React.ReactNode }) {
    return (
        <MusicProvider>
            <Header />
            <main>{children}</main>
        </MusicProvider>
    );
}