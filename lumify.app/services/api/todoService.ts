
import { CONFIG } from "@/app/config/config";
import { saveFetch } from "@/services/utils/auth";
import type { TodoListDTO, TodoEntryDTO } from "@/models/Todos";
import type { WorkspaceDTO } from "@/models/Space";


const API_BASE = CONFIG.API.API_BASE;

/**
 * Client for the todo endpoints: todo lists and their entries, plus queries. Methods throw on a
 * non-OK response.
 */
export const TodoService = {

    /**
     * Creates a new todo list, optionally inside a workspace.
     * @param data List name and optional workspace.
     * @returns The created list.
     */
    async addTodoList(data: { name: string; workspaceID: string | null }): Promise<TodoListDTO> {
        const res = await saveFetch(`${API_BASE}/todos/addTodoList`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                name: data.name,
                workspaceID: data.workspaceID,
            }),
        });

        if (!res.ok) {
            const text = await res.text();

            console.error("Failed to add todo list", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to add todo list (${res.status}): ${text}`);
        }

        return await res.json();
    },

    /**
     * Adds an entry to a todo list.
     * @param data Entry name, parent list ID and optional description.
     * @returns The created entry.
     */
    async addTodoEntry(data: { name: string; todoListID: string; description: string | null }): Promise<TodoEntryDTO> {
        const res = await saveFetch(`${API_BASE}/todos/addTodoEntry`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                todoListID: data.todoListID,
                name: data.name,
                description: data.description || null,
            }),
        });

        if (!res.ok) {
            const text = await res.text();

            console.error("Failed to add todo entry", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to add todo entry (${res.status}): ${text}`);
        }

        return await res.json();
    },


    /**
     * Updates a todo list (only provided fields change).
     * @param data The list ID plus the fields to change (name, status, archived).
     * @returns The updated list.
     */
    async saveTodoList(data: { id: string; name?: string; status?: number; isArchived?: boolean }): Promise<TodoListDTO> {
        const res = await saveFetch(`${API_BASE}/todos/saveTodoList`, {
            method: "PATCH",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                id: data.id,
                name: data.name,
                status: data.status,
                isArchived: data.isArchived,
            }),
        });

        if (!res.ok) {
            const text = await res.text();

            console.error("Failed to save todo list", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to save todo list (${res.status}): ${text}`);
        }

        return await res.json();
    },

    /**
     * Updates a todo entry (only provided fields change). Toggling the status may flip the
     * parent list's status.
     * @param data The entry ID plus the fields to change (name, description, status).
     * @returns The updated entry.
     */
    async saveTodoEntry(data: { id: string; name?: string; description?: string; status?: number; }): Promise<TodoEntryDTO> {
        const res = await saveFetch(`${API_BASE}/todos/saveTodoEntry`, {
            method: "PATCH",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                id: data.id,
                name: data.name,
                description: data.description,
                status: data.status,
            }),
        });

        if (!res.ok) {
            const text = await res.text();

            console.error("Failed to save todo entry", {
                status: res.status,
                statusText: res.statusText,
                body: text
            });

            throw new Error(`Failed to save todo entry (${res.status}): ${text}`);
        }

        return await res.json();
    },


    /**
     * Deletes a todo list.
     * @param todoListID The list to delete.
     * @returns The server confirmation payload.
     */
    async deleteTodoList(todoListID: string) {

        const res = await saveFetch(`${API_BASE}/todos/deleteTodoList?todoListID=${encodeURIComponent(todoListID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete TodoList: " + todoListID);
        }

        return await res.json();
    },

    /**
     * Deletes a todo entry.
     * @param todoEntryID The entry to delete.
     * @returns The server confirmation payload.
     */
    async deleteTodoEntry(todoEntryID: string) {

        const res = await saveFetch(`${API_BASE}/todos/deleteTodoEntry?todoEntryID=${encodeURIComponent(todoEntryID)}`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete TodoEntry: " + todoEntryID);
        }

        return await res.json();
    },


    // --- GET --- //

    /**
     * Returns all of the current user's personal todo lists.
     * @returns The list of todo lists.
     */
    async getAllTodoListsOfUser() {

        const res = await saveFetch(`${API_BASE}/todos/getAllTodoListsOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch todo lists of user");
        }

        return await res.json();
    },

    /**
     * Returns all todo lists of a workspace.
     * @param workspaceID The workspace whose lists are requested.
     * @returns The list of todo lists.
     */
    async getAllTodoListsOfWorkspace(workspaceID: string) {

        const res = await saveFetch(`${API_BASE}/todos/getAllTodoListsOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch todo lists of workspace");
        }

        return await res.json();
    },

    /**
     * Returns a single todo list by its ID.
     * @param todoListID The list to look up.
     * @returns The todo list.
     */
    async getTodoListWithID(todoListID: string): Promise<TodoListDTO> {
        const res = await saveFetch(`${API_BASE}/todos/getTodoListWithID?todoListID=${encodeURIComponent(todoListID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch todo list with id: " + todoListID);
        }

        return await res.json();
    },



    /**
     * Returns all entries of the current user's personal todo lists.
     * @returns The list of entries.
     */
    async getAllTodoEntriesOfUser() {
        const res = await saveFetch(`${API_BASE}/todos/getAllTodoEntriesOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch todo entries of user");
        }

        return await res.json();
    },

    /**
     * Returns all entries of all todo lists in a workspace.
     * @param workspaceID The workspace whose entries are requested.
     * @returns The list of entries.
     */
    async getAllTodoEntriesOfWorkspace(workspaceID: string) {
        const res = await saveFetch(`${API_BASE}/todos/getAllTodoEntriesOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch todo entries of workspace");
        }

        return await res.json();
    },


    /**
     * Returns the number of the current user's personal todo lists.
     * @returns The list count.
     */
    async getTodoListCountOfUser(): Promise<number> {
        const response = await saveFetch(`${API_BASE}/todos/getTodoListCountOfUser`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch todolist count of user.");
        }

        return await response.json();
    },

    /**
     * Returns the number of todo lists in a workspace.
     * @param workspaceID The workspace to count lists for.
     * @returns The list count.
     */
    async getTodoListCountOfWorkspace(workspaceID: string): Promise<number> {
        const response = await saveFetch(`${API_BASE}/todos/getTodoListCountOfWorkspace?workspaceID=${encodeURIComponent(workspaceID)}`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch todolist count of workspace.");
        }

        return await response.json();
    },


    /**
     * Returns the workspace a todo entry belongs to.
     * @param todoEntryID The entry whose workspace is requested.
     * @returns The workspace.
     */
    async getSpaceInfosOfTodoEntry(todoEntryID: string): Promise<WorkspaceDTO> {
        const res = await saveFetch(`${API_BASE}/todos/getSpaceInfosOfTodoEntry?todoEntryID=${encodeURIComponent(todoEntryID)}`, {
            method: "GET"
        });

        if (!res.ok) {
            throw new Error("Failed to fetch space infos of todo entry: " + todoEntryID);
        }

        return await res.json();
    },

};