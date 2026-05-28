
import { saveFetch } from "@/services/utils/auth";
import { CONFIG } from "@/app/(app)/config/config";

const API_BASE = CONFIG.API.API_BASE;


export const FolderService = {

    // --- ADD --- //
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



     // --- SAVE / EDIT --- //
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



    // --- DELETE --- //
    async deleteFolder(folderID: string) {

        const res = await saveFetch(`${API_BASE}/folders/deleteFolder?folderID=${folderID}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete Folder: " + folderID);
        }

        return res.json();
    },



    // --- GET --- //
    async getAllFoldersOfUser() {

        const res = await saveFetch(`${API_BASE}/folders/getAllFoldersOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch folders of user");
        }

        return res.json();
    },

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