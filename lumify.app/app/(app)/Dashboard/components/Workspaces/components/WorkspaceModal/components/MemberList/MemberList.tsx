"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Components
import MemberItem from "./components/MemberItem/MemberItem";
// Models
import type { WorkspaceMembersDTO } from "@/models/Space";
// Styles
import styles from "./MemberList.module.css";



// ------------------- //
// --- Types/Props --- //
// ------------------- //
const c = {
    container:      styles["container"],
    heading:        styles["heading"],

    memberList:     styles["memberList"],
    memberItem:     styles["memberItem"],
    empty:          styles["empty"],

    avatarArea:     styles["avatarArea"],
    avatar:         styles["avatar"],

    infoArea:       styles["infoArea"],
    username:       styles["username"],
    email:          styles["email"],

    actionArea:     styles["actionArea"],
    button:         styles["button"],
} as const;


export type MemberListProps = {
    members: WorkspaceMembersDTO[];
    onRemoveMember: (userID: string) => void;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function MemberList({
    members,
    onRemoveMember,
}: MemberListProps) {



    // ----------------- //
    // --- UI Helper --- //
    // ----------------- //

    // Workspace members excluding the owner. ( 1=Owner | 2=Admin | 3=User )
    const otherMembers = members.filter(member => member.role !== 1);

    const renderMemberList = () => {
        return otherMembers.map(member => (

            <MemberItem
                key={member.userID}
                avatar={member.avatarUrl ?? ""}
                displayName={member.displayName ?? ""}
                username={member.username}
                email={member.email ?? ""}
                onRemove={() => onRemoveMember(member.userID)}
            />
        ));
    };


    // ---------------- //
    // --- Handlers --- //
    // ---------------- //



    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>
            {otherMembers.length === 0 ? (
                <div className={c.empty}>
                    Noch keine Mitglieder in diesem Workspace
                </div>
            ) : (
                <div className={c.memberList}>
                    {renderMemberList()}
                </div>
            )}
        </div>
    );
}