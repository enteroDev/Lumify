"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Components
import Dropdown from "@/components/Dropdown/Dropdown";
// Services
import { WorkspaceService } from "@/services/api/workspaceService";
// Utils
import { useSpace, Space } from "@/components/_Space/SpaceProvider";
// Models
import type { DropdownEntry } from "@/models/Dropdown";
// Styles
import styles from "./SpaceSwitcher.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container: styles["container"],
} as const;



// ----------------- //
// --- Component --- //
// ----------------- //
export default function SpaceSwitcher() {
    const { currentSpace, setCurrentSpace } = useSpace();

    // Holds dropdown entries including private + fetched workspaces
    const [entries, setEntries] = useState<DropdownEntry<Space>[]>([
        { value: "private", text: "Privat", payload: { type: "private" } },
    ]);

    useEffect(() => {
        // Fetch workspaces where the current user is owner or member
        async function loadWorkspaces() {
            try {
                const workspaces = await WorkspaceService.getWorkspacesOfUser();

                const workspaceEntries: DropdownEntry<Space>[] = workspaces.map((ws) => ({
                    value: ws.id,
                    text: ws.name ?? "[Unnamed Workspace]",
                    payload: {
                        type: "workspace",
                        workspaceID: ws.id,
                        name: ws.name,
                    },
                }));

                setEntries([
                    { value: "private", text: "Privat", payload: { type: "private" } },
                    ...workspaceEntries,
                ]);
            }
            catch (err) {
                console.error("Failed to load workspaces", err);
            }
        }

        loadWorkspaces();
    }, []);

    const value =
        currentSpace.type === "private"
            ? "private"
            : currentSpace.workspaceID;


    return (
        <div className={c.container}>
            <Dropdown<Space>
                entries={entries}
                value={value}
                spaceType={currentSpace.type}
                onChange={(e) => setCurrentSpace(e.payload!)}
            />
        </div>
    );
}