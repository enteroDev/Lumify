// Components
import Header from "@/components/Header/Header";

// Providers
import MusicProvider from "@/components/_Audio/MusicProvider";




export default function AppLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <MusicProvider>
        <Header />
        <main>{children}</main>
      </MusicProvider>
    </>
  );
}