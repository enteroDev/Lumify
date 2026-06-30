import { CONFIG } from "@/app/config/config";
import { saveFetch } from "@/services/utils/auth";

import type { Note_TextBlock } from "@/models/Notes";
import type { WorkspaceDTO } from "@/models/Space";


const API_BASE = CONFIG.API.API_BASE;


/**
 * Client for the note endpoints: notes and their modules (text blocks, link items), plus queries.
 * Methods throw on a non-OK response.
 */
export const NoteService = {

    /**
     * Creates a new (empty) note, optionally inside a folder and/or workspace.
     * @param data Note fields (name, optional folder and workspace).
     * @returns The created note.
     */
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


    /**
     * Appends a text block module to a note.
     * @param data The parent note, block `type` (0 = text, 1 = code) and content.
     * @returns The created text block.
     */
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

    /**
     * Appends a link item module (labelled URL) to a note.
     * @param data The parent note, optional label and the URL.
     * @returns The created link item.
     */
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



    /**
     * Updates a note (only provided fields change; an empty folder moves it to the root).
     * @param data The note ID plus the fields to change.
     * @returns The updated note.
     */
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

    /**
     * Updates a text block (content, type, position, collapsed state, …).
     * @param block The full text block to persist.
     * @returns The updated text block.
     */
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



    /**
     * Deletes a note together with all of its modules.
     * @param noteID The note to delete.
     * @returns The server confirmation payload.
     */
    async deleteNote(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/deleteNote?noteID=${encodeURIComponent(noteID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete Note: " + noteID);
        }

        return await res.json();
    },

    /**
     * Deletes a single text block from its note.
     * @param textblockID The text block to delete.
     * @returns The server confirmation payload.
     */
    async deleteTextBlock(textblockID: string) {

        const res = await saveFetch(`${API_BASE}/notes/deleteTextBlock?textblockID=${encodeURIComponent(textblockID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete TextBlock: " + textblockID);
        }

        return await res.json();
    },

    /**
     * Deletes a single link item from its note.
     * @param linkItemID The link item to delete.
     * @returns The server confirmation payload.
     */
    async deleteLinkItem(linkItemID: string) {

        const res = await saveFetch(`${API_BASE}/notes/deleteLinkItem?linkItemID=${encodeURIComponent(linkItemID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete LinkItem: " + linkItemID);
        }

        return await res.json();
    },



    /**
     * Returns all of the current user's personal notes.
     * @returns The list of notes.
     */
    async getAllNotesOfUser() {

        const res = await saveFetch(`${API_BASE}/notes/getAllNotesOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch notes of user");
        }

        return await res.json();
    },

    /**
     * Returns all notes of a workspace.
     * @param workspaceID The workspace whose notes are requested.
     * @returns The list of notes.
     */
    async getAllNotesOfWorkspace(workspaceID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getAllNotesOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch notes of workspace");
        }

        return await res.json();
    },

    /**
     * Returns a single note (metadata only) by its ID.
     * @param noteID The note to look up.
     * @returns The note.
     */
    async getNoteWithID(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getNoteWithID?noteID=${encodeURIComponent(noteID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch note with id: " + noteID);
        }

        return await res.json();
    },

    /**
     * Returns the number of the current user's personal notes.
     * @returns The note count.
     */
    async getNoteCountOfUser(): Promise<number> {
        const response = await saveFetch(`${API_BASE}/notes/getNoteCountOfUser`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch note count of user.");
        }

        return await response.json();
    },

    /**
     * Returns the number of notes in a workspace.
     * @param workspaceID The workspace to count notes for.
     * @returns The note count.
     */
    async getNoteCountOfWorkspace(workspaceID: string): Promise<number> {
        const response = await saveFetch(`${API_BASE}/notes/getNoteCountOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch note count of workspace.");
        }

        return await response.json();
    },


    /**
     * Returns all text blocks of a note, in display order.
     * @param noteID The note whose text blocks are requested.
     * @returns The ordered text blocks.
     */
    async getTextBlocksOfNote(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getTextBlocksOfNote?noteID=${encodeURIComponent(noteID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch text blocks of note with id: " + noteID);
        }

        return await res.json();
    },

    /**
     * Returns all link items of a note, in display order.
     * @param noteID The note whose link items are requested.
     * @returns The ordered link items.
     */
    async getLinkItemsOfNote(noteID: string) {

        const res = await saveFetch(`${API_BASE}/notes/getLinkItemsOfNote?noteID=${encodeURIComponent(noteID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch link items of note with id: " + noteID);
        }

        return await res.json();
    },

    /**
     * Returns the workspace a note belongs to.
     * @param noteID The note whose workspace is requested.
     * @returns The workspace.
     */
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