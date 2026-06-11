// --------------- //
// --- Imports --- //
// --------------- //

// Components
import Pathbar from "./Pathbar/Pathbar";
// Styles
import styles from "./FileViewHeader.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:      styles["container"],
    searchRow:      styles["search-bar"],
    searchInput:    styles["search-input"],
} as const;

export type FileViewHeaderProps = {
    path: string;
    query: string;
    setQuery: (value: string) => void;
    onBack: () => void;
    canGoBack: boolean;
};



// ----------------- //
// --- Component --- //
// ----------------- //

// Query = Search term entered by the user. Used to filter displayed items in the FileView with "SetQuery".
export default function FileViewHeader({ 
    path, 
    query, 
    setQuery, 
    onBack, 
    canGoBack, 
}: FileViewHeaderProps) {


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* Pathbar */}
            <Pathbar path={path} onBack={onBack} canGoBack={canGoBack} />

            {/* SearchRow */}
            <div className={c.searchRow}>

                {/* SearchInput */}
                <input
                    className={c.searchInput}
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    placeholder="Search..."
                />
            </div>
        </div>
    );
}