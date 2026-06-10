
// LAYOUT: WorkSpace -> Only show SpaeSwitcher in Spaces.
// -> I tried using SpaceProvider in here, but Dashboard needs to access it, therefore i moved it up to the app-layout.

// Components
import SpaceNavigationOverlay from "@/components/SpaceNavigationOverlay/SpaceNavigationOverlay";



export default function SpaceLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <SpaceNavigationOverlay />
      {children}
    </>
  );
}