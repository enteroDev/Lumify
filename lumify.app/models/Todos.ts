// --- CONSTS --- //

/** Allowed todo status values (1 = pending, 2 = done). */
export const TODO_STATUS = {
    PENDING: 1,
    DONE: 2,
} as const;



// --- UNIONS --- //

/** Union of the valid {@link TODO_STATUS} values. */
export type TodoStatus = typeof TODO_STATUS[keyof typeof TODO_STATUS];



// --- DTOs --- //

/** A todo list as delivered to the client. Mirrors the backend `TodoListResponse`. */
export type TodoListDTO = {
    /** List ID. */
    id: string;
    /** The owner's user ID. */
    ownerID: string;
    /** The owning workspace, or `null` for a personal list. */
    workspaceID: string | null;

    /** List name. */
    name: string;
    /** List status (see {@link TodoStatus}). */
    status: TodoStatus;
    /** Archived flag as an integer (0 = active, 1 = archived). */
    isArchived: number;

    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
};

/** A todo entry as delivered to the client. Mirrors the backend `TodoEntryResponse`. */
export type TodoEntryDTO = {
    /** Entry ID. */
    id: string;
    /** The parent todo list. */
    todoListID: string;
    /** The owner's user ID. */
    ownerID: string;

    /** Entry title. */
    name: string;
    /** Optional description. */
    description?: string | null;
    /** Entry status (see {@link TodoStatus}). */
    status: TodoStatus;

    /** Frontend-only flag: true if this update was the last open entry being checked (list became done). */
    wasLastUnchecked?: boolean;

    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
};
