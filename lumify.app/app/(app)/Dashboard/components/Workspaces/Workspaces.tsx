"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
// Services
import { WorkspaceService } from "@/services/api/workspaceService";
import { UserService } from "@/services/api/userService";
// Components
import PrivatespaceCard from "./components/PrivatespaceCard/PrivatespaceCard";
import WorkspaceCard from "./components/WorkspaceCard/WorkspaceCard";
// Providers
import { useAccountModal } from "@/components/AccountModal/AccountModalProvider";
import { useToast } from "@/components/Toast/ToastProvider";
// Models
import type { WorkspaceDTO, WorkspaceVM } from "@/models/Space";
// Icons
import AddIcon from "@/app/src/svg/add.svg";
// Styles
import styles from "./Workspaces.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container: styles["container"],
    header: styles["header"],
    body: styles["body"],
    addCard: styles["addCard"],
    addCardIcon: styles["addCardIcon"],
} as const;


// ----------------- //
// --- Component --- //
// ----------------- //
export default function Workspaces() {

    const router = useRouter();
    const toast = useToast();

    const { displayName } = useAccountModal();

    const [editingID, setEditingID] = useState<string | null>(null);
    const [currentUserID, setCurrentUserID] = useState<string | null>(null);
    const [workspaces, setWorkspaces] = useState<WorkspaceVM[]>([]);



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    const openPrivateSpace = () => {
        router.push("/SpaceHub");
    };

    const openPrivateTodos = () => {
        router.push("/Todos");
    };

    const openPrivateEvents = () => {
        router.push("/Events");
    };

    const openPrivateNotes = () => {
        router.push("/Notes");
    };


    const openWorkspace = () => {
        router.push("/SpaceHub");
    };

    const openWorkspaceTodos = () => {
        router.push("/Todos");
    };

    const openWorkspaceEvents = () => {
        router.push("/Events");
    };

    const openWorkspaceNotes = () => {
        router.push("/Notes");
    };

    // Updates name / ownerName
    const handleWorkspaceSaved = (updatedWorkspace: WorkspaceVM) => {
        updateWorkspace(updatedWorkspace.id, {
            name: updatedWorkspace.name,
            ownerName: updatedWorkspace.ownerName,
        });
    };

    // Updates memberCount
    const handleMemberCountChanged = (workspaceID: string, memberCount: number) => {
        updateWorkspace(workspaceID, {
            memberCount,
        });
    };



    // ------------- //
    // --- Logic --- //
    // ------------- //

    /* ------ */
    /* UPDATE */
    /* ------ */
    const updateWorkspace = (workspaceID: string, patch: Partial<WorkspaceVM>) => {
        setWorkspaces(prev => prev.map(ws => {
            if (ws.id !== workspaceID) { return ws; }

            return {
                ...ws,
                ...patch,
            };
        }));
    };


    /* --- */
    /* ADD */
    /* --- */

    // Create draft
    const createDraftWorkspace = () => {
        if (editingID) { return; }

        const tempID = Math.random().toString(36).substring(2, 11);
        const draftID = `draft_${tempID}`;

        const draftWorkspace: WorkspaceVM = {
            id: draftID,
            name: "",
            ownerID: "",
            ownerName: "You",
            memberCount: 0,
        };

        setWorkspaces(prev => [...prev, draftWorkspace]);
        setEditingID(draftID);
    };

    // Add workspace to database
    const addWorkspace = async (workspaceID: string, name: string) => {
        const trimmedName = name.trim();

        if (!trimmedName) {
            setWorkspaces(prev => prev.filter(x => x.id !== workspaceID));
            setEditingID(null);
            return;
        }

        const draftWorkspace = workspaces.find(x => x.id === workspaceID);
        if (!draftWorkspace) { return; }

        try {
            const createdWorkspace = await WorkspaceService.addWorkspace({
                name: trimmedName,
            });

            const owner = await UserService.getUserProfileWithID(createdWorkspace.ownerID);

            setWorkspaces(prev => prev.map(x => {
                if (x.id !== workspaceID) { return x; }

                return {
                    id: createdWorkspace.id,
                    name: createdWorkspace.name ?? "[Unbenannter Space]",
                    ownerID: createdWorkspace.ownerID,
                    ownerName: owner.displayName ?? createdWorkspace.ownerID,
                    memberCount: 0,
                } satisfies WorkspaceVM;
            }));

            setEditingID(null);
            toast.success("Workspace wurde erstellt.");
        } catch (error) {
            console.error("Failed to create workspace", error);
            toast.error("Fehler beim Erstellen des Workspaces.");
        }
    };

    // Chancel adding workspace -> remove draft
    const cancelAddWorkspace = (workspaceID: string) => {
        const isDraft = workspaceID.startsWith("draft_");

        if (isDraft) {
            setWorkspaces(prev => prev.filter(x => x.id !== workspaceID));
        }

        setEditingID(null);
    };


    /* ----------- */
    /* EDIT / SAVE */
    /* ----------- */
    const editWorkspace = (workspaceID: string) => {
        const workspace = workspaces.find(x => x.id === workspaceID);
        if (!workspace) { return; }

        handleWorkspaceSaved(workspace);
        handleMemberCountChanged(workspaceID, workspace.memberCount);
    };


    /* ------ */
    /* DELETE */
    /* ------ */
    const deleteWorkspace = async (workspaceID: string) => {
        try {
            await WorkspaceService.deleteWorkspace(workspaceID);

            setWorkspaces(prev => prev.filter(x => x.id !== workspaceID));
            toast.success("Workspace wurde gelöscht.");

        } catch (error) {
            console.error("Failed to delete workspace", error);
            toast.error("Fehler beim Löschen des Workspaces.");
        }
    };


    /* ---------- */
    /* GET / LOAD */
    /* ---------- */
    const getWorkspaceMembers = async (workspaceID: string) => {
        try {
            const members = await WorkspaceService.getWorkspaceMembers(workspaceID);
            return members;

        } catch (error) {
            console.error("Failed to load workspace members", error);
            toast.error("Fehler beim Laden der Workspace-Mitglieder.");
            return [];
        }
    };

    const loadWorkspaces = async () => {
        try {
            const currentUser = await UserService.getUserAccountInfo();
            const data = await WorkspaceService.getWorkspacesOfUser();

            const workspaceCards = await Promise.all(
                data.map(async (ws: WorkspaceDTO) => {
                    const owner = await UserService.getUserProfileWithID(ws.ownerID);
                    const members = await getWorkspaceMembers(ws.id);

                    return {
                        id: ws.id,
                        name: ws.name ?? "[Unbenannter Space]",
                        ownerID: ws.ownerID,
                        ownerName: owner.displayName ?? ws.ownerID,
                        memberCount: members.length,
                    } satisfies WorkspaceVM;
                })
            );

            setCurrentUserID(currentUser.id);
            setWorkspaces(workspaceCards);
        } catch (error) {
            console.error("Failed to load workspaces", error);
            toast.error("Fehler beim Laden der Workspaces.");
        }
    };



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        void loadWorkspaces();
    }, []);

    useEffect(() => {
        if (!currentUserID) { return; }
        if (!displayName) { return; }

        setWorkspaces(prev => prev.map(ws => {
            if (ws.ownerID !== currentUserID) { return ws; }

            return {
                ...ws,
                ownerName: displayName,
            };
        }));
    }, [displayName, currentUserID]);




    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            <div className={c.header}>Deine Spaces</div>

            <div className={c.body}>
                <PrivatespaceCard
                    name="Privat"
                    ownerName="You"
                    onOpenSpace={openPrivateSpace}
                    onOpenTodos={openPrivateTodos}
                    onOpenEvents={openPrivateEvents}
                    onOpenNotes={openPrivateNotes}
                />

                {workspaces.map(ws => (
                    <WorkspaceCard
                        key={ws.id}
                        workspaceID={ws.id}
                        name={ws.name}

                        ownerName={ws.ownerName}
                        memberCount={ws.memberCount}
                        isEditing={editingID === ws.id}
                        currentUserIsOwner={ws.ownerID === currentUserID}

                        onAddWorkspace={addWorkspace}
                        onCancelAddWorkspace={cancelAddWorkspace}
                        onDeleteWorkspace={deleteWorkspace}
                        onEditWorkspace={editWorkspace}

                        onOpenSpace={() => openWorkspace()}
                        onOpenTodos={() => openWorkspaceTodos()}
                        onOpenEvents={() => openWorkspaceEvents()}
                        onOpenNotes={() => openWorkspaceNotes()}
                    />
                ))}

                <div
                    className={c.addCard}
                    title="Space hinzufügen"
                    onClick={() => {
                        if (editingID) { return; }
                        createDraftWorkspace();
                    }}
                >
                    <div className={c.addCardIcon}>
                        <AddIcon />
                    </div>
                </div>
            </div>
        </div>
    );
}