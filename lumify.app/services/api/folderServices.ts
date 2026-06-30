
import { saveFetch } from "@/services/utils/auth";
import { CONFIG } from "@/app/config/config";

const API_BASE = CONFIG.API.API_BASE;


/**
 * Client for the folder endpoints (create, update, delete, list). Methods throw on a non-OK
 * response.
 */
export const FolderService = {

    /**
     * Creates a new folder, optionally nested and/or inside a workspace.
     * @param name Folder name.
     * @param parentFolderID Parent folder, or `null` for the root.
     * @param workspaceID Target workspace, or `null` for a personal folder.
     * @returns The created folder.
     */
    async addFolder(name: string, parentFolderID: string | null, workspaceID: string | null) {

        const res = await saveFetch(`${API_BASE}/folders/addFolder`, {
            method: "POST",
            body: JSON.stringify({
                name,
                parentFolderID,
                workspaceID: workspaceID || null,
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to add folder");
        }

        return res.json();
    },



    /**
     * Updates a folder (only provided fields are changed; an empty parent moves it to the root).
     * @param data The folder ID plus the fields to change.
     * @returns The updated folder.
     */
    async saveFolder(data: {
        id: string;
        name?: string;
        description?: string;
        parentFolderID?: string;
    }) {

        const res = await saveFetch(`${API_BASE}/folders/saveFolder`, {
            method: "PATCH",
            body: JSON.stringify({
                id: data.id,
                name: data.name,
                description: data.description,
                parentFolderID: data.parentFolderID
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to save folder");
        }

        const saved = await res.json();
        return saved;
    },



    /**
     * Deletes a folder together with its sub-folders and notes (recursive soft-delete on the server).
     * @param folderID The root folder to delete.
     * @returns The server confirmation payload.
     */
    async deleteFolder(folderID: string) {

        const res = await saveFetch(`${API_BASE}/folders/deleteFolder?folderID=${folderID}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete Folder: " + folderID);
        }

        return res.json();
    },



    /**
     * Returns all of the current user's personal folders.
     * @returns The list of folders.
     */
    async getAllFoldersOfUser() {

        const res = await saveFetch(`${API_BASE}/folders/getAllFoldersOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch folders of user");
        }

        return res.json();
    },

    /**
     * Returns all folders of a workspace.
     * @param workspaceID The workspace whose folders are requested.
     * @returns The list of folders.
     */
    async getAllFoldersOfWorkspace(workspaceID: string) {

        const res = await saveFetch(`${API_BASE}/folders/getAllFoldersOfWorkspace?workspaceID=${workspaceID}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch folders of workspace");
        }

        return res.json();
    }
};