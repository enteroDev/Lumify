
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



export const UserService = {


    // ------------ //
    // --- SAVE --- //
    // ------------ //
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

    async deleteAccount() {

        const res = await saveFetch(`${API_BASE}/users/deleteAccount`, {
            method: "DELETE",
        });

        if (!res.ok) {
            throw new Error("Failed to delete account");
        }

        return true;
    },

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

    // --- Get userInformation of current user --- //
    async getUserProfile() {

        const res = await saveFetch(`${API_BASE}/users/getUserProfile`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch profile of user");
        }

        return await res.json();
    },

    async getUserAccountInfo() {

        const res = await saveFetch(`${API_BASE}/users/getUserAccountInfo`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch account info of user");
        }

        return await res.json();
    },

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

    // --- Get userInformation by providing userID --- //
    async getUserProfileWithID(userID: string) {

        const res = await saveFetch(`${API_BASE}/users/getUserProfileWithID?userID=${encodeURIComponent(userID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch profile of user: " + userID);
        }

        return await res.json();
    },

    async getUserAccountInfoWithID(userID: string) {

        const res = await saveFetch(`${API_BASE}/users/getUserAccountInfoWithID?userID=${encodeURIComponent(userID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch account infos of user: " + userID);
        }

        return await res.json();
    },

    async getUserPreviewWithID(userID: string) {
        const res = await saveFetch(`${API_BASE}/users/getUserPreviewWithID?userID=${encodeURIComponent(userID)}`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch user preview");
        }

        return await res.json();
    },

    async getRelatedUsers() {

        const res = await saveFetch(`${API_BASE}/users/getRelatedUsers`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch related users");
        }

        return await res.json();
    },


    // --- Get last modified data of user --- //
    async getLast5ModifiedTodosOfUser(): Promise<TodoEntryDTO[]> {

        const res = await saveFetch(`${API_BASE}/users/get5LastModifiedTodosOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch last modified todos of user");
        }

        return await res.json() as TodoEntryDTO[];
    },

    async getLast5ModifiedEventsOfUser(): Promise<CalendarEventDTO[]> {

        const res = await saveFetch(`${API_BASE}/users/getLast5ModifiedEventsOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch last modified events of user");
        }

        return await res.json() as CalendarEventDTO[];
    },

    async getLast5ModifiedNotesOfUser(): Promise<Note[]> {

        const res = await saveFetch(`${API_BASE}/users/getLast5ModifiedNotesOfUser`, {
            method: "GET",
        });

        if (!res.ok) {
            throw new Error("Failed to fetch last modified notes of user");
        }

        return await res.json() as Note[];
    },





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