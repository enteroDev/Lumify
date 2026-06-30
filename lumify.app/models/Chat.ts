import { PresenceStatus } from "./User";



// --- DTO --- //

/** A chat message as delivered to the client. Mirrors the backend `ChatMessageResponse`. */
export type ChatMessageDTO = {
    /** Message ID. */
    id: string;
    /** The room the message belongs to. */
    roomID: string;

    /** The sender's user ID. */
    senderID: string;
    /** The sender's resolved display name. */
    senderName: string;
    /** The message text. */
    content: string;

    /** Creation timestamp (ISO-8601 string). */
    createdAt: string;
    /** Last-update timestamp (ISO-8601 string). */
    updatedAt: string;
};



// --- ViewModels --- //

/** The user currently selected in the chat UI, with presence for the header. */
export type SelectedChatUserVM = {
    /** The selected user's ID. */
    userID: string;
    /** Display name shown in the chat header. */
    displayName: string;
    /** E-mail address. */
    email: string;
    /** Avatar URL, if any. */
    avatarUrl?: string | null;
    /** Current presence status. */
    presenceStatus: PresenceStatus;
};
