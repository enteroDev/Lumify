// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Provider
import { useToast } from "@/components/Toast/ToastProvider";
// Styles
import styles from "./AddEventForm.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    container:        styles["container"],

    typeSelector:     styles["typeSelector"],
    typeButton:       styles["typeButton"],
    typeButtonActive: styles["typeButtonActive"],

    section:          styles["section"],
    sectionTitle:     styles["sectionTitle"],

    row:              styles["row"],
    field:            styles["field"],
    label:            styles["label"],
    input:            styles["input"],
    textarea:         styles["textarea"],

    actions:          styles["actions"],
    cancelButton:     styles["cancelButton"],
    saveButton:       styles["saveButton"],
} as const;


type EventType = "fullDay" | "timed" | "multiDay";

export type AddEventFormProps = {
    startDate?: string;

    onSave: (data: AddEventFormData) => void;
    onCancel: () => void;
};


export type AddEventFormData = {
    name: string;
    description: string;

    eventType: EventType;
    isAllDay: boolean;

    startTime: string;
    endTime: string;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AddEventForm({
    startDate,
    onSave,
    onCancel,
}: AddEventFormProps) {

    // -------------- //
    // --- States --- //
    // -------------- //
    const [eventType, setEventType] = useState<EventType>("timed");

    const [eventDate, setEventDate] = useState(startDate ?? "");

    const [startDateValue, setStartDateValue] = useState(startDate ?? "");
    const [endDateValue, setEndDateValue] = useState(startDate ?? "");

    const [startClockTime, setStartClockTime] = useState("09:00");
    const [endClockTime, setEndClockTime] = useState("10:00");

    const [name, setName] = useState("");
    const [description, setDescription] = useState("");

    const toast = useToast();



    // ---------------- //
    // --- Handlers --- //
    // ---------------- //
    function handleSubmit() {
        // Validate required fields so no event can be created without a real date/time.
        if (!name.trim()) {
            toast.error("Bitte einen Titel eingeben.");
            return;
        }

        let finalStartTime = "";
        let finalEndTime = "";

        if (eventType === "fullDay") {
            if (!eventDate) {
                toast.error("Bitte ein Datum wählen.");
                return;
            }

            finalStartTime = `${eventDate}T00:00`;
            finalEndTime = `${eventDate}T23:59`;
        }

        if (eventType === "timed") {
            if (!eventDate) {
                toast.error("Bitte ein Datum wählen.");
                return;
            }

            if (!startClockTime || !endClockTime) {
                toast.error("Bitte Start- und Endzeit angeben.");
                return;
            }

            if (endClockTime <= startClockTime) {
                toast.error("Die Endzeit muss nach der Startzeit liegen.");
                return;
            }

            finalStartTime = `${eventDate}T${startClockTime}`;
            finalEndTime = `${eventDate}T${endClockTime}`;
        }

        if (eventType === "multiDay") {
            if (!startDateValue || !endDateValue) {
                toast.error("Bitte Start- und Enddatum wählen.");
                return;
            }

            if (endDateValue < startDateValue) {
                toast.error("Das Enddatum darf nicht vor dem Startdatum liegen.");
                return;
            }

            finalStartTime = `${startDateValue}T00:00`;
            finalEndTime = `${endDateValue}T23:59`;
        }

        onSave({
            name,
            description,

            eventType,
            isAllDay: eventType !== "timed",

            startTime: finalStartTime,
            endTime: finalEndTime,
        });
    }

    function selectEventType(nextEventType: EventType) {
        setEventType(nextEventType);
    }



    // --------------- //
    // --- Effects --- //
    // --------------- //
    useEffect(() => {
        if (!startDate) { return; }

        setEventDate(startDate);
        setStartDateValue(startDate);
        setEndDateValue(startDate);
    }, [startDate]);



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* EVENT TYPE */}
            <div className={c.section}>
                <div className={c.field}>
                    <div className={c.label}>Terminart</div>
                    <div className={c.typeSelector}>
                        <button
                            type="button"
                            className={`${c.typeButton} ${eventType === "timed" ? c.typeButtonActive : ""}`}
                            onClick={() => selectEventType("timed")}
                        >
                            Zeitlich
                        </button>

                        <button
                            type="button"
                            className={`${c.typeButton} ${eventType === "fullDay" ? c.typeButtonActive : ""}`}
                            onClick={() => selectEventType("fullDay")}
                        >
                            Ganztag
                        </button>

                        <button
                            type="button"
                            className={`${c.typeButton} ${eventType === "multiDay" ? c.typeButtonActive : ""}`}
                            onClick={() => selectEventType("multiDay")}
                        >
                            Mehrere Tage
                        </button>
                    </div>
                </div>
            </div>


            {/* DATE / TIME */}
            <div className={c.section}>

                {eventType === "fullDay" && (
                    <div className={c.field}>
                        <label className={c.label}>Datum</label>
                        <input
                            className={c.input}
                            type="date"
                            value={eventDate}
                            onChange={(e) => setEventDate(e.target.value)}
                        />
                    </div>
                )}

                {eventType === "timed" && (
                    <>
                        <div className={c.field}>
                            <label className={c.label}>Datum</label>
                            <input
                                className={c.input}
                                type="date"
                                value={eventDate}
                                onChange={(e) => setEventDate(e.target.value)}
                            />
                        </div>

                        <div className={c.row}>
                            <div className={c.field}>
                                <label className={c.label}>Von</label>
                                <input
                                    className={c.input}
                                    type="time"
                                    value={startClockTime}
                                    onChange={(e) => setStartClockTime(e.target.value)}
                                />
                            </div>

                            <div className={c.field}>
                                <label className={c.label}>Bis</label>
                                <input
                                    className={c.input}
                                    type="time"
                                    value={endClockTime}
                                    onChange={(e) => setEndClockTime(e.target.value)}
                                />
                            </div>
                        </div>
                    </>
                )}

                {eventType === "multiDay" && (
                    <div className={c.row}>
                        <div className={c.field}>
                            <label className={c.label}>Von</label>
                            <input
                                className={c.input}
                                type="date"
                                value={startDateValue}
                                onChange={(e) => setStartDateValue(e.target.value)}
                            />
                        </div>

                        <div className={c.field}>
                            <label className={c.label}>Bis</label>
                            <input
                                className={c.input}
                                type="date"
                                value={endDateValue}
                                onChange={(e) => setEndDateValue(e.target.value)}
                            />
                        </div>
                    </div>
                )}

            </div>


            {/* INFO */}
            <div className={c.section}>

                {/* Name */}
                <div className={c.field}>
                    <label className={c.label}>Titel</label>
                    <input
                        className={c.input}
                        type="text"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                        placeholder="z. B. Meeting, Arzttermin, Geburtstag..."
                    />
                </div>

                {/* Description */}
                <div className={c.field}>
                    <label className={c.label}>Beschreibung</label>
                    <textarea
                        className={c.textarea}
                        value={description}
                        onChange={(e) => setDescription(e.target.value)}
                        placeholder="Optionale Beschreibung..."
                    />
                </div>
            </div>



            {/* ACTIONS */}
            <div className={c.actions}>
                <button type="button" className={c.cancelButton} onClick={onCancel}>
                    Abbrechen
                </button>

                <button type="button" className={c.saveButton} onClick={handleSubmit}>
                    Speichern
                </button>
            </div>

        </div>
    );
}