"use client"

import Link from "next/link";
import { usePathname } from "next/navigation";
import PathIcon from "../../../app/src/svg/home.svg";

import styles from "./PathBar.module.css";

export const c = {
    container:      styles["container"],
    pathPill:       styles["pathPill"],
    pathText:       styles["pathText"],
    pathButton:     styles["pathButton"],
    pathIcon:       styles["pathIcon"],
} as const;


export default function PathBar() {
    const pathname = usePathname();

    // If "/" → Display: "/Dashboard"
    const routeLabel =
        pathname === "/"
            ? "Dashboard"
            : pathname.replace("/", "").charAt(0).toUpperCase() 
            + pathname.replace("/", "").slice(1);

    return (
        <div className={c.container}>
            <div className={c.pathPill}>
                <Link href="/" className={c.pathButton}>
                    <PathIcon className={c.pathIcon} />
                </Link>
                <div className={c.pathText}>/ {routeLabel}</div>
            </div>
        </div>
    );
}