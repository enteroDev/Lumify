"use client"

// --------------- //
// --- Imports --- //
// --------------- //

// React & Microsoft
import { useState, useEffect } from "react";
// Components
import SidePanel from "@/components/SidePanel/SidePanel";
// Provider
import { useSpace } from "@/components/_Space/SpaceProvider";
import { useToast } from "../../../../components/Toast/ToastProvider";
import { useAlert } from "../../../../components/AlertModal/AlertProvider";
// Service
import { TodoService } from "@/services/api/todoService";
// Hooks
import { useTodoHub } from "@/hooks/useTodoHub";
// Models
import type { TodoListDTO, TodoEntryDTO } from "@/models/todo";
import { TODO_STATUS } from "@/models/todo";



// ------------------ //
// --- Components --- //
// ------------------ //
export default function Todos() {

    const toast = useToast();
    const { showAlert } = useAlert();

    const [todoLists, setTodoLists] = useState<TodoListDTO[]>([]);
    const [todoEntries, setTodoEntries] = useState<TodoEntryDTO[]>([]);

    const { isPrivate, workspaceID } = useSpace();
    const [editingTodoListID, setEditingTodoListID] = useState<string | null>(null);
    const [editingTodoEntryID, setEditingTodoEntryID] = useState<string | null>(null);



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //

    // LOAD DATA on space-change (same pattern as Notes)
    useEffect(() => {

        let cancelled = false;

        const load = async () => {

            try {

                let lists: TodoListDTO[] = [];
                let entries: TodoEntryDTO[] = [];

                if (isPrivate) {
                    lists = await TodoService.getAllTodoListsOfUser();
                    entries = await TodoService.getAllTodoEntriesOfUser();
                } else {
                    if (!workspaceID) {
                        setTodoLists([]);
                        setTodoEntries([]);
                        return;
                    }

                    lists = await TodoService.getAllTodoListsOfWorkspace(workspaceID);
                    entries = await TodoService.getAllTodoEntriesOfWorkspace(workspaceID);
                }

                if (cancelled) return;

                setTodoLists(lists);
                setTodoEntries(entries);

            } catch (err) {
                console.error("Failed to load todos", err);
            }
        };

        void load();

        return () => { cancelled = true; };

    }, [isPrivate, workspaceID]);



    // ------------- //
    // --- Hooks --- //
    // ------------- //
    useTodoHub({
        isPrivate,
        workspaceID,

        onTodoListCreated: (todoList) => {
            setTodoLists(prev => {
                const exists = prev.some(x => x.id === todoList.id);
                if (exists) { return prev; }

                return [...prev, todoList];
            });
        },

        onTodoListUpdated: (todoList) => {
            setTodoLists(prev => prev.map(x => {
                if (x.id !== todoList.id) { return x; }
                return todoList;
            }));
        },

        onTodoListDeleted: ({ todoListID }) => {
            setTodoLists(prev => prev.filter(x => x.id !== todoListID));
            setTodoEntries(prev => prev.filter(x => x.todoListID !== todoListID));

            setEditingTodoListID(prev => {
                if (prev !== todoListID) { return prev; }
                return null;
            });
        },

        onTodoEntryCreated: (todoEntry) => {
            setTodoEntries(prev => {
                const exists = prev.some(x => x.id === todoEntry.id);
                if (exists) { return prev; }

                return [...prev, todoEntry];
            });
        },

        onTodoEntryUpdated: (todoEntry) => {
            setTodoEntries(prev => prev.map(x => {
                if (x.id !== todoEntry.id) { return x; }
                return todoEntry;
            }));
        },

        onTodoEntryDeleted: ({ todoEntryID }) => {
            setTodoEntries(prev => prev.filter(x => x.id !== todoEntryID));
        },
    });



    // ------------- //
    // --- Logic --- //
    // ------------- //

    /* --- */
    /* ADD */
    /* --- */
    const createDraftTodoList = () => {
        const tempID = Math.random().toString(36).substring(2, 11);
        const draftID = `draft_${tempID}`;

        const draft: TodoListDTO = {
            id: draftID,
            ownerID: "",
            workspaceID: isPrivate ? null : workspaceID,
            name: "",
            status: TODO_STATUS.PENDING,
            isArchived: 0,
            createdAt: "",
            updatedAt: "",
        };

        setTodoLists(prev => [...prev, draft]);
        setEditingTodoListID(draftID);
    };

    const addTodoList = async (todoListID: string, name: string) => {
        const trimmedName = name.trim();

        if (!trimmedName) {
            setTodoLists(prev => prev.filter(x => x.id !== todoListID));
            setEditingTodoListID(null);
            return;
        }

        const draftTodoList = todoLists.find(x => x.id === todoListID);
        if (!draftTodoList) { return; }

        try {
            const createdTodoList = await TodoService.addTodoList({
                name: trimmedName,
                workspaceID: isPrivate ? null : workspaceID,
            });

            setTodoLists(prev => prev.map(x => {
                if (x.id !== todoListID) { return x; }

                return {
                    ...x,
                    id: createdTodoList.id,
                    ownerID: createdTodoList.ownerID,
                    workspaceID: createdTodoList.workspaceID,
                    name: createdTodoList.name,
                    status: createdTodoList.status,
                    isArchived: createdTodoList.isArchived,
                    createdAt: createdTodoList.createdAt,
                    updatedAt: createdTodoList.updatedAt,
                };
            }));

            setEditingTodoListID(null);

            toast.success("Todo-Liste wurde erstellt.");

        } catch (error) {
            console.error("Failed to create todo list", error);
            toast.error("Fehler beim Erstellen der Todo-Liste");
        }
    };

    const createDraftTodoEntry = (todoListID: string) => {
        const tempID = Math.random().toString(36).substring(2, 11);
        const draftID = `draft_${tempID}`;

        const draft: TodoEntryDTO = {
            id: draftID,
            todoListID: todoListID,
            ownerID: "",
            name: "",
            description: "",
            status: TODO_STATUS.PENDING,
            createdAt: "",
            updatedAt: "",
        };

        setTodoEntries(prev => [...prev, draft]);
        setEditingTodoEntryID(draftID);
    };

    const addTodoEntry = async (todoEntryID: string, todoListID: string, name: string, description?: string) => {
        const trimmedName = name.trim();

        if (!trimmedName) {
            setTodoEntries(prev => prev.filter(x => x.id !== todoEntryID));
            setEditingTodoEntryID(null);
            return;
        }

        try {
            const createdTodoEntry = await TodoService.addTodoEntry({
                todoListID: todoListID,
                name: trimmedName,
                description: description?.trim() || null,
            });

            setTodoEntries(prev => prev.map(x => {
                if (x.id !== todoEntryID) { return x; }
                return createdTodoEntry;
            }));

            setEditingTodoEntryID(null);

            toast.success("Todo-Eintrag wurde hinzugefügt.");
        } catch (error) {
            console.error("Failed to add todo entry", error);
            toast.error("Fehler beim Hinzufügen des Todo-Eintrags.");
        }
    };



    /* ---- */
    /* SAVE */
    /* ---- */
    const saveTodoList = async (todoListID: string, name: string) => {
        const trimmedName = name.trim();

        if (!trimmedName) {
            setEditingTodoListID(null);
            return;
        }

        try {
            const savedTodoList = await TodoService.saveTodoList({
                id: todoListID,
                name: trimmedName,
            });

            setTodoLists(prev => prev.map(todoList => {
                if (todoList.id !== todoListID) { return todoList; }

                return {
                    ...todoList,
                    name: savedTodoList.name,
                    status: savedTodoList.status,
                    isArchived: savedTodoList.isArchived,
                    updatedAt: savedTodoList.updatedAt,
                };
            }));

            setEditingTodoListID(null);

            toast.success("Todo-Liste wurde umbenannt.");

        } catch (error) {
            console.error("Failed to save todo list", error);
            toast.error("Fehler beim Speichern der Todo-Liste");
        }
    };

    const saveTodoEntry = async (todoEntry: TodoEntryDTO) => {
        try {

            const oldEntry = todoEntries.find(x => x.id === todoEntry.id);
            const hasStatusChanged = oldEntry?.status !== todoEntry.status;

            // Save TodoEntry in the database
            const savedTodoEntry = await TodoService.saveTodoEntry({
                id: todoEntry.id,
                name: todoEntry.name?.trim(),
                description: todoEntry.description?.trim(),
                status: todoEntry.status,
            });

            // Set new TodoEntries locally
            setTodoEntries(prev => prev.map(todoEntryItem => {
                if (todoEntryItem.id !== todoEntry.id) { return todoEntryItem; }

                return {
                    ...todoEntryItem,
                    name: savedTodoEntry.name,
                    description: savedTodoEntry.description ?? "",
                    status: savedTodoEntry.status,
                    wasLastUnchecked: savedTodoEntry.wasLastUnchecked,
                    updatedAt: savedTodoEntry.updatedAt,
                };
            }));

            // If status (checkbox) got changed, do:
            if (hasStatusChanged) {
                let newTodoListStatus: number | null = null;

                if (savedTodoEntry.wasLastUnchecked) {
                    newTodoListStatus = TODO_STATUS.DONE;
                }

                if (savedTodoEntry.status !== TODO_STATUS.DONE) {
                    newTodoListStatus = TODO_STATUS.PENDING;
                }

                if (newTodoListStatus !== null) {
                    const savedTodoList = await TodoService.saveTodoList({
                        id: savedTodoEntry.todoListID,
                        status: newTodoListStatus,
                    });

                    setTodoLists(prev => prev.map(todoList => {
                        if (todoList.id !== savedTodoList.id) { return todoList; }

                        return {
                            ...todoList,
                            status: savedTodoList.status,
                            updatedAt: savedTodoList.updatedAt,
                        };
                    }));
                }
            }

        } catch (error) {
            console.error("Failed to save todo entry", error);
            toast.error("Fehler beim Speichern des Todo-Eintrags.");
        }
    };


    /* ------ */
    /* DELETE */
    /* ------ */
    const deleteTodoList = async (todoListID: string) => {
        try {
            await TodoService.deleteTodoList(todoListID);

            setTodoLists(prev => prev.filter(x => x.id !== todoListID));

            toast.success("Todoliste wurde gelöscht.");

        } catch (error) {
            console.error("Failed to delete todoList", error);
            toast.error("Fehler beim Löschen der Todoliste");
        }
    };

    const deleteTodoEntry = async (todoEntryID: string) => {
        try {
            await TodoService.deleteTodoEntry(todoEntryID);

            setTodoEntries(prev => prev.filter(x => x.id !== todoEntryID));

            toast.success("Todoeintrag wurde gelöscht.");

        } catch (error) {
            console.error("Failed to delete todoEntry", error);
            toast.error("Fehler beim Löschen des Todoeintrags");
        }
    };



    /* --------- */
    /* EDIT-MODE */
    /* --------- */
    const startEditTodoList = (todoListID: string) => {
        setEditingTodoListID(todoListID);
    };

    const cancelEditTodoList = (todoListID: string) => {
        if (editingTodoListID !== todoListID) { return; }

        const isDraft = todoListID.startsWith("draft_");

        if (isDraft) {
            setTodoLists(prev => prev.filter(x => x.id !== todoListID));
        }

        setEditingTodoListID(null);
    };

    const cancelEditTodoEntry = (todoEntryID: string) => {
        if (editingTodoEntryID !== todoEntryID) { return; }

        const isDraft = todoEntryID.startsWith("draft_");

        if (isDraft) {
            setTodoEntries(prev => prev.filter(x => x.id !== todoEntryID));
        }

        setEditingTodoEntryID(null);
    };



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className="scrollView">
            <div className="content">

            </div>
        </div>
    );
}

