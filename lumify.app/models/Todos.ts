// --- CONSTS --- //
export const TODO_STATUS = {
    PENDING: 1,
    DONE: 2,
} as const;

// Only allow what is given per constant
export type TodoStatus = typeof TODO_STATUS[keyof typeof TODO_STATUS];



// --- DTOs --- //
export type TodoListDTO = {
    id: string;
    ownerID: string;
    workspaceID: string | null;

    name: string;
    status: TodoStatus;
    isArchived: number; // boolean: true or false

    createdAt: string;
    updatedAt: string;
};

export type TodoEntryDTO = {
    id: string;
    todoListID: string;
    ownerID: string;

    name: string;
    description?: string | null;
    status: TodoStatus;

    wasLastUnchecked?: boolean; // Not in DB. Only as info for Frontend. Bool decides API

    createdAt: string;
    updatedAt: string;
};