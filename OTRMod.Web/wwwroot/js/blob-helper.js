// Blob URL helper for efficient binary data display in Blazor

/**
 * Creates a Blob URL from byte array data.
 * @param {Uint8Array} data - The binary data
 * @param {string} mimeType - MIME type (e.g., "image/png")
 * @returns {string} The Blob URL
 */
export function createBlobUrl(data, mimeType) {
    const blob = new Blob([data], { type: mimeType });
    return URL.createObjectURL(blob);
}

/**
 * Revokes a Blob URL to free memory.
 * @param {string} url - The Blob URL to revoke
 */
export function revokeBlobUrl(url) {
    if (url && url.startsWith('blob:')) {
        URL.revokeObjectURL(url);
    }
}
