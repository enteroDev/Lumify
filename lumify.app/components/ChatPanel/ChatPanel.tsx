"use client";


// --------------- //
// --- Imports --- //
// --------------- //

// React
import { useEffect, useState } from "react";
// Components
import Avatar from "@/components/Avatar/Avatar";
// Hooks
import { useChatHub } from "@/hooks/useChatHub";
// Services
import { ChatService } from "@/services/api/chatService";
import { UserService } from "@/services/api/userService";
// Models
import type { SelectedChatUserVM } from "@/models/Chat";
// Icons
import SendIcon from "@/app/src/svg/send.svg"
// Styles
import styles from "./ChatPanel.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
const c = {
    container:          styles["container"],

    header:             styles["header"],
    avatarArea:         styles["avatarArea"],
    avatar:             styles["avatar"],
    infoArea:           styles["infoArea"],
    name:               styles["name"],
    info:               styles["info"],
    closeButton:        styles["closeButton"],

    messages:           styles["messages"],
    messageRow:         styles["messageRow"],
    messageRowOwn:      styles["messageRowOwn"],
    messageRowOther:    styles["messageRowOther"],
    messageBubble:      styles["messageBubble"],
    messageBubbleOwn:   styles["messageBubbleOwn"],
    messageBubbleOther: styles["messageBubbleOther"],
    messageSender:      styles["messageSender"],
    messageContent:     styles["messageContent"],

    inputArea:          styles["inputArea"],
    input:              styles["input"],
    button:             styles["button"],
} as const;

type ChatPanelProps = {
    selectedChatUser: SelectedChatUserVM | null;
    onCloseChat: () => void;
};

type Message = {
    id: string;
    senderID: string;
    senderName: string;
    content: string;
    createdAt: string;
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function ChatPanel({
    selectedChatUser,
    onCloseChat,
}: ChatPanelProps) {

    const [currentUserID, setCurrentUserID] = useState<string | null>(null);
    const [messages, setMessages] = useState<Message[]>([]);
    const [input, setInput] = useState("");

    const roomID = buildDirectRoomID(currentUserID, selectedChatUser?.userID ?? null);



    // ------------------ //
    // --- UseEffects --- //
    // ------------------ //

    // Load user information
    useEffect(() => {
        async function loadUser() {
            const userProfile = await UserService.getUserProfile();
            setCurrentUserID(userProfile.id);
        }

        loadUser();
    }, []);

    // Load chat
    useEffect(() => {
        if (!roomID) {
            setMessages([]);
            return;
        }

        let cancelled = false;

        const loadMessages = async () => {
            try {
                const loadedMessages = await ChatService.getMessagesOfRoom(roomID);

                if (cancelled) {
                    return;
                }

                setMessages(loadedMessages.map(x => ({
                    id: x.id,
                    senderID: x.senderID,
                    senderName: x.senderName,
                    content: x.content,
                    createdAt: x.createdAt,
                })));
            } catch (error) {
                if (cancelled) {
                    return;
                }

                console.error("Failed to load chat messages", error);
                setMessages([]);
            }
        };

        void loadMessages();

        return () => {
            cancelled = true;
        };
    }, [roomID]);


    // ----------- //
    // --- HUB --- //
    // ----------- //
    const { sendMessage } = useChatHub({
        userID: currentUserID,
        roomID,
        onMessageReceived: (msg) => {
            setMessages(prev => {
                const alreadyExists = prev.some(x => x.id === msg.id);

                if (alreadyExists) {
                    return prev;
                }

                return [
                    ...prev,
                    {
                        id: msg.id,
                        senderID: msg.senderID,
                        senderName: msg.senderName,
                        content: msg.content,
                        createdAt: msg.createdAt,
                    }
                ];
            });
        }
    });


    // ------------- //
    // --- Logic --- //
    // ------------- //
    function buildDirectRoomID(userAID: string | null, userBID: string | null) {
        if (!userAID || !userBID) {
            return null;
        }

        const sortedUserIDs = [userAID, userBID].sort();
        return `direct_${sortedUserIDs[0]}_${sortedUserIDs[1]}`;
    }

    const onSendMessage = async () => {
        const trimmedInput = input.trim();

        if (!trimmedInput) {
            return;
        }

        console.log("SEND CLICK", {
            currentUserID,
            targetUserID: selectedChatUser?.userID,
            roomID,
            input: trimmedInput,
        });

        try {
            await sendMessage(trimmedInput);
            setInput("");
        } catch (error) {
            console.error("Send failed", error);
        }
    };


    // ----------- //
    // --- JSX --- //
    // ----------- //
    return (
        <div className={c.container}>

            {/* HEADER */}
            <div className={c.header}>

                {/* Avatar */}
                <div className={c.avatarArea}>
                    <Avatar
                        avatarUrl={selectedChatUser?.avatarUrl}
                        displayName={selectedChatUser?.displayName || "Icognito"}
                        presenceStatus={selectedChatUser?.presenceStatus ?? 0}
                    />
                </div>

                {/* Infos */}
                <div className={c.infoArea}>
                    <div className={c.name}>{selectedChatUser?.displayName || "Icognito"}</div>
                    <div className={c.info}>{selectedChatUser?.email || "No email"}</div>
                </div>

                {/* Close */}
                <button className={c.closeButton} onClick={onCloseChat}>
                    ✕
                </button>
            </div>


            {/* MESSAGE-BOX */}
            <div className={c.messages}>
                {messages.map(x => {
                    const isOwnMessage = x.senderID === currentUserID;

                    return (
                        <div
                            key={x.id}
                            className={`${c.messageRow} ${isOwnMessage ? c.messageRowOwn : c.messageRowOther}`}
                        >
                            <div
                                className={`${c.messageBubble} ${isOwnMessage ? c.messageBubbleOwn : c.messageBubbleOther}`}
                            >
                                {!isOwnMessage && (
                                    <div className={c.messageSender}>{x.senderName}</div>
                                )}
                                <div className={c.messageContent}>{x.content}</div>
                            </div>
                        </div>
                    );
                })}
            </div>


            {/* INPUT-AREA */}
            <div className={c.inputArea}>
                <input
                    className={c.input}
                    value={input}
                    placeholder="Schreibe eine Nachricht..."

                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" && !e.shiftKey) {
                            e.preventDefault();
                            onSendMessage();
                        }
                    }}
                />
                <button className={c.button} onClick={onSendMessage}>
                    <SendIcon />
                </button>
            </div>

        </div>
    );
}