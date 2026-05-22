// Components
import Header from "@/components/Header/Header";

// Providers
import MusicProvider from "@/components/_Audio/MusicProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";




export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <MusicProvider>
        <AccountModalProvider>
          <Header />
          <main>{children}</main>
        </AccountModalProvider>
      </MusicProvider>
    </>
  );
}