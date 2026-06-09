

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import Header from "./components/Header/Header";
import AddEventForm from "@/components/_Forms/AddEventForm/AddEventForm";
import type { AddEventFormData } from "@/components/_Forms/AddEventForm/AddEventForm";
// Styles
import styles from "./AddEventModal.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
export const c = {
    overlay:        styles["overlay"],
    modal:          styles["modal"],

    header:         styles["header"],
    body:           styles["body"],
} as const;

export type AddEventModalProps = {
    isOpen: boolean;
    startDate?: string;

    onClose: () => void;
    onSave: (data: AddEventFormData) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AddEventModal({
    isOpen,
    startDate,

    onClose,
    onSave,
}:AddEventModalProps) {



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.overlay} onClick={onClose}>

            <div className={c.modal} onClick={(e) => e.stopPropagation()}>

                {/* HEADER */}
                <div className={c.header}>
                    <Header onClose={onClose}/>
                </div>

                {/* BODY */}
                <div className={c.body}>
                    <AddEventForm
                        startDate={startDate}
                        onCancel={onClose}
                        onSave={onSave}
                    />
                </div>

            </div>

        </div>
    );
}