// Components
import Header from "@/components/Header/Header";
import Drawer from "@/components/Drawer/Drawer";
import Friends from "@/components/FriendsOverlay/FriendsOverlay";
import PresenceBridge from "@/services/utils/presenceBridge";

// Providers
import MusicProvider from "@/components/_Audio/MusicProvider";
import ToastProvider from "@/components/Toast/ToastProvider";
import TooltipProvider from "@/components/Tooltip/TooltipProvider";
import AlertProvider from "@/components/AlertModal/AlertProvider";
import AccountModalProvider from "@/components/AccountModal/AccountModalProvider";
import ModalProvider from "@/components/Modal/ModalProvider";
import SpaceProvider from "@/components/_Space/SpaceProvider";




export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <ToastProvider>
      <TooltipProvider>
        <AlertProvider>
          <MusicProvider>
            <ModalProvider>
              <AccountModalProvider>

                <PresenceBridge>
                  <SpaceProvider>
                    <Header />
                    <Drawer />
                    <Friends />
                    <main>{children}</main>
                  </SpaceProvider>
                </PresenceBridge>


              </AccountModalProvider>
            </ModalProvider>
          </MusicProvider>
        </AlertProvider>
      </TooltipProvider>
    </ToastProvider>
  );
}