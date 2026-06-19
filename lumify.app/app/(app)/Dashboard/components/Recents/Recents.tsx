"use client"

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Services
import { UserService } from "@/services/api/userService";
import { TodoService } from "@/services/api/todoService";
import { EventService } from "@/services/api/eventService";
import { NoteService } from "@/services/api/noteService";
// Types
import type { TodoEntryDTO } from "@/models/todo";
import type { CalendarEventDTO } from "@/models/Events";
import type { Note } from "@/models/notes";
// Components
import RecentItem from "./components/RecentItem/RecentItem";
// Icons
import TodoIcon from "@/app/src/svg/todo.svg";
import EventIcon from "@/app/src/svg/calendar.svg";
import NoteIcon from "@/app/src/svg/folder.svg";
// Styles
import styles from "./Recents.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    header:             styles["header"],
    body:               styles["body"],

    group:              styles["group"],
    groupHeader:        styles["groupHeader"],
    groupContent:       styles["groupContent"],
    icon:               styles["icon"],
    title:              styles["title"],
    chevron:            styles["chevron"],

    item:               styles["item"],
} as const;


// ----------------- //
// --- Component --- //
// ----------------- //
export default function Recents() {


    // -------------- //
    // --- States --- //
    // -------------- //
    const [todos, setTodos] = useState<TodoEntryDTO[]>([]);
    const [events, setEvents] = useState<CalendarEventDTO[]>([]);
    const [notes, setNotes] = useState<Note[]>([]);

    // Collapsed state per group - all collapsed initially
    const [todosOpen, setTodosOpen] = useState(false);
    const [eventsOpen, setEventsOpen] = useState(false);
    const [notesOpen, setNotesOpen] = useState(false);



    // ------------- //
    // --- Logic --- //
    // ------------- //
    async function loadRecents() {

        const [todosRes, eventsRes, notesRes] = await Promise.all([
            UserService.getLast5ModifiedTodosOfUser(),
            UserService.getLast5ModifiedEventsOfUser(),
            UserService.getLast5ModifiedNotesOfUser(),
        ]);

        setTodos(todosRes);
        setEvents(eventsRes);
        setNotes(notesRes);
    }


    async function getSpaceInfoOfTodoEntry(todoEntryID: string) {
        const workspace = await TodoService.getSpaceInfosOfTodoEntry(todoEntryID);
        return workspace;
    }

    async function getSpaceInfoOfEvent(eventID: string) {
        const workspace = await EventService.getSpaceInfosOfEvent(eventID);
        return workspace;
    }

    async function getSpaceInfoOfNote(noteID: string) {
        const workspace = await NoteService.getSpaceInfosOfNote(noteID);
        return workspace;
    }



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        loadRecents();
    }, []);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>Aktuelles</div>


            {/* BODY */}
            <div className={c.body}>

                {/* Group */}
                <div className={c.group}>
                    {/* GroupHeader */}
                    <div className={c.groupHeader} onClick={() => setTodosOpen((open) => !open)}>
                        <div className={c.icon}><TodoIcon /></div>
                        <div className={c.title}>Todos:</div>
                        <button type="button" className={c.chevron}>{todosOpen ? "▼" : "▶"}</button>
                    </div>
                    {/* GroupContent */}
                    {todosOpen && (
                        <div className={c.groupContent}>
                            {todos.map((todo) => (
                                <RecentItem
                                    key={todo.id}
                                    todo={todo}
                                    getSpaceInfoOfTodoEntry={getSpaceInfoOfTodoEntry}
                                />
                            ))}
                        </div>
                    )}
                </div>


                {/* Group */}
                <div className={c.group}>
                    {/* GroupHeader */}
                    <div className={c.groupHeader} onClick={() => setEventsOpen((open) => !open)}>
                        <div className={c.icon}><EventIcon /></div>
                        <div className={c.title}>Events:</div>
                        <button type="button" className={c.chevron}>{eventsOpen ? "▼" : "▶"}</button>
                    </div>
                    {/* GroupContent */}
                    {eventsOpen && (
                        <div className={c.groupContent}>
                            {events.map((event) => (
                                <RecentItem
                                    key={event.id}
                                    event={event}
                                    getSpaceInfoOfEvent={getSpaceInfoOfEvent}
                                />
                            ))}
                        </div>
                    )}
                </div>

                {/* Group */}
                <div className={c.group}>
                    {/* GroupHeader */}
                    <div className={c.groupHeader} onClick={() => setNotesOpen((open) => !open)}>
                        <div className={c.icon}><NoteIcon /></div>
                        <div className={c.title}>Notes:</div>
                        <button type="button" className={c.chevron}>{notesOpen ? "▼" : "▶"}</button>
                    </div>
                    {/* GroupContent */}
                    {notesOpen && (
                        <div className={c.groupContent}>
                            {notes.map((note) => (
                                <RecentItem
                                    key={note.id}
                                    note={note}
                                    getSpaceInfoOfNote={getSpaceInfoOfNote}
                                />
                            ))}
                        </div>
                    )}
                </div>

            </div>
        </div>
    );
}