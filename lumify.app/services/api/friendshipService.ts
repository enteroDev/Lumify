"use client";

// --------------- //
// --- Imports --- //
// --------------- //

// Utils
import { saveFetch } from "@/services/utils/auth";
// Config
import { CONFIG } from "@/app/config/config";
// Models
import type { FriendshipDTO } from "@/models/Friendship";
import type { UserPreviewDTO } from "@/models/User";

const API_BASE = CONFIG.API.API_BASE;



// --------------- //
// --- Service --- //
// --------------- //
/**
 * Client for the friendship endpoints (send/accept/reject requests, remove friends, and list
 * friends and pending requests). Live updates are delivered separately over the chat hub
 * (see `usePresence`). Methods throw with the server's message on failure.
 */
export const FriendshipService = {

    /**
     * Sends a friend request to another user.
     * @param addresseeID The user to send the request to.
     */
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

    /**
     * Accepts a pending friend request addressed to the current user.
     * @param friendshipID The pending friendship to accept.
     */
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

    /**
     * Rejects a pending friend request addressed to the current user.
     * @param friendshipID The pending friendship to reject.
     */
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
    /**
     * Removes an existing friend.
     * @param friendID The friend to remove.
     */
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

    /**
     * Returns the current user's accepted friends, each with live presence status.
     * @returns The list of friends.
     */
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

    /**
     * Returns the pending friend requests addressed to the current user.
     * @returns The list of incoming requests.
     */
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

    /**
     * Returns the pending friend requests sent by the current user.
     * @returns The list of outgoing requests.
     */
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