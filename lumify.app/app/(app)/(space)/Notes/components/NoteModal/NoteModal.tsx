"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useCallback, useState } from "react";
// Styles & Icons
import styles from "./NoteModal.module.css";
// Components
import Header from "./components/Header/Header";
import InfoLine from "./components/InfoLine/Infoline";
import ActionLine from "./components/ActionLine/ActionLine";
import BlockList from "./components/BlockList/BlockList";
import AddLinkModal from "./components/AddLinkModal/AddLinkModal";
// Models
import type { Note, Note_TextBlock, Note_LinkItem } from "@/models/notes";
// Service
import { NoteService } from "@/services/api/noteService";
// Provider
import { useAlert } from "@/components/AlertModal/AlertProvider";
import { useToast } from "@/components/Toast/ToastProvider";
// Hooks
import { useNoteHub } from "@/hooks/useNoteHub";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    overlay:            styles["overlay"],
    modal:              styles["modal"],

    header:             styles["header"],
    headerInfo:         styles["headerInfo"],
    headerWarning:      styles["headerWarning"],
    headerDelete:       styles["headerDelete"],

    iconWrap:           styles["iconWrap"],
    title:              styles["title"],

    body:               styles["body"],
    content:            styles["content"],
} as const;


export type NoteModalProps = {
    initialNote: Note | null;
    initialTextBlocks: Note_TextBlock[];
    initialLinkItems: Note_LinkItem[];

    isOpen: boolean;
    onClose: () => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function NoteModal({
    isOpen,
    initialNote,
    initialTextBlocks,
    initialLinkItems,
    onClose,
}: NoteModalProps) {

    const { showAlert } = useAlert();
    const toast = useToast();


    // -------------- //
    // --- States --- //
    // -------------- //
    const [note, setNote] = useState<Note | null>(initialNote);
    const [textBlocks, setTextBlocks] = useState<Note_TextBlock[]>(initialTextBlocks);
    const [linkItems, setLinkItems] = useState<Note_LinkItem[]>(initialLinkItems);

    const [isAddLinkModalOpen, setIsAddLinkModalOpen] = useState(false);
    const [addLinkInitialLabel, setAddLinkInitialLabel] = useState<string | null>("");
    const [addLinkInitialUrl, setAddLinkInitialUrl] = useState<string | null>("");

    const closeAddLinkModal = useCallback(() => {
        setIsAddLinkModalOpen(false);
        setAddLinkInitialLabel("");
        setAddLinkInitialUrl("");
    }, []);

    const openAddLinkModal = useCallback(() => {
        setIsAddLinkModalOpen(true);
        setAddLinkInitialLabel("");
        setAddLinkInitialUrl("");
    }, []);




    // ------------------ //
    // --- UseEffects --- //
    // ------------------ //

    // HUB-HOOK
    useNoteHub({
        isPrivate: note?.workspaceID == null,
        workspaceID: note?.workspaceID ?? null,

        onNoteUpdated: (updatedNote) => {
            if (!note || updatedNote.id !== note.id) { return; }

            setNote(prev => {
                if (!prev || prev.id !== updatedNote.id) { return prev; }

                return {
                    ...prev,
                    ...updatedNote,
                };
            });
        },

        onNoteDeleted: ({ noteID }) => {
            if (!note || noteID !== note.id) { return; }

            toast.error("Diese Notiz wurde gelöscht.");
            onClose();
        },

        onTextBlockCreated: (textBlock) => {
            if (!note || textBlock.noteID !== note.id) { return; }

            setTextBlocks(prev => {
                const exists = prev.some(x => x.id === textBlock.id);
                if (exists) { return prev; }

                return [...prev, textBlock].sort((a, b) => a.notePos - b.notePos);
            });
        },

        onTextBlockUpdated: (textBlock) => {
            if (!note || textBlock.noteID !== note.id) { return; }

            setTextBlocks(prev =>
                prev
                    .map(x => x.id === textBlock.id ? textBlock : x)
                    .sort((a, b) => a.notePos - b.notePos)
            );
        },

        onTextBlockDeleted: ({ textBlockID }) => {
            setTextBlocks(prev => prev.filter(x => x.id !== textBlockID));
        },

        onLinkItemCreated: (linkItem) => {
            if (!note || linkItem.noteID !== note.id) { return; }

            setLinkItems(prev => {
                const exists = prev.some(x => x.id === linkItem.id);
                if (exists) { return prev; }

                return [...prev, linkItem].sort((a, b) => a.notePos - b.notePos);
            });
        },

        onLinkItemDeleted: ({ linkItemID }) => {
            setLinkItems(prev => prev.filter(x => x.id !== linkItemID));
        },
    });






    // ------------- //
    // --- Logic --- //
    // ------------- //

    // ########### //
    // ### ADD ### //
    // ########### //

    // ADD Textblock
    const addTextBlock = useCallback(async (note: Note) => {
        try {
            const createdBlock = await NoteService.addTextBlock({
                noteID: note.id,
                type: 0,          // 0=text
                name: null,
                content: "",
                codeLanguage: null,
            });

            if (note.workspaceID == null) {
                setTextBlocks(prev => [...prev, createdBlock]);
            }

            toast.success("Textblock wurde hinzugefügt.");
        } catch (error) {
            console.error("Failed to add textBlock", error);
            toast.error("Fehler beim Hinzufügen des Textblocks.");
        }
    }, [toast]);

    // ADD Codeblock
    const addCodeBlock = useCallback(async (note: Note) => {
        try {
            const createdBlock = await NoteService.addTextBlock({
                noteID: note.id,
                type: 1,
                name: null,
                content: "",
                codeLanguage: "typescript",
            });

            if (note.workspaceID == null) {
                setTextBlocks(prev => [...prev, createdBlock]);
            }

            toast.success("Codeblock wurde hinzugefügt.");
        } catch (error) {
            console.error("Failed to add codeBlock", error);
            toast.error("Fehler beim Hinzufügen des Codeblocks.");
        }
    }, [toast]);

    // ADD LinkItem
    const addLinkItem = useCallback(async (label: string | null, url: string) => {
        if (!note) { return; }

        try {
            const createdLinkItem = await NoteService.addLinkItem({
                noteID: note.id,
                label: label,
                url: url
            });

            if (note.workspaceID == null) {
                setLinkItems(prev => [...prev, createdLinkItem]);
            }

            closeAddLinkModal();
            toast.success("Link wurde hinzugefügt.");
        } catch (error) {
            console.error("Failed to add link item", error);
            toast.error("Fehler beim Hinzufügen des Links.");
        }
    }, [note, closeAddLinkModal, toast]);


    // ############ //
    // ### SAVE ### //
    // ############ //

    // SAVE Note
    const saveNote = useCallback(async (noteID: string, name: string) => {
        const trimmedName = name.trim();
        if (!trimmedName) { return; }

        try {
            await NoteService.saveNote({
                id: noteID,
                name: trimmedName
            });

            setNote(prev => {
                if (!prev || prev.id !== noteID) { return prev; }

                return {
                    ...prev,
                    name: trimmedName,
                };
            });

            toast.success("Note wurde gespeichert.");
        } catch {
            toast.error("Fehler beim Speichern der Note.");
        }
    }, [toast]);

    // SAVE Textblock
    const saveTextBlock = useCallback(async (block: Note_TextBlock) => {
        try {
            const savedBlock = await NoteService.saveTextBlock(block);

            setTextBlocks(prev => prev.map(x => {
                if (x.id !== savedBlock.id) { return x; }
                return savedBlock;
            }));

            toast.success("Textblock wurde gespeichert.");
        } catch {
            toast.error("Fehler beim Speichern des Textblocks.");
        }
    }, [toast]);


    // ############## //
    // ### DELETE ### //
    // ############## //

    // DELETE Textblock
    const deleteTextBlock = useCallback(async (block: Note_TextBlock) => {
        showAlert({
            title: "Textblock löschen",
            message: "Willst du den Block wirklich löschen?",
            status: "delete",
            confirmText: "Löschen",
            cancelText: "Abbrechen",

            onConfirm: async () => {
                await NoteService.deleteTextBlock(block.id);

                setTextBlocks(prev => prev.filter(x => x.id !== block.id));

                toast.success("Textblock wurde gelöscht.");
            }
        });
    }, [showAlert, toast]);

    // DELETE LinkItem
    const deleteLinkItem = useCallback(async (linkItem: Note_LinkItem) => {
        showAlert({
            title: "Link löschen",
            message: "Willst du den Link wirklich löschen?",
            status: "delete",
            confirmText: "Löschen",
            cancelText: "Abbrechen",

            onConfirm: async () => {
                await NoteService.deleteLinkItem(linkItem.id);

                setLinkItems(prev => prev.filter(x => x.id !== linkItem.id));

                toast.success("Link wurde gelöscht.");
            }
        });
    }, [showAlert, toast]);


    // ############## //
    // ### OTHERS ### //
    // ############## //

    // TOGGLE Collapse - Also persist collapsed state in database
    const toggleTextBlockCollapse = useCallback(async (block: Note_TextBlock, isCollapsed: boolean) => {
        const updatedBlock: Note_TextBlock = {
            ...block,
            isCollapsed: isCollapsed
        };

        const savedBlock = await NoteService.saveTextBlock(updatedBlock);

        setTextBlocks(prev => prev.map(x => {
            if (x.id !== savedBlock.id) { return x; }
            return savedBlock;
        }));
    }, []);


    if (!isOpen || !note) { return null; }



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <>
            <div className={c.overlay} onClick={onClose}>

                {/* Modal */}
                <div className={c.modal} onClick={(e) => e.stopPropagation()}>

                    {/* HEADER */}
                    <Header
                        note={note}
                        onClose={onClose}
                        onSaveNote={saveNote}
                    />

                    {/* INFOLINE */}
                    <InfoLine
                        note={note}
                    />

                    {/* ACTIONLINE */}
                    <ActionLine
                        note={note}
                        onAddTextBlock={addTextBlock}
                        onAddCodeBlock={addCodeBlock}
                        onAddLinkItem={openAddLinkModal}
                    />

                    {/* BODY */}
                    <div className={c.body}>
                        <BlockList
                            textBlocks={textBlocks}
                            linkItems={linkItems}
                            onSaveTextBlock={saveTextBlock}
                            onToggleTextBlockCollapse={toggleTextBlockCollapse}
                            onDeleteTextBlock={deleteTextBlock}
                            onDeleteLinkItem={deleteLinkItem}
                        />
                    </div>

                </div>
            </div>

            {/* AddLinkModal */}
            <AddLinkModal
                isOpen={isAddLinkModalOpen}
                initialLabel={addLinkInitialLabel}
                initialUrl={addLinkInitialUrl}
                onClose={closeAddLinkModal}
                onSubmit={addLinkItem}
            />
        </>
    );
}