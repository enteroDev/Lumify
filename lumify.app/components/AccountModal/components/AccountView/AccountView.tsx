"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useRef, useState, useEffect, use } from "react";
// Models
import { SaveAccountInfoRequest } from "@/models/User";
// Icons
import SaveIcon from "@/app/src/svg/save.svg";
// Styles
import styles from "./AccountView.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:        styles["container"],
    header:             styles["header"],
    title:              styles["title"],

    body:               styles["body"],
    group:              styles["group"],
    groupContent:       styles["groupContent"],
    groupHeader:        styles["groupHeader"],
    inputWrap:          styles["inputWrap"],
    label:              styles["label"],
    input:              styles["input"],

    footer:             styles["footer"],
    button:             styles["button"],
    buttonIcon:         styles["buttonIcon"],
    buttonText:         styles["buttonText"],

    dangerZone:         styles["dangerZone"],
    dangerHint:         styles["dangerHint"],
    deleteText:         styles["deleteText"],

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

                {/* Group */}
                <div className={c.group}>
                    <div className={c.groupHeader}></div>
                    <div className={c.groupContent}>

                        {/* Input: First Name */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Vorname</div>
                            <input className={c.input} value={accountFirstName} placeholder="[LEER]" disabled onChange={(e) => setAccountFirstName(e.target.value)}></input>
                        </div>

                        {/* Input: Last Name */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>Nachname</div>
                            <input className={c.input} value={accountLastName} placeholder="[LEER]" disabled onChange={(e) => setAccountLastName(e.target.value)}></input>
                        </div>

                        {/* Input: Email */}
                        <div className={c.inputWrap}>
                            <div className={c.label}>E-Mail</div>
                            <input className={c.input} value={accountEmail} placeholder="[LEER]" onChange={(e) => setAccountEmail(e.target.value)}></input>
                        </div>
                    </div>

                </div>

                {/* Security */}
                <div className={c.group}>
                    <div className={c.groupHeader}>Sicherheit</div>
                    <div className={c.groupContent}>
                        <button type="button" className={c.button} onClick={onOpenTwoFactor}>
                            <div className={c.buttonText}>Zwei-Faktor-Authentifizierung</div>
                        </button>
                    </div>
                </div>

                {/* Danger Zone - destructive, irreversible action kept apart from the rest */}
                <div className={c.dangerZone}>
                    <div className={c.dangerHint}>Diese Aktion kann nicht rückgängig gemacht werden.</div>
                    <button type="button" className={c.deleteText} onClick={onDeleteAccount}>
                        Account löschen
                    </button>
                </div>
            </div>

            {/* FOOTER */}
            <div className={c.footer}>

                {/* Button: Save */}
                <button 
                    className={c.button}
                    onClick={handleSave}
                    disabled={isSavingAccountInfo}
                >
                    <div className={c.buttonIcon}><SaveIcon /></div>
                    <div className={c.buttonText}>{isSavingAccountInfo ? "Speichert..." : "Speichern"}</div>
                </button>
            </div>
        </div>
    );
}