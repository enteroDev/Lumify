import { CONFIG } from "@/app/config/config";
import { saveFetch } from "@/services/utils/auth";

import type { Note_TextBlock } from "@/models/Notes";
import type { WorkspaceDTO } from "@/models/Space";


const API_BASE = CONFIG.API.API_BASE;


export const NoteService = {

    // --- Add --- //
    async addNote(data: {
        name: string;
        folderID: string | null;
        workspaceID: string | null;
    }) {

        const res = await saveFetch(`${API_BASE}/notes/addNote`, {
            method: "POST",
            body: JSON.stringify({
                name: data.name,
                folderID: data.folderID,
                workspaceID: data.workspaceID,
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to add note");
        }

        return await res.json();
    },


    async addTextBlock(data: {
        noteID: string;
        type: number;                    // 0=text, 1=code
        name: string | null;
        content: string | null;
        codeLanguage: string | null;
    }) {

        const res = await saveFetch(`${API_BASE}/notes/addTextBlock`, {
            method: "POST",
            body: JSON.stringify({
                noteID: data.noteID,
                type: data.type,
                name: data.name,
                content: data.content,
                codeLanguage: data.codeLanguage,
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to add text block");
        }

        return await res.json();
    },

    async addLinkItem(data: {
        noteID: string;
        label: string | null;
        url: string | null;
    }) {

        const res = await saveFetch(`${API_BASE}/notes/addLinkItem`, {
            method: "POST",
            body: JSON.stringify({
                noteID: data.noteID,
                label: data.label,
                url: data.url,
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to add link item");
        }

        return await res.json();
    },



    // --- SAVE / EDIT --- //
    async saveNote(data: {
        id: string;
        name?: string;
        folderID?: string;
    }) {

        const res = await saveFetch(`${API_BASE}/notes/saveNote`, {
            method: "PATCH",
            body: JSON.stringify({
                id: data.id,
                name: data.name,
                folderID: data.folderID
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to save note");
        }

        const saved = await res.json();
        return saved;
    },

    async saveTextBlock(block: Note_TextBlock) {

        const res = await saveFetch(`${API_BASE}/notes/saveTextBlock`, {
            method: "PATCH",
            body: JSON.stringify({
                id: block.id,
                type: block.type,
                name: block.name ?? null,
                content: block.content ?? null,
                codeLanguage: block.codeLanguage ?? null,
                isCollapsed: block.isCollapsed,
                notePos: block.notePos,
            }),
        });

        if (!res.ok) {
            throw new Error("Failed to save text block");
        }

        const saved = await res.json();

        return saved;
    },



    // --- DELETE --- //
    async deleteNote(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/deleteNote?noteID=${encodeURIComponent(noteID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete Note: " + noteID);
        }

        return await res.json();
    },

    async deleteTextBlock(textblockID: string) {

        const res = await saveFetch(`${API_BASE}/notes/deleteTextBlock?textblockID=${encodeURIComponent(textblockID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete TextBlock: " + textblockID);
        }

        return await res.json();
    },

    async deleteLinkItem(linkItemID: string) {

        const res = await saveFetch(`${API_BASE}/notes/deleteLinkItem?linkItemID=${encodeURIComponent(linkItemID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete LinkItem: " + linkItemID);
        }

        return await res.json();
    },



    // --- GET --- //
    async getAllNotesOfUser() {

        const res = await saveFetch(`${API_BASE}/notes/getAllNotesOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch notes of user");
        }

        return await res.json();
    },

    async getAllNotesOfWorkspace(workspaceID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getAllNotesOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch notes of workspace");
        }

        return await res.json();
    },

    async getNoteWithID(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getNoteWithID?noteID=${encodeURIComponent(noteID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch note with id: " + noteID);
        }

        return await res.json();
    },

    async getNoteCountOfUser(): Promise<number> {
        const response = await saveFetch(`${API_BASE}/notes/getNoteCountOfUser`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch note count of user.");
        }

        return await response.json();
    },

    async getNoteCountOfWorkspace(workspaceID: string): Promise<number> {
        const response = await saveFetch(`${API_BASE}/notes/getNoteCountOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch note count of workspace.");
        }

        return await response.json();
    },


    async getTextBlocksOfNote(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getTextBlocksOfNote?noteID=${encodeURIComponent(noteID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch text blocks of note with id: " + noteID);
        }

        return await res.json();
    },

    async getLinkItemsOfNote(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getLinkItemsOfNote?noteID=${encodeURIComponent(noteID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch link items of note with id: " + noteID);
        }

        return await res.json();
    },

    async getSpaceInfosOfNote(noteID: string): Promise<WorkspaceDTO> {
        const res = await saveFetch(`${API_BASE}/notes/getSpaceInfosOfNote?noteID=${encodeURIComponent(noteID)}`, { 
            method: "GET" 
        });

        if (!res.ok) {
            throw new Error("Failed to fetch space infos of note: " + noteID);
        }

        return await res.json();
    },
};