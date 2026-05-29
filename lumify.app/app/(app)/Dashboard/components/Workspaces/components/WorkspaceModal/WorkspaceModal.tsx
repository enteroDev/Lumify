"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState, useEffect } from "react";
// Services
import { WorkspaceService } from "@/services/api/workspaceService";
import { UserService } from "@/services/api/userService";
// Provider
import { useToast } from "@/components/Toast/ToastProvider";
import { useAlert } from "@/components/AlertModal/AlertProvider";
// Components
import Header from "./components/Header/Header";
import ActionBar from "./components/ActionBar/ActionBar";
import MemberList from "./components/MemberList/MemberList";
// Models
import type { WorkspaceVM, WorkspaceMembersDTO } from "@/models/Space";
import type { RelatedUserDTO } from "@/models/User";
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

    titleBar:           styles["titleBar"],
    actionArea:         styles["actionArea"],
    searchBar:          styles["searchBar"],
    input:              styles["input"],
    searchIcon:         styles["searchIcon"],
    button:             styles["button"],

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
    const [relatedUsers, setRelatedUsers] = useState<RelatedUserDTO[]>([]);
    const [isAddMemberOpen, setIsAddMemberOpen] = useState(false);

    const [memberSearchValue, setMemberSearchValue] = useState("");
    const [debouncedMemberSearchValue, setDebouncedMemberSearchValue] = useState("");

    const [userSearchValue, setUserSearchValue] = useState("");
    const [debouncedUserSearchValue, setDebouncedUserSearchValue] = useState("");



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //
    const showAddMemberOverlay = async () => {
        setIsAddMemberOpen(true);

        // Initially load related users of the current user if available.
        if (relatedUsers.length === 0) {
            await getRelatedUsers();
        }
    };

    const closeAddMemberOverlay = () => {
        setIsAddMemberOpen(false);

        // Clear search values
        setUserSearchValue("");
        setDebouncedUserSearchValue("");
    };


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

    const addWorkspaceMember = async (userID: string) => {
        if (!userID.trim()) { return; }

        try {
            await WorkspaceService.addWorkspaceMember({
                workspaceID: workspace.id,
                userID: userID,
            });

            const updatedMembers = await WorkspaceService.getWorkspaceMembers(workspace.id);
            setMembers(updatedMembers);
            onMemberCountChanged?.(workspace.id, updatedMembers.length);

            closeAddMemberOverlay();

            toast.success("User erfolgreich zu Workspace hinzugefügt.");

        } catch (error) {
            toast.error("Fehler beim hinzufügen des Workspaces");
            console.error("Failed to add workspace member", error);
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

    const searchAvailableUsersForWorkspace = async () => {
        if (!workspace.id.trim()) {
            console.error("No workspaceID was given");
            return;
        }

        try {
            const users = await UserService.searchAvailableUsersForWorkspace(workspace.id, debouncedUserSearchValue);
            setRelatedUsers(users);

        } catch (error) {
            console.error("Failed to search available users for workspace", error);
            setRelatedUsers([]);
        }
    };

    const getRelatedUsers = async () => {
        try {
            const relatedUsers = await UserService.getRelatedUsers();
            setRelatedUsers(relatedUsers);

        } catch (error) {
            console.error("Failed to fetch related users", error);
        }
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

    // Debounce memberSearch
    useEffect(() => {
        const timeout = setTimeout(() => {
            setDebouncedMemberSearchValue(memberSearchValue.trim().toLowerCase());
        }, 300);

        return () => clearTimeout(timeout);
    }, [memberSearchValue]);

    // Debounce userSearch
    useEffect(() => {
        const timeout = setTimeout(() => {
            setDebouncedUserSearchValue(userSearchValue.trim().toLowerCase());
        }, 300);

        return () => clearTimeout(timeout);
    }, [userSearchValue]);

    useEffect(() => {
        if (!isAddMemberOpen) {
            return;
        }

        if (!debouncedUserSearchValue) {
            void getRelatedUsers();
            return;
        }

        void searchAvailableUsersForWorkspace();
    }, [debouncedUserSearchValue, isAddMemberOpen]);





    // ---------------- //
    // --- Computed --- //
    // ---------------- //

    // Filter users away that are already in the current Workspace
    const filteredRelatedUsers = relatedUsers.filter(user =>
        !members.some(member => member.userID === user.userID)
    );

    const filteredMembers = members.filter(member => {
        const effectiveName = (member.displayName || member.username || "").toLowerCase();

        if (!debouncedMemberSearchValue) {
            return true;
        }

        return effectiveName.includes(debouncedMemberSearchValue);
    });



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
                    <ActionBar
                        memberSearchValue={memberSearchValue}
                        onMemberSearchChange={setMemberSearchValue}

                        userSearchValue={userSearchValue}
                        onUserSearchChange={setUserSearchValue}

                        isAddMemberOpen={isAddMemberOpen}
                        relatedUsers={filteredRelatedUsers}

                        onShowAddMemberOverlay={showAddMemberOverlay}
                        onCloseAddMemberOverlay={closeAddMemberOverlay}
                        onAddMember={addWorkspaceMember}
                    />
                    <MemberList
                        members={filteredMembers}
                        onRemoveMember={removeWorkspaceMember}
                    />
                </div>


                {/* FOOTER */}
                <div className={c.footer}>

                </div>
            </div>
        </div>
    );
}