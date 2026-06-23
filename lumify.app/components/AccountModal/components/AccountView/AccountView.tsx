"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useRef, useState, useEffect } from "react";
// Models
import { SaveAccountInfoRequest } from "@/models/User";
// Icons
import SaveIcon from "@/app/src/svg/save.svg";
import LockIcon from "@/app/src/svg/lock.svg";
// Styles
import styles from "./AccountView.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:        styles["container"],
    header:           styles["header"],
    title:            styles["title"],

    body:             styles["body"],
    section:          styles["section"],
    sectionTitle:     styles["sectionTitle"],
    sectionContent:   styles["sectionContent"],
    inputWrap:        styles["inputWrap"],
    label:            styles["label"],
    input:            styles["input"],

    saveRow:          styles["saveRow"],
    button:           styles["button"],
    buttonIcon:       styles["buttonIcon"],
    buttonText:       styles["buttonText"],

    twoFactorButton:  styles["twoFactorButton"],
    twoFactorIcon:    styles["twoFactorIcon"],

    dangerZone:       styles["dangerZone"],
    dangerHint:       styles["dangerHint"],
    deleteText:       styles["deleteText"],
} as const;

type AccountViewProps = {
    firstName: string;
    lastName: string;
    email: string;
    username: string;

    onSaveAccountInfo: (data: SaveAccountInfoRequest) => void | Promise<void>;
    isSavingAccountInfo?: boolean;

    onOpenTwoFactor: () => void;
    onDeleteAccount: () => void;
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function AccountView({
    firstName,
    lastName,
    email,
    username,

    onSaveAccountInfo,
    isSavingAccountInfo,
    onOpenTwoFactor,
    onDeleteAccount,
}: AccountViewProps) {

    const fileInputRef = useRef<HTMLInputElement>(null);
    const [accountFirstName, setAccountFirstName] = useState(firstName);
    const [accountLastName, setAccountLastName] = useState(lastName);
    const [accountEmail, setAccountEmail] = useState(email);



    // ------------------- //
    // --- UI-Handlers --- //
    // ------------------- //
    function handleSave() {
        onSaveAccountInfo({
            firstName: accountFirstName.trim(),
            lastName: accountLastName.trim(),
            email: accountEmail.trim(),
        });
    }



    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        setAccountFirstName(firstName);
    }, [firstName]);

    useEffect(() => {
        setAccountLastName(lastName);
    }, [lastName]);

    useEffect(() => {
        setAccountEmail(email);
    }, [email]);





    return (
        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>
                <div className={c.title}>Account</div>
            </div>

            {/* BODY */}
            <div className={c.body}>

                {/* Section: Personal data */}
                <div className={c.section}>
                    <div className={c.sectionTitle}>Persönliche Daten</div>

                    <div className={c.sectionContent}>
                        {/* First name (locked) */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Vorname</div>
                            <input className={c.input} value={accountFirstName} placeholder="[LEER]" disabled onChange={(e) => setAccountFirstName(e.target.value)} />
                        </div>

                        {/* Last name (locked) */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Nachname</div>
                            <input className={c.input} value={accountLastName} placeholder="[LEER]" disabled onChange={(e) => setAccountLastName(e.target.value)} />
                        </div>

                        {/* Email (editable) */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>E-Mail</div>
                            <input className={c.input} value={accountEmail} placeholder="[LEER]" onChange={(e) => setAccountEmail(e.target.value)} />
                        </div>
                    </div>

                    {/* Save - directly under the fields it saves */}
                    <div className={c.saveRow}>
                        <button className={c.button} onClick={handleSave} disabled={isSavingAccountInfo}>
                            <div className={c.buttonIcon}><SaveIcon /></div>
                            <div className={c.buttonText}>{isSavingAccountInfo ? "Speichert..." : "Speichern"}</div>
                        </button>
                    </div>
                </div>

                {/* Section: Security */}
                <div className={c.section}>
                    <div className={c.sectionTitle}>Sicherheit</div>

                    <button type="button" className={c.twoFactorButton} onClick={onOpenTwoFactor}>
                        <span className={c.twoFactorIcon}><LockIcon /></span>
                        Zwei-Faktor-Authentifizierung
                    </button>
                </div>

                {/* Danger zone */}
                <div className={c.dangerZone}>
                    <div className={c.dangerHint}>Diese Aktion kann nicht rückgängig gemacht werden.</div>
                    <button type="button" className={c.deleteText} onClick={onDeleteAccount}>
                        Account löschen
                    </button>
                </div>
            </div>
        </div>
    );
}
