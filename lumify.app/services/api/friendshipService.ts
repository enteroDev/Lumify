"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Utils
import { saveFetch } from "@/services/utils/auth";
// Config
import { CONFIG } from "@/app/(app)/config/config";
// Models
import type { FriendshipDTO } from "@/models/Friendship";
import type { UserPreviewDTO } from "@/models/User";

const API_BASE = CONFIG.API.API_BASE;



// --------------- //
// --- Service --- //
// --------------- //
export const FriendshipService = {

    // ---------------------- //
    // --- FriendRequests --- //
    // ---------------------- //

    async sendFriendRequest(addresseeID: string): Promise<void> {
        const res = await saveFetch(
            `${API_BASE}/friendship/sendFriendRequest?addresseeID=${encodeURIComponent(addresseeID)}`,
            {
                method: "POST",
            }
        );

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || "Failed to send friend request.");
        }
    },

    async acceptFriendRequest(friendshipID: string): Promise<void> {
        const res = await saveFetch(
            `${API_BASE}/friendship/acceptFriendRequest?friendshipID=${encodeURIComponent(friendshipID)}`,
            {
                method: "PATCH",
            }
        );

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || "Failed to accept friend request.");
        }
    },

    async rejectFriendRequest(friendshipID: string): Promise<void> {
        const res = await saveFetch(
            `${API_BASE}/friendship/rejectFriendRequest?friendshipID=${encodeURIComponent(friendshipID)}`,
            {
                method: "PATCH",
            }
        );

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || "Failed to reject friend request.");
        }
    },



    // -------------- //
    // --- DELETE --- //
    // -------------- //    
    async removeFriend(friendID: string): Promise<void> {
        const res = await saveFetch(
            `${API_BASE}/friendship/removeFriend?friendID=${encodeURIComponent(friendID)}`,
            {
                method: "PATCH",
            }
        );

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || "Failed to remove friend.");
        }
    },



    // ----------- //
    // --- GET --- //
    // ----------- //

    async getFriendsOfUser(): Promise<UserPreviewDTO[]> {
        const res = await saveFetch(`${API_BASE}/friendship/getFriendsOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || "Failed to fetch friends.");
        }

        return await res.json();
    },

    async getIncomingFriendRequests(): Promise<FriendshipDTO[]> {
        const res = await saveFetch(`${API_BASE}/friendship/getIncomingFriendRequests`, {
            method: "GET",
        });

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || "Failed to fetch incoming friend requests.");
        }

        return await res.json();
    },

    async getOutgoingFriendRequests(): Promise<FriendshipDTO[]> {
        const res = await saveFetch(`${API_BASE}/friendship/getOutgoingFriendRequests`, {
            method: "GET",
        });

        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || "Failed to fetch outgoing friend requests.");
        }

        return await res.json();
    },
};