"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Components
import FeatureSelection from "./components/FeatureSelection/FeatureSelection";
import SpaceHubTitle from "./components/SpaceHubTitle/SpaceHubTitle";
// Provider
import { useSpace } from "@/components/_Space/SpaceProvider";
import { useToast } from "@/components/Toast/ToastProvider";
// Services
import { NoteService } from "@/services/api/noteService";
import { TodoService } from "@/services/api/todoService";
import { EventService } from "@/services/api/eventService";




// ----------------- //
// --- Component --- //
// ----------------- //
export default function SpaceHub() {

    const { isPrivate, workspaceID } = useSpace();
    const toast = useToast();



    // -------------- //
    // --- States --- //
    // -------------- //
    const [noteCount, setNoteCount] = useState(0);
    const [todoListCount, setTodoListCount] = useState(0);
    const [eventCount, setEventCount] = useState(0);



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {

        let cancelled = false;

        const loadCounts = async () => {
            try {

                let notes = 0;
                let todos = 0;
                let events = 0;

                if (isPrivate) {
                    [notes, todos, events] = await Promise.all([
                        NoteService.getNoteCountOfUser(),
                        TodoService.getTodoListCountOfUser(),
                        EventService.getEventCountOfUser(),
                    ]);
                }
                else if (workspaceID) {
                    [notes, todos, events] = await Promise.all([
                        NoteService.getNoteCountOfWorkspace(workspaceID),
                        TodoService.getTodoListCountOfWorkspace(workspaceID),
                        EventService.getEventCountOfWorkspace(workspaceID),
                    ]);
                }

                if (cancelled) { return; }

                setNoteCount(notes);
                setTodoListCount(todos);
                setEventCount(events);

            } catch (error) {
                if (cancelled) { return; }

                console.error("Failed to load space hub counts", error);
                toast.error("Fehler beim Laden der Space-Daten.");
            }
        };

        void loadCounts();

        return () => { cancelled = true; };

    }, [isPrivate, workspaceID, toast]);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className="scrollView">
            <div className="bg-overlay" aria-hidden="true" />

            <div className="content-fullHeightAlt">
                <div className="spaceHub-headingArea">
                    <SpaceHubTitle />
                </div>

                <div className="spacer-h-20"></div>

                <div className="featureSelectionArea">
                    <FeatureSelection
                        noteCount={noteCount}
                        todoListCount={todoListCount}
                        eventCount={eventCount}
                    />
                </div>
            </div>
        </div>
    );
}