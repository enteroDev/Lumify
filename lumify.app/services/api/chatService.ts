
// --------------- //
// --- Imports --- //
// --------------- //

// Utils
import { CONFIG } from "@/app/(app)/config/config";
import { saveFetch } from "@/services/utils/auth";
// Models
import type { ChatMessageDTO } from "@/models/Chat";


const API_BASE = CONFIG.API.API_BASE;

export const ChatService = {

    // --- GET --- //
    async getMessagesOfRoom(roomID: string): Promise<ChatMessageDTO[]> {
        const response = await saveFetch(`${API_BASE}/chat/getMessagesOfRoom?roomID=${encodeURIComponent(roomID)}`, {
            method: "GET",
        });

        if (!response.ok) {
            throw new Error("Failed to fetch messages of room.");
        }

        return await response.json();
    },

};