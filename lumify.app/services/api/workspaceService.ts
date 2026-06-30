
import { saveFetch } from "@/services/utils/auth";
import type { WorkspaceDTO, WorkspaceMembersDTO } from "../../models/Space";
import { CONFIG } from "@/app/config/config";

const API_BASE = CONFIG.API.API_BASE;


/**
 * Client for the workspace endpoints (create/rename/delete workspaces, add/remove members, and
 * query workspaces and members). Methods throw on a non-OK response.
 */
export const WorkspaceService = {


    /**
     * Creates a new workspace (the current user becomes its owner).
     * @param data The workspace name.
     * @returns The created workspace.
     */
    async addWorkspace(data: {
        name: string;
    }) {

        const res = await saveFetch(`${API_BASE}/workspace/addWorkspace`, {
            method: "POST",
            body: JSON.stringify({
                name: data.name,
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to add workspace");
        }

        return await res.json();
    },


    /**
     * Adds a user as a member to a workspace (owner only).
     * @param data The target workspace and the user to add.
     * @returns The new member payload.
     */
    async addWorkspaceMember(data: {
        workspaceID: string;
        userID: string;
    }) {

        const res = await saveFetch(`${API_BASE}/workspace/addWorkspaceMember`, {
            method: "POST",
            body: JSON.stringify({
                workspaceID: data.workspaceID,
                userID: data.userID,
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to add workspaceMember");
        }

        return await res.json();
    },


    /**
     * Renames a workspace (owner only).
     * @param data The workspace ID and the new name.
     * @returns The updated workspace.
     */
    async saveWorkspace(data: { id: string; name: string }) {

        const res = await saveFetch(`${API_BASE}/workspace/saveWorkspace`, {
            method: "PATCH",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(data)
        });

        if (!res.ok) {
            const text = await res.text();

            console.error("Failed to save Workspace", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to save Workspace, (${res.status}): ${text}`);
        }

        return await res.json();
    },



    /**
     * Deletes a workspace (owner only).
     * @param workspaceID The workspace to delete.
     * @returns The server confirmation payload.
     */
    async deleteWorkspace(workspaceID: string) {

        const res = await saveFetch(`${API_BASE}/workspace/deleteWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete workspace: " + workspaceID);
        }

        return await res.json();
    },

    /**
     * Removes a member from a workspace (owner only; the owner cannot be removed).
     * @param workspaceID The workspace.
     * @param userID The member to remove.
     * @returns The server response text.
     */
    async removeWorkspaceMember(workspaceID: string, userID: string) {

        const res = await saveFetch(
            `${API_BASE}/workspace/removeWorkspaceMember?workspaceID=${encodeURIComponent(workspaceID)}&userID=${encodeURIComponent(userID)}`,
            {
                method: "DELETE",
            }
        );

        if (!res.ok) {
            const text = await res.text();
            throw new Error(`Failed to remove member (${res.status}): ${text}`);
        }

        return await res.text();
    },



    /**
     * Returns a single workspace by its ID.
     * @param id The workspace ID.
     * @returns The workspace.
     */
    async getWorkspaceWithID(id: string) {

        const res = await fetch(`${API_BASE}/workspace/getWorkspaceWithID?id=${id}`);

        if (!res.ok) {
            throw new Error("Failed to fetch workspace");
        }

        return await res.json();
    },

    /**
     * Returns all workspaces the current user owns or is a member of.
     * @returns The list of workspaces.
     */
    async getWorkspacesOfUser(): Promise<WorkspaceDTO[]> {

        const res = await saveFetch(`${API_BASE}/workspace/GetWorkspacesOfUser`, {
            method: "GET",
        });

        if (!res.ok) {

            const text = await res.text();

            console.error("Workspace fetch failed", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Workspace fetch failed (${res.status}): ${text}`);
        }

        return await res.json() as WorkspaceDTO[];
    },

    /**
     * Returns all members of a workspace with their profile and role.
     * @param workspaceID The workspace.
     * @returns The list of members.
     */
    async getWorkspaceMembers(workspaceID: string): Promise<WorkspaceMembersDTO[]> {

        const res = await saveFetch(`${API_BASE}/workspace/getWorkspaceMembers?id=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!res.ok) {

            const text = await res.text();

            console.error("Failed to fetch members of workspace: " + workspaceID, {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to fetch members of workspace. (${res.status}): ${text}`);
        }

        return await res.json() as WorkspaceMembersDTO[];
    }

};