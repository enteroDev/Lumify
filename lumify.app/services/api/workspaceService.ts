
import { saveFetch } from "@/services/utils/auth";
import type { WorkspaceDTO, WorkspaceMembersDTO } from "../../models/Space";
import { CONFIG } from "@/app/config/config";

const API_BASE = CONFIG.API.API_BASE;


export const WorkspaceService = {


    // --- Add --- //
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


    // --- SAVE --- //
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



    // --- DELETE --- //
    async deleteWorkspace(workspaceID: string) {

        const res = await saveFetch(`${API_BASE}/workspace/deleteWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete workspace: " + workspaceID);
        }

        return await res.json();
    },

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



    // --- GET --- //
    async getWorkspaceWithID(id: string) {

        const res = await fetch(`${API_BASE}/workspace/getWorkspaceWithID?id=${id}`);

        if (!res.ok) {
            throw new Error("Failed to fetch workspace");
        }

        return await res.json();
    },

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