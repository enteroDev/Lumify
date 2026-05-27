
// --------------- //
// --- Imports --- //
// --------------- //

// Components
import Header from "@/components/Header/Header";
import PresenceBridge from "@/services/utils/presenceBridge";
import Friends from "@/components/FriendsOverlay/FriendsOverlay";

// Providers
import MusicProvider from "@/components/_Audio/MusicProvider";
import ToastProvider from "@/components/Toast/ToastProvider";
import TooltipProvider from "@/components/Tooltip/TooltipProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";



// -------------- //
// --- Layout --- //
// -------------- //
export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <ToastProvider>
      <TooltipProvider>
        <PresenceBridge>
          <MusicProvider>
            <AccountModalProvider>

              <Header />
              <Friends />
              <main>{children}</main>

            </AccountModalProvider>
          </MusicProvider>
        </PresenceBridge>
      </TooltipProvider>
    </ToastProvider>
  );
}