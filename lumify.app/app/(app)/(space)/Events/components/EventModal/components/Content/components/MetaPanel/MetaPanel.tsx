"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useState } from "react";
// Provider
import { useAlert } from "@/components/AlertModal/AlertProvider";
// Models
import type { SaveEventDTO, CalendarEventDTO } from "@/models/Events";
// Icons
import SelectIcon from "@/app/src/svg/select.svg";
import DeleteIcon from "@/app/src/svg/trash.svg";
import AbortIcon from "@/app/src/svg/abort.svg";
import EditIcon from "@/app/src/svg/document_edit.svg";
import RenameIcon from "@/app/src/svg/rename_2.svg";
import SaveIcon from "@/app/src/svg/save.svg";
// Styles
import styles from "./MetaPanel.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:      styles["container"],
    center:         styles["center"],
    spaceBetween:   styles["spaceBetween"],
    icon:           styles["icon"],

    selectIcon:     styles["selectIcon"],
    selectInfo:     styles["selectInfo"],

    title:          styles["title"],
    description:    styles["description"],
    time:           styles["time"],

    group:          styles["group"],
    groupLabel:     styles["groupLabel"],

    item:           styles["item"],
    itemLabel:      styles["itemLabel"],
    itemValue:      styles["itemValue"],
    itemEM:         styles["itemEM"],

    actionArea:     styles["actionArea"],
    button:         styles["button"],
    buttonDelete:   styles["buttonDelete"],
    buttonSave:     styles["buttonSave"],
    buttonIcon:     styles["buttonIcon"],
    buttonText:     styles["buttonText"],
} as const;

