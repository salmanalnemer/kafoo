"use strict";

document.addEventListener("DOMContentLoaded", () => {
    const page = document.getElementById("rateLimitPage");
    const countdown = document.getElementById("rateLimitCountdown");

    if (!(page instanceof HTMLElement) ||
        !(countdown instanceof HTMLElement)) {
        return;
    }

    const configuredSeconds = Number.parseInt(
        page.dataset.seconds ?? "60",
        10
    );

    const returnUrl = isSafeLocalPath(page.dataset.returnUrl)
        ? page.dataset.returnUrl
        : "/Portal/Login";

    let remainingSeconds = Number.isFinite(configuredSeconds)
        ? Math.max(1, Math.min(configuredSeconds, 900))
        : 60;

    const render = () => {
        const minutes = Math.floor(remainingSeconds / 60);
        const seconds = remainingSeconds % 60;

        countdown.textContent =
            `${String(minutes).padStart(2, "0")}:` +
            `${String(seconds).padStart(2, "0")}`;
    };

    render();

    const timer = window.setInterval(() => {
        remainingSeconds -= 1;

        if (remainingSeconds <= 0) {
            window.clearInterval(timer);
            countdown.textContent = "00:00";
            window.location.replace(returnUrl);
            return;
        }

        render();
    }, 1000);
});

function isSafeLocalPath(value) {
    if (typeof value !== "string") {
        return false;
    }

    const path = value.trim();

    return path.startsWith("/") &&
        !path.startsWith("//") &&
        !path.startsWith("/\\") &&
        !path.includes("\r") &&
        !path.includes("\n") &&
        !path.includes("\0");
}
