(() => {
    "use strict";

    if (window.kafoSecurityUiInitialized) return;
    window.kafoSecurityUiInitialized = true;

    document.addEventListener("submit", event => {
        const form = event.target instanceof HTMLFormElement ? event.target : null;
        const message = form?.dataset.confirm;
        if (message && !window.confirm(message)) event.preventDefault();
    }, true);

    document.addEventListener("click", event => {
        const target = event.target instanceof Element
            ? event.target.closest("[data-browser-action]")
            : null;
        const action = target?.getAttribute("data-browser-action");
        if (action === "print") window.print();
        if (action === "close") window.close();
    });

    document.addEventListener("change", event => {
        const target = event.target instanceof HTMLSelectElement ? event.target : null;
        if (target?.dataset.autoSubmit === "true" && target.form) {
            target.form.requestSubmit();
        }
    });

    const hideBrokenImage = image => {
        if (!(image instanceof HTMLImageElement) || image.dataset.hideOnError !== "true") return;
        image.hidden = true;
        image.nextElementSibling?.classList.remove("is-hidden");
    };

    document.addEventListener("error", event => hideBrokenImage(event.target), true);
    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll('img[data-hide-on-error="true"]').forEach(image => {
            if (image.complete && image.naturalWidth === 0) hideBrokenImage(image);
        });
    });
})();
