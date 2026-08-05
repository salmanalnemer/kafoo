(function () {
    "use strict";

    var THEME_KEY = "kafo.ui.theme";
    var LEGACY_FONT_KEY = "kafo.ui.fontScale";
    var root = document.documentElement;
    var currentTheme = readTheme();
    var scanGeneration = 0;
    var mutationObserver = null;

    function safeGet(key) {
        try {
            return window.localStorage.getItem(key);
        } catch (error) {
            return null;
        }
    }

    function safeSet(key, value) {
        try {
            window.localStorage.setItem(key, value);
        } catch (error) {
            // يستمر الاختيار داخل الصفحة حتى لو كان التخزين المحلي غير متاح.
        }
    }

    function safeRemove(key) {
        try {
            window.localStorage.removeItem(key);
        } catch (error) {
            // لا يلزم اتخاذ إجراء.
        }
    }

    function readTheme() {
        return safeGet(THEME_KEY) === "dark" ? "dark" : "light";
    }

    function removeOldFontFeature() {
        safeRemove(LEGACY_FONT_KEY);
        delete root.dataset.fontScale;
        root.style.removeProperty("--kafo-font-scale");

        document.querySelectorAll(".kafo-font-scalable").forEach(function (element) {
            element.classList.remove("kafo-font-scalable");
            element.style.removeProperty("--kafo-base-font-size");
        });
    }

    function applyThemeAttribute() {
        root.dataset.theme = currentTheme;
        root.style.colorScheme = currentTheme;

        var themeMeta = document.querySelector('meta[name="theme-color"]');
        if (themeMeta) {
            themeMeta.setAttribute(
                "content",
                currentTheme === "dark" ? "#0f1115" : "#74429a"
            );
        }
    }

    function updateButtonState() {
        var isDark = currentTheme === "dark";
        var label = isDark ? "تفعيل الوضع الفاتح" : "تفعيل الوضع الداكن";

        document.querySelectorAll("[data-kafo-theme-toggle]").forEach(function (button) {
            button.setAttribute("aria-pressed", String(isDark));
            button.setAttribute("aria-label", label);
            button.setAttribute("title", label);
        });
    }

    function announce(message) {
        var status = document.querySelector("[data-kafo-theme-status]");
        if (!status) {
            return;
        }

        status.textContent = "";
        window.setTimeout(function () {
            status.textContent = message;
        }, 20);
    }

    function parseColor(value) {
        if (!value || value === "transparent") {
            return null;
        }

        var match = value.match(
            /rgba?\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)(?:\s*[,/]\s*(\d+(?:\.\d+)?%?))?\s*\)/i
        );

        if (!match) {
            return null;
        }

        var alpha = 1;
        if (match[4] !== undefined) {
            alpha = match[4].indexOf("%") >= 0
                ? Number.parseFloat(match[4]) / 100
                : Number.parseFloat(match[4]);
        }

        return {
            r: Number.parseFloat(match[1]),
            g: Number.parseFloat(match[2]),
            b: Number.parseFloat(match[3]),
            a: alpha
        };
    }

    function luminance(color) {
        function channel(value) {
            var normalized = value / 255;
            return normalized <= 0.03928
                ? normalized / 12.92
                : Math.pow((normalized + 0.055) / 1.055, 2.4);
        }

        return (
            0.2126 * channel(color.r) +
            0.7152 * channel(color.g) +
            0.0722 * channel(color.b)
        );
    }

    function shouldSkip(element) {
        if (!(element instanceof HTMLElement)) {
            return true;
        }

        if (element.closest("[data-no-dark-auto]")) {
            return true;
        }

        return [
            "SCRIPT",
            "STYLE",
            "NOSCRIPT",
            "SVG",
            "PATH",
            "IMG",
            "PICTURE",
            "VIDEO",
            "CANVAS",
            "IFRAME",
            "SOURCE"
        ].indexOf(element.tagName) >= 0;
    }

    function isPageSurface(element) {
        return (
            element === document.body ||
            element.matches(
                "main, section.kafo-admin-content, section.portal-admin-content, " +
                ".kafo-admin-main, .portal-admin-main, .kafo-admin-content, " +
                ".portal-admin-content, .page-wrapper, .content-wrapper, " +
                ".main-content, .dashboard-page, .portal-page"
            )
        );
    }

    function isRaisedSurface(element) {
        return element.matches(
            "input, textarea, select, option, .form-control, .form-select, " +
            ".input-group-text, th, thead, .card-header, .card-footer, " +
            ".modal-header, .modal-footer, .accordion-button, .list-group-item, " +
            ".dropdown-item, .page-link, .select2-selection"
        );
    }

    function classifyElement(element) {
        if (shouldSkip(element)) {
            return;
        }

        var styles = window.getComputedStyle(element);
        var background = parseColor(styles.backgroundColor);
        var textColor = parseColor(styles.color);
        var borderColor = parseColor(styles.borderTopColor);
        var backgroundImage = styles.backgroundImage || "";
        var hasPhoto = backgroundImage.indexOf("url(") >= 0;

        if (
            background &&
            background.a > 0.18 &&
            luminance(background) > 0.55 &&
            !hasPhoto
        ) {
            element.classList.add(
                isPageSurface(element)
                    ? "kafo-dark-auto-page"
                    : isRaisedSurface(element)
                        ? "kafo-dark-auto-raised"
                        : "kafo-dark-auto-surface"
            );
        }

        if (
            borderColor &&
            borderColor.a > 0.18 &&
            luminance(borderColor) > 0.50 &&
            styles.borderTopStyle !== "none" &&
            Number.parseFloat(styles.borderTopWidth) > 0
        ) {
            element.classList.add("kafo-dark-auto-border");
        }

        if (
            textColor &&
            textColor.a > 0.35 &&
            luminance(textColor) < 0.36
        ) {
            if (
                element.matches(
                    ".text-muted, .text-secondary, small, " +
                    "[class*='muted'], [class*='description'], " +
                    "[class*='subtitle'], [class*='meta']"
                )
            ) {
                element.classList.add("kafo-dark-auto-muted");
            } else {
                element.classList.add("kafo-dark-auto-text");
            }
        }
    }

    function createElementQueue(container) {
        var queue = [];

        if (container instanceof HTMLElement) {
            queue.push(container);
        }

        if (container && container.querySelectorAll) {
            Array.prototype.push.apply(queue, container.querySelectorAll("*"));
        }

        return queue;
    }

    function scanQueue(queue, generation) {
        var index = 0;
        var chunkSize = 220;

        function processChunk() {
            if (
                generation !== scanGeneration ||
                currentTheme !== "dark"
            ) {
                return;
            }

            var limit = Math.min(index + chunkSize, queue.length);
            for (; index < limit; index += 1) {
                classifyElement(queue[index]);
            }

            if (index < queue.length) {
                window.setTimeout(processChunk, 0);
            }
        }

        processChunk();
    }

    function scanDarkTree(container) {
        if (currentTheme !== "dark" || !document.body || !container) {
            return;
        }

        scanQueue(createElementQueue(container), scanGeneration);
    }

    function scanWholePage() {
        if (currentTheme !== "dark" || !document.body) {
            return;
        }

        scanGeneration += 1;
        scanDarkTree(document.body);
    }

    function setTheme(theme, shouldAnnounce) {
        currentTheme = theme === "dark" ? "dark" : "light";
        safeSet(THEME_KEY, currentTheme);
        applyThemeAttribute();
        updateButtonState();

        scanGeneration += 1;
        if (currentTheme === "dark") {
            window.requestAnimationFrame(scanWholePage);
            window.setTimeout(scanWholePage, 180);
        }

        if (shouldAnnounce) {
            announce(
                currentTheme === "dark"
                    ? "تم تفعيل الوضع الداكن"
                    : "تم تفعيل الوضع الفاتح"
            );
        }
    }

    function bindButtons() {
        document.querySelectorAll("[data-kafo-theme-toggle]").forEach(function (button) {
            if (button.dataset.kafoThemeBound === "true") {
                return;
            }

            button.dataset.kafoThemeBound = "true";
            button.addEventListener("click", function () {
                setTheme(currentTheme === "dark" ? "light" : "dark", true);
            });
        });
    }

    function observeDynamicContent() {
        if (!window.MutationObserver || !document.body) {
            return;
        }

        mutationObserver = new MutationObserver(function (mutations) {
            bindButtons();

            if (currentTheme !== "dark") {
                return;
            }

            mutations.forEach(function (mutation) {
                Array.prototype.forEach.call(mutation.addedNodes, function (node) {
                    if (node instanceof HTMLElement) {
                        scanDarkTree(node);
                    }
                });
            });
        });

        mutationObserver.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    function initialize() {
        removeOldFontFeature();
        applyThemeAttribute();
        bindButtons();
        updateButtonState();
        observeDynamicContent();

        if (currentTheme === "dark") {
            scanWholePage();
            window.setTimeout(scanWholePage, 160);
        }
    }

    /* يطبق السمة قبل اكتمال DOM لتقليل ظهور الخلفية البيضاء. */
    applyThemeAttribute();

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialize, { once: true });
    } else {
        initialize();
    }
})();
