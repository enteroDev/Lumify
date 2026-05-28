
// --------------- //
// --- Imports --- //
// --------------- //

// Components
import Header from "@/components/Header/Header";
import Friends from "@/components/FriendsOverlay/FriendsOverlay";
import PresenceBridge from "@/services/utils/presenceBridge";

// Providers
import MusicProvider from "@/components/_Audio/MusicProvider";
import ToastProvider from "@/components/Toast/ToastProvider";
import TooltipProvider from "@/components/Tooltip/TooltipProvider";
import AlertProvider from "@/components/AlertModal/AlertProvider";
import SpaceProvider from "@/components/_Space/SpaceProvider";

import ModalProvider from "@/components/Modal/ModalProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";



// -------------- //
// --- Layout --- //
// -------------- //
export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <MusicProvider>

      <ToastProvider>
        <TooltipProvider>
          <AlertProvider>

            <ModalProvider>
              <AccountModalProvider>

                <PresenceBridge>
                  <SpaceProvider>
                    <Header />
                    <Friends />
                    <main>{children}</main>
                  </SpaceProvider>
                </PresenceBridge>

              </AccountModalProvider>
            </ModalProvider>

          </AlertProvider>
        </TooltipProvider>
      </ToastProvider>

    </MusicProvider>

  );
}