export type MetaPanelProps = {
    selectedEvent: CalendarEventDTO | null;

    onSaveEvent: (data: SaveEventDTO) => Promise<CalendarEventDTO | null>;
    onDeleteEvent: (eventID: string) => void,
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function MetaPanel({
    selectedEvent,

    onSaveEvent,
    onDeleteEvent,
}: MetaPanelProps) {

    const { showAlert } = useAlert();

    const [editMode, setEditMode] = useState(false);

    const [editName, setEditName] = useState("");
    const [editDescription, setEditDescription] = useState("");
    const [editStartTime, setEditStartTime] = useState("");
    const [editEndTime, setEditEndTime] = useState("");



    // -------------- //
    // --- Helper --- //
    // -------------- //
    function determineType() {
        if (!selectedEvent) {
            return null;
        }

        const startDateValue = selectedEvent.startTime.slice(0, 10);
        const endDateValue = selectedEvent.endTime.slice(0, 10);

        const isMultiDay = startDateValue !== endDateValue;

        if (isMultiDay) {
            return "multiDay";
        }

        if (selectedEvent.isAllDay) {
            return "fullDay";
        }

        return "timed";
    }

    function formatDate(value: string) {
        const day = value.slice(8, 10);
        const month = value.slice(5, 7);
        const year = value.slice(0, 4);

        return `${day}.${month}.${year}`;
    }

    function formatTime(value: string) {
        return value.slice(11, 16);
    }

    function replaceDatePart(value: string, date: string) {
        const timePart = value.includes("T") ? value.slice(10) : "T00:00";

        return `${date}${timePart}`;
    }

    function replaceTimePart(value: string, time: string) {
        const datePart = value.slice(0, 10);

        return `${datePart}T${time}`;
    }



    // --------------- //
    // --- Handler --- //
    // --------------- //
    function handleStartEditMode() {
        if (!selectedEvent) {
            return;
        }

        setEditName(selectedEvent.name);
        setEditDescription(selectedEvent.description ?? "");
        setEditStartTime(selectedEvent.startTime);
        setEditEndTime(selectedEvent.endTime);

        setEditMode(true);
    }

    // Ask for confirmation before deleting. For multi-day events we warn that the whole series is removed.
    function handleDeleteEvent() {
        if (!selectedEvent) {
            return;
        }

        const isMultiDay = determineType() === "multiDay";

        showAlert({
            title: "Termin löschen",
            message: isMultiDay
                ? `Möchtest du den mehrtägigen Termin "${selectedEvent.name}" wirklich löschen? Dadurch wird die gesamte Terminreihe entfernt.`
                : `Möchtest du den Termin "${selectedEvent.name}" wirklich löschen?`,
            status: "delete",
            confirmText: "Ja",
            cancelText: "Nein",
            onConfirm: () => {
                onDeleteEvent(selectedEvent.id);
            },
        });
    }

    async function handleSaveEvent() {
        if (!selectedEvent) {
            return;
        }

        const savedEvent = await onSaveEvent({
            id: selectedEvent.id,
            name: editName,
            description: editDescription.trim() || null,
            isAllDay: selectedEvent.isAllDay,
            startTime: editStartTime,
            endTime: editEndTime,
        });

        if (!savedEvent) {
            return;
        }

        setEditMode(false);
    }

    function handleCancelEditMode() {
        setEditMode(false);
    }



    // ----------------- //
    // --- UI-Helper --- //
    // ----------------- //

    // Render Time text depending if timedEvent or multiDayEvent (Date-string will be prepared accordingly)
    function renderTimeText() {
        if (!selectedEvent) {
            return "";
        }

        const eventType = determineType();

        if (eventType === "fullDay") {
            return "Ganztag";
        }

        if (eventType === "multiDay") {
            const startDate = formatDate(selectedEvent.startTime);
            const endDate = formatDate(selectedEvent.endTime);

            return `${startDate} - ${endDate}`;
        }

        const startTime = formatTime(selectedEvent.startTime);
        const endTime = formatTime(selectedEvent.endTime);

        return `${startTime} - ${endTime}`;
    }

    // Render edit mode - Different input types depending on eventType (dayEvent, multiDayEvent, timedEvent)
    function renderEditMode() {
        if (!selectedEvent) {
            return null;
        }

        const eventType = determineType();

        return (
            <>
                {/* Group: Time */}
                <div className={c.group}>
                    <div className={c.groupLabel}>Zeit</div>

                    {eventType === "multiDay" && (
                        <>
                            <div className={c.itemEM}>
                                <input
                                    type="date"
                                    value={editStartTime.slice(0, 10)}
                                    onChange={(e) => setEditStartTime(replaceDatePart(editStartTime, e.target.value))}
                                />
                                <div className={c.icon}>
                                    <RenameIcon />
                                </div>
                            </div>

                            <div className={c.itemEM}>
                                <input
                                    type="date"
                                    value={editEndTime.slice(0, 10)}
                                    onChange={(e) => setEditEndTime(replaceDatePart(editEndTime, e.target.value))}
                                />
                                <div className={c.icon}>
                                    <RenameIcon />
                                </div>
                            </div>
                        </>
                    )}

                    {eventType === "fullDay" && (
                        <div className={c.itemEM}>
                            <input
                                type="date"
                                value={editStartTime.slice(0, 10)}
                                onChange={(e) => {
                                    setEditStartTime(replaceDatePart(editStartTime, e.target.value));
                                    setEditEndTime(replaceDatePart(editEndTime, e.target.value));
                                }}
                            />
                            <div className={c.icon}>
                                <RenameIcon />
                            </div>
                        </div>
                    )}

                    {eventType === "timed" && (
                        <>
                            <div className={c.itemEM}>
                                <input
                                    type="time"
                                    value={editStartTime.slice(11, 16)}
                                    onChange={(e) => setEditStartTime(replaceTimePart(editStartTime, e.target.value))}
                                />
                                <div className={c.icon}>
                                    <RenameIcon />
                                </div>
                            </div>

                            <div className={c.itemEM}>
                                <input
                                    type="time"
                                    value={editEndTime.slice(11, 16)}
                                    onChange={(e) => setEditEndTime(replaceTimePart(editEndTime, e.target.value))}
                                />
                                <div className={c.icon}>
                                    <RenameIcon />
                                </div>
                            </div>
                        </>
                    )}
                </div>


                {/* Group: Name */}
                <div className={c.group}>
                    <div className={c.groupLabel}>Name</div>

                    <div className={c.itemEM}>
                        <input
                            type="text"
                            value={editName}
                            onChange={(e) => setEditName(e.target.value)}
                        />
                        <div className={c.icon}>
                            <RenameIcon />
                        </div>
                    </div>
                </div>


                {/* Group: Description */}
                <div className={c.group}>
                    <div className={c.groupLabel}>Beschreibung</div>

                    <div className={c.itemEM}>
                        <input
                            type="text"
                            value={editDescription}
                            onChange={(e) => setEditDescription(e.target.value)}
                        />
                        <div className={c.icon}>
                            <RenameIcon />
                        </div>
                    </div>
                </div>


                {/* ActionArea */}
                <div className={c.actionArea}>
                    <button type="button" className={c.buttonSave} onClick={handleSaveEvent}>
                        <div className={c.buttonIcon}><SaveIcon/></div>
                        <div className={c.buttonText}>Speichern</div>
                    </button>

                    <button type="button" className={c.button} onClick={handleCancelEditMode}>
                        <div className={c.buttonIcon}><AbortIcon/></div>
                        <div className={c.buttonText}>Abbrechen</div>
                    </button>
                </div>
            </>
        );
    }

    function renderDisplayMode() {
        if (!selectedEvent) {
            return (
                <div className={c.center}>
                    <div className={c.selectIcon}>
                        <SelectIcon />
                    </div>
                    <div className={c.selectInfo}>
                        Wähle ein Event aus um Details zu sehen
                    </div>
                </div>
            );
        }

        const timeText = renderTimeText();

        return (
            <>
                <div className={c.group}>
                    <div className={c.groupLabel}>Zeit</div>
                    <div className={c.item}>
                        <div className={c.itemValue}>
                            {timeText}
                        </div>
                    </div>
                </div>

                <div className={c.group}>
                    <div className={c.groupLabel}>Beschreibung</div>
                    <div className={c.item}>
                        <div className={c.itemValue}>
                            {description}
                        </div>
                    </div>
                </div>

                <div className={c.group}>
                    <div className={c.groupLabel}>Details</div>

                    <div className={c.item + " " + c.spaceBetween}>
                        <div className={c.itemLabel}>ID:</div>
                        <div className={c.itemValue}>
                            {selectedEvent.id}
                        </div>
                    </div>

                    <div className={c.item + " " + c.spaceBetween}>
                        <div className={c.itemLabel}>Erstellt von:</div>
                        <div className={c.itemValue}>
                            {selectedEvent.createdBy}
                        </div>
                    </div>

                    <div className={c.item + " " + c.spaceBetween}>
                        <div className={c.itemLabel}>Erstellt am:</div>
                        <div className={c.itemValue}>
                            {createdAt}
                        </div>
                    </div>

                    <div className={c.item + " " + c.spaceBetween}>
                        <div className={c.itemLabel}>Aktualisiert von:</div>
                        <div className={c.itemValue}>
                            {selectedEvent.updatedBy}
                        </div>
                    </div>

                    <div className={c.item + " " + c.spaceBetween}>
                        <div className={c.itemLabel}>Aktualisiert am:</div>
                        <div className={c.itemValue}>
                            {updatedAt}
                        </div>
                    </div>
                </div>

                <div className={c.actionArea}>
                    <button type="button" className={c.button} onClick={handleStartEditMode}>
                        <div className={c.buttonIcon}><EditIcon/></div>
                        <div className={c.buttonText}>Bearbeiten</div>
                    </button>
                    <button type="button" className={c.buttonDelete} onClick={handleDeleteEvent}>
                        <div className={c.buttonIcon}><DeleteIcon/></div>
                        <div className={c.buttonText}>Löschen</div>
                    </button>
                </div>
            </>
        );
    }

    // Render content depending if an item is selected or not.
    function renderContent() {
        if (editMode) {
            return renderEditMode();
        }

        return renderDisplayMode();
    }



    // ---------------- //
    // --- Computed --- //
    // ---------------- //
    const description = selectedEvent?.description != null
        ? selectedEvent.description
        : selectedEvent?.name;

    const createdAt = selectedEvent
        ? formatDate(selectedEvent.createdAt)
        : "-";

    const updatedAt = selectedEvent
        ? formatDate(selectedEvent.updatedAt)
        : "-";



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            {renderContent()}
        </div>
    );
}