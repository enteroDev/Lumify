"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState, useEffect } from "react";
// Services
import { WorkspaceService } from "@/services/api/workspaceService";
// Provider
import { useToast } from "@/components/Toast/ToastProvider";
import { useAlert } from "@/components/AlertModal/AlertProvider";
// Components
import Header from "./components/Header/Header";
// Models
import type { WorkspaceVM, WorkspaceMembersDTO } from "@/models/Space";
// Styles
import styles from "./WorkspaceModal.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    overlay:            styles["overlay"],
    modal:              styles["modal"],

    header:             styles["header"],
    title:              styles["title"],

    body:               styles["body"],

    footer:             styles["footer"],
} as const;

export type WorkspaceModalProps = {
    workspace: WorkspaceVM;
    onClose: () => void;
    onWorkspaceSaved?: (updatedWorkspace: WorkspaceVM) => void;
    onMemberCountChanged?: (workspaceID: string, memberCount: number) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function WorkspaceModal({
    workspace,
    onClose,
    onWorkspaceSaved,
    onMemberCountChanged,
}: WorkspaceModalProps) {

    const toast = useToast();
    const { showAlert } = useAlert();

    const [name, setName] = useState(workspace.name ?? "");
    const [members, setMembers] = useState<WorkspaceMembersDTO[]>([]);



    // ------------- //
    // --- Logic --- //
    // ------------- //
    const saveWorkspace = async () => {
        const trimmedName = name.trim();
        if (!trimmedName) { return; }

        try {
            await WorkspaceService.saveWorkspace({
                id: workspace.id,
                name: trimmedName,
            });

            const updatedWorkspace: WorkspaceVM = {
                ...workspace,
                name: trimmedName,
            };

            setName(trimmedName);
            onWorkspaceSaved?.(updatedWorkspace);

        } catch (error) {
            console.error("Failed to save workspace", error);
        }
    };

    const removeWorkspaceMember = (userID: string) => {
        if (!userID.trim()) {
            console.error("No userID was given");
            return;
        }

        const member = members.find(x => x.userID === userID);
        const memberName = member?.displayName || member?.username || userID;

        showAlert({
            title: "Mitglied entfernen",
            message: `"${memberName}" wirklich aus dem Workspace entfernen?`,
            status: "delete",
            confirmText: "Ja",
            cancelText: "Nein",
            onConfirm: async () => {
                try {
                    await WorkspaceService.removeWorkspaceMember(workspace.id, userID);

                    const updatedMembers = members.filter(x => x.userID !== userID);
                    setMembers(updatedMembers);
                    onMemberCountChanged?.(workspace.id, updatedMembers.length);

                    toast.success("User erfolgreich aus Workspace entfernt.");

                } catch (error) {
                    console.error("Failed to remove member", error);
                    toast.error("Fehler beim Entfernen des Users.");
                }
            }
        });
    };

    const loadMembers = async () => {
        try {
            const data = await WorkspaceService.getWorkspaceMembers(workspace.id);
            setMembers(data);

        } catch (error) {
            console.error("Failed to load workspace members", error);
        }
    };



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //

    // Load members initially
    useEffect(() => {
        void loadMembers();
    }, []);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.overlay} onClick={onClose}>
            <div className={c.modal} onClick={(e) => e.stopPropagation()}>

                {/* HEADER */}
                <div className={c.header}>
                    <Header
                        workspace={workspace}
                        onClose={onClose}
                        onSaveWorkspace={saveWorkspace}
                    />
                </div>
                <div className="spacer-h-20"></div>


                {/* BODY */}
                <div className={c.body}>

                </div>


                {/* FOOTER */}
                <div className={c.footer}>

                </div>
            </div>
        </div>
    );
}