import { PresenceStatus } from "./User";



// --- DTO --- //
export type ChatMessageDTO = {
    id: string;
    roomID: string;

    senderID: string;
    senderName: string;
    content: string;

    createdAt: string;
    updatedAt: string;
};



// --- ViewModels --- //
export type SelectedChatUserVM = {
    userID: string;
    displayName: string;
    email: string;
    avatarUrl?: string | null;
    presenceStatus: PresenceStatus;
};