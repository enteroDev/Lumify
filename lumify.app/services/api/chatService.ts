
// --------------- //
// --- Imports --- //
// --------------- //

// Utils
import { CONFIG } from "@/app/config/config";
import { saveFetch } from "@/services/utils/auth";
// Models
import type { ChatMessageDTO } from "@/models/Chat";


const API_BASE = CONFIG.API.API_BASE;

/**
 * Client for the chat endpoints. Real-time message delivery is handled by the chat hub
 * (see `useChatHub`); this service only loads the persisted history.
 */
export const ChatService = {

    /**
     * Loads the full message history of a room, oldest first.
     * @param roomID The room to load.
     * @returns The room's messages.
     */
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