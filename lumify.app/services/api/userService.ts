
// Utils
import { saveFetch } from "@/services/utils/auth";
import { getCsrfFromCookie } from "@/services/utils/auth";
// Misc
import { CONFIG } from "@/app/config/config";
import { SaveAccountInfoRequest, SaveUserProfileRequest } from "@/models/User";
// Types
import type { TodoEntryDTO } from "@/models/Todos";
import type { CalendarEventDTO } from "@/models/Events";
import type { Note } from "@/models/Notes";

const API_BASE = CONFIG.API.API_BASE;



/**
 * Client for the user endpoints: profile/account/avatar management, lookups by ID, recent
 * activity, related users and user search. Methods throw on a non-OK response.
 */
export const UserService = {


    /**
     * Updates the current user's profile (display name, avatar URL, bio).
     * @param data The profile fields to change.
     * @returns The updated profile.
     */
    async saveUserProfile(data: SaveUserProfileRequest) {

        const res = await saveFetch(`${API_BASE}/users/saveUserProfile`, {
            method: "PATCH",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(data),
        });

        if (!res.ok) {
            throw new Error("Failed to save userProfile");
        }

        return await res.json();
    },

    /**
     * Updates the current user's account info (name, e-mail).
     * @param data The account fields to change.
     * @returns The updated account info.
     */
    async saveAccountInfo(data: SaveAccountInfoRequest) {

        const res = await saveFetch(`${API_BASE}/users/saveAccountInfo`, {
            method: "PATCH",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(data),
        });

        if (!res.ok) {
            throw new Error("Failed to save accountInfo");
        }

        return await res.json();
    },

    /**
     * Soft-deletes the current user's account.
     * @returns `true` on success.
     */
    async deleteAccount() {

        const res = await saveFetch(`${API_BASE}/users/deleteAccount`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete account");
        }

        return true;
    },

    /**
     * Uploads a new avatar image for the current user (multipart upload with CSRF header).
     * @param file The image file to upload.
     * @returns The stored avatar URL.
     */
    async saveUserAvatar(file: File) {
        const formData = new FormData();
        formData.append("file", file);

        const csrfToken = getCsrfFromCookie();

        const res = await fetch(`${API_BASE}/users/saveUserAvatar`, {
            method: "POST",
            body: formData,
            credentials: "include",
            headers: {
                "X-CSRF-Token": csrfToken // Send csrf-token so API is satisfied
            }
        });

        if (!res.ok) {
            throw new Error("Failed to save avatar");
        }

        const avatarUrl = await res.text();
        return avatarUrl;
    },



    // ----------- //
    // --- GET --- //
    // ----------- //

    /**
     * Returns the current user's own profile.
     * @returns The profile.
     */
    async getUserProfile() {

        const res = await saveFetch(`${API_BASE}/users/getUserProfile`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch profile of user");
        }

        return await res.json();
    },

    /**
     * Returns the current user's own account info.
     * @returns The account info.
     */
    async getUserAccountInfo() {

        const res = await saveFetch(`${API_BASE}/users/getUserAccountInfo`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch account info of user");
        }

        return await res.json();
    },

    /**
     * Returns the current user's avatar URL.
     * @returns The avatar URL.
     */
    async getAvatarOfUser() {

        const res = await saveFetch(`${API_BASE}/users/getAvatarOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch avatar of user");
        }

        const avatarUrl = await res.text();
        return avatarUrl;
    },

    /**
     * Returns the public profile of a specific user.
     * @param userID The user to look up.
     * @returns The profile.
     */
    async getUserProfileWithID(userID: string) {

        const res = await saveFetch(`${API_BASE}/users/getUserProfileWithID?userID=${encodeURIComponent(userID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch profile of user: " + userID);
        }

        return await res.json();
    },

    /**
     * Returns the account info of a specific user.
     * @param userID The user to look up.
     * @returns The account info.
     */
    async getUserAccountInfoWithID(userID: string) {

        const res = await saveFetch(`${API_BASE}/users/getUserAccountInfoWithID?userID=${encodeURIComponent(userID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch account infos of user: " + userID);
        }

        return await res.json();
    },

    /**
     * Returns a compact preview of a specific user, with live presence.
     * @param userID The user to look up.
     * @returns The user preview.
     */
    async getUserPreviewWithID(userID: string) {
        const res = await saveFetch(`${API_BASE}/users/getUserPreviewWithID?userID=${encodeURIComponent(userID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch user preview");
        }

        return await res.json();
    },

    /**
     * Returns the users that share at least one workspace with the current user.
     * @returns The list of related users.
     */
    async getRelatedUsers() {

        const res = await saveFetch(`${API_BASE}/users/getRelatedUsers`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch related users");
        }

        return await res.json();
    },


    /**
     * Returns the current user's 5 most recently modified todo entries.
     * @returns Up to 5 todo entries, newest first.
     */
    async getLast5ModifiedTodosOfUser(): Promise<TodoEntryDTO[]> {

        const res = await saveFetch(`${API_BASE}/users/get5LastModifiedTodosOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch last modified todos of user");
        }

        return await res.json() as TodoEntryDTO[];
    },

    /**
     * Returns the current user's 5 most recently modified events.
     * @returns Up to 5 events, newest first.
     */
    async getLast5ModifiedEventsOfUser(): Promise<CalendarEventDTO[]> {

        const res = await saveFetch(`${API_BASE}/users/getLast5ModifiedEventsOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch last modified events of user");
        }

        return await res.json() as CalendarEventDTO[];
    },

    /**
     * Returns the current user's 5 most recently modified notes.
     * @returns Up to 5 notes, newest first.
     */
    async getLast5ModifiedNotesOfUser(): Promise<Note[]> {

        const res = await saveFetch(`${API_BASE}/users/getLast5ModifiedNotesOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch last modified notes of user");
        }

        return await res.json() as Note[];
    },





    /**
     * Searches users by display name, username or e-mail (excludes the current user).
     * @param query The search term.
     * @returns The matching users, with live presence.
     */
    async searchUsers(query: string) {

        const res = await saveFetch(
            `${API_BASE}/users/searchUsers?query=${encodeURIComponent(query)}`,
            {
                method: "GET",
            }
        );

        if (!res.ok) {
            const text = await res.text();

            throw new Error(`Failed to search users: ${text}`);
        }

        return await res.json();
    },

    /**
     * Searches users who can still be added to a workspace (excludes the current user and
     * existing members).
     * @param workspaceID The workspace to find addable users for.
     * @param query The search term.
     * @returns The addable users.
     */
    async searchAvailableUsersForWorkspace(workspaceID: string, query: string) {

        const res = await saveFetch(
            `${API_BASE}/users/searchAvailableUsersForWorkspace?workspaceID=${encodeURIComponent(workspaceID)}&query=${encodeURIComponent(query)}`,
            {
                method: "GET",
            }
        );

        if (!res.ok) {
            const text = await res.text();

            throw new Error(`Failed to search users for workspace (${workspaceID}): ${text}`);
        }

        return await res.json();
    },

};