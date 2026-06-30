// services/utils/format.ts
// Small helpers for privacy-friendly display formatting.



/**
 * Masks an email address for display, keeping only the first character of the local part and of the
 * domain name while preserving the top-level domain — e.g. `entero.world@yahoo.com` becomes
 * `e***@y***.com`. The masking uses a fixed number of asterisks so it does not leak the original
 * lengths. Inputs that are not a normal `local@domain` shape are returned unchanged.
 * @param email The email address to mask.
 * @returns The masked email, or the original string when it cannot be masked.
 */
export function maskEmail(email: string): string {
    if (!email) return "";

    const atIndex = email.indexOf("@");
    if (atIndex < 1) return email;

    const local = email.slice(0, atIndex);
    const domain = email.slice(atIndex + 1);

    const maskedLocal = `${local[0]}***`;

    const dotIndex = domain.lastIndexOf(".");
    if (dotIndex < 1) {
        return `${maskedLocal}@${domain[0]}***`;
    }

    const domainName = domain.slice(0, dotIndex);
    const tld = domain.slice(dotIndex); // includes the leading "."

    return `${maskedLocal}@${domainName[0]}***${tld}`;
}
