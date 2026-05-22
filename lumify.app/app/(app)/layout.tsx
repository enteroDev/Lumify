
// --------------- //
// --- Imports --- //
// --------------- //

// Components
import Header from "@/components/Header/Header";

// Providers
import MusicProvider from "@/components/_Audio/MusicProvider";
import ToastProvider from "@/components/Toast/ToastProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";



// -------------- //
// --- Layout --- //
// -------------- //
export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <ToastProvider>
        <MusicProvider>
          <AccountModalProvider>
            <Header />
            <main>{children}</main>
          </AccountModalProvider>
        </MusicProvider>
      </ToastProvider>
    </>
  );
}