
// --------------- //
// --- Imports --- //
// --------------- //

// React
import Link from "next/link";
// Styles
import styles from "./FeatureCard.module.css";



// --------------------- //
// --- Types & Props --- //
// --------------------- //
export const c = {
    container:          styles["container"],
    previewArea:        styles["previewArea"],
    previewImage:       styles["previewImage"],
    infoArea:           styles["infoArea"],
    infoLabel:          styles["infoLabel"],
    infoValue:          styles["infoValue"],
} as const;

export type FeatureCardProps = {
    infoText: string;
    value: number | null; // null = loading/unknown
    link: string;
    PreviewImage: React.ElementType;  // Bildquelle
};



// ----------------- //
// --- Component --- //
// ----------------- //
export default function FeatureCard({ infoText, value, link, PreviewImage }: FeatureCardProps) {
    return (
        <Link href={link} className={c.container}>
            <div className={c.previewArea}>
                <PreviewImage className={c.previewImage} />
            </div>
            <div className={c.infoArea}>
                <div className={c.infoLabel}>{infoText}</div>
                <div className={c.infoValue}>{value ?? "N/A"}</div>
            </div>
        </Link>
    );
}