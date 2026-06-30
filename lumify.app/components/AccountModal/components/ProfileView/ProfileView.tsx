"use client";

// --------------- //
// --- Imports --- //
// --------------- //


// React
import { useRef, useState, useEffect } from "react";
// Styles
import styles from "./ProfileView.module.css";
// Icons
import SaveIcon from "@/app/src/svg/save.svg";
import CameraIcon from "@/app/src/svg/camera.svg"
// Models
import { SaveUserProfileRequest } from "@/models/User";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:          styles["container"],
    header:             styles["header"],
    title:              styles["title"],

    body:               styles["body"],
    avatarLine:         styles["avatarLine"],
    avatar:             styles["avatar"],
    cameraBadge:        styles["cameraBadge"],
    userInfos:          styles["userInfos"],
    infoLine:           styles["infoLine"],
    infoLineLabel:      styles["infoLineLabel"],
    infoLineValue:      styles["infoLineValue"],
    usernameHint:       styles["usernameHint"],

    inputWrap:          styles["inputWrap"],
    label:              styles["label"],
    labelAvatar:        styles["labelAvatar"],
    input:              styles["input"],
    textarea:           styles["textarea"],

    footer:             styles["footer"],
    button:             styles["button"],
    buttonIcon:         styles["buttonIcon"],
    buttonText:         styles["buttonText"],
} as const;

type ProfileViewProps = {
    avatarUrl: string | null;
    isChangingAvatar?: boolean;
    onChangeAvatar: (file: File) => void;

    email: string;
    displayName: string;
    username: string;
    bio: string;

    onSaveProfile: (data: SaveUserProfileRequest) => void | Promise<void>;
    isSavingProfile?: boolean;
}



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ProfileView({
    avatarUrl,
    isChangingAvatar = false,
    onChangeAvatar,
    email,
    displayName,
    username,
    bio,
    onSaveProfile,
    isSavingProfile = false,
}:ProfileViewProps) {


    const fileInputRef = useRef<HTMLInputElement>(null);
    const [profileDisplayName, setProfileDisplayName] = useState(displayName);
    const [profileBio, setProfileBio] = useState(bio);



    // ------------------- //
    // --- UI-Helpers --- //
    // ------------------- //
    // Show the username as the main name; once a displayName exists, it leads and the username
    // trails in smaller parentheses. Falls back to "[LEER]" when nothing is set.
    const renderName = () => {
        if (displayName) {
            return (
                <>
                    {displayName}
                    {username && <span className={c.usernameHint}>({username})</span>}
                </>
            );
        }

        return username || "[LEER]";
    };



    // ------------------- //
    // --- UI-Handlers --- //
    // ------------------- // 
    function handleSave() {
        onSaveProfile({
            displayName: profileDisplayName.trim(),
            bio: profileBio.trim(),
        });
    }


    // ----------------- //
    // --- UseEffect --- //
    // ----------------- //
    useEffect(() => {
        setProfileDisplayName(displayName);
    }, [displayName]);

    useEffect(() => {
        setProfileBio(bio);
    }, [bio]);



    return (
        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>
                <div className={c.title}>Profil</div>
            </div>


            {/* BODY */}
            <div className={c.body}>

                {/* AvatarLine */}
                <div className={c.avatarLine}>
                    <div
                        className={c.avatar}
                        style={{
                            backgroundImage: avatarUrl ? `url(${avatarUrl})` : undefined,
                            opacity: isChangingAvatar ? 0.5 : 1, // Slight fade while uploading
                        }}
                    >
                        {/* Camera Badge */}
                        <span className={c.cameraBadge} onClick={() => fileInputRef.current?.click()}>
                            <CameraIcon />
                        </span>
                    </div>

                    {/* UserInfos */}
                    <div className={c.userInfos}>
                        <div className={c.infoLine}>
                            <div className={c.infoLineLabel}>Name: </div>
                            <div className={c.infoLineValue}>{renderName()}</div>
                        </div>

                        <div className={c.infoLine}>
                            <div className={c.infoLineLabel}>Email: </div>
                            <div className={c.infoLineValue}>{email || "[LEER]"}</div>
                        </div>

                        <div className={c.infoLine}>
                            <div className={c.infoLineLabel}>Role: </div>
                            <div className={c.infoLineValue}>User</div>
                        </div>
                    </div>

                    <input
                        ref={fileInputRef}
                        type="file"
                        accept="image/*"
                        style={{ display: "none" }}
                        onChange={(e) => {
                            const file = e.target.files?.[0];
                            if (file) onChangeAvatar(file);
                        }}
                    />
                </div>

                {/* Input: Display-Name */}
                <div className={c.inputWrap}>
                    <div className={c.label}>Anzeigename</div>
                    <input className={c.input} value={profileDisplayName} placeholder="[LEER]" onChange={(e) => setProfileDisplayName(e.target.value)}></input>
                </div>

                {/* Input: Profil-Bio */}
                <div className={c.inputWrap}>
                    <div className={c.label}>Bio</div>
                    <textarea className={c.textarea} value={profileBio} placeholder="[LEER]" onChange={(e) => setProfileBio(e.target.value)}></textarea>
                </div>
            </div>


            {/* FOOTER */}
            <div className={c.footer}>

                {/* Button: Save */}
                <button
                    className={c.button}
                    onClick={handleSave}
                    disabled={isSavingProfile}
                >
                    <div className={c.buttonIcon}><SaveIcon /></div>
                    <div className={c.buttonText}>{isSavingProfile ? "Speichert..." : "Speichern"}</div>
                </button>
            </div>
        </div>
    );
}