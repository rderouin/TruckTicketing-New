
export function openPdfBytes(bytes) {
    const numArray = atob(bytes).split('').map(c => c.charCodeAt(0));
    const uint8Array = new Uint8Array(numArray);
    const blob = new Blob([uint8Array], { type: "application/pdf" });
    const url = URL.createObjectURL(blob);
    window.open(url);
}

export function openPdf(uri) {
    if (uri) {
        window.open(uri);
    }
    return;
}
