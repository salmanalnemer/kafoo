(() => {
    "use strict";

    const input = document.getElementById("candidateCvInput");
    const fileName = document.getElementById("candidateCvFileName");

    if (!input || !fileName) {
        return;
    }

    const container = input.closest(".aor-upload-field");
    const allowedExtensions = [".pdf", ".doc", ".docx"];
    const maximumSize = 10 * 1024 * 1024;

    const reset = (message) => {
        input.value = "";
        fileName.textContent = message;
        container?.classList.remove("is-selected");
        container?.classList.add("is-invalid");
    };

    input.addEventListener("change", () => {
        container?.classList.remove("is-invalid", "is-selected");

        const selectedFile = input.files?.[0];

        if (!selectedFile) {
            fileName.textContent = "لم يتم اختيار ملف";
            return;
        }

        const lowerName = selectedFile.name.toLowerCase();
        const extensionIsAllowed = allowedExtensions.some(
            (extension) => lowerName.endsWith(extension)
        );

        if (!extensionIsAllowed) {
            reset("الصيغة غير مسموحة — اختر PDF أو DOC أو DOCX");
            return;
        }

        if (selectedFile.size > maximumSize) {
            reset("حجم الملف يتجاوز 10MB");
            return;
        }

        fileName.textContent = selectedFile.name;
        container?.classList.add("is-selected");
    });
})();
