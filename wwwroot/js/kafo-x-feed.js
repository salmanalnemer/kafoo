(function () {
    "use strict";

    var SCRIPT_ID = "kafo-x-widgets-script";
    var WIDGETS_URL = "https://platform.twitter.com/widgets.js";
    var renderToken = 0;
    var themeObserver = null;

    function getTheme() {
        return document.documentElement.dataset.theme === "dark"
            ? "dark"
            : "light";
    }

    function normalizeUsername(value) {
        return String(value || "")
            .trim()
            .replace(/^@+/, "")
            .replace(/[^A-Za-z0-9_]/g, "");
    }

    function loadWidgetsScript() {
        if (window.twttr && window.twttr.widgets) {
            return Promise.resolve(window.twttr);
        }

        var existing = document.getElementById(SCRIPT_ID);
        if (existing) {
            return new Promise(function (resolve, reject) {
                var attempts = 0;
                var timer = window.setInterval(function () {
                    attempts += 1;

                    if (window.twttr && window.twttr.widgets) {
                        window.clearInterval(timer);
                        resolve(window.twttr);
                    } else if (attempts >= 100) {
                        window.clearInterval(timer);
                        reject(new Error("تعذر تحميل مكوّن X."));
                    }
                }, 100);
            });
        }

        return new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.id = SCRIPT_ID;
            script.src = WIDGETS_URL;
            script.async = true;
            script.charset = "utf-8";
            script.referrerPolicy = "no-referrer";

            script.addEventListener("load", function () {
                if (window.twttr && window.twttr.widgets) {
                    resolve(window.twttr);
                } else {
                    reject(new Error("تم تحميل السكربت دون توفر مكوّن X."));
                }
            }, { once: true });

            script.addEventListener("error", function () {
                reject(new Error("فشل الاتصال بمنصة X."));
            }, { once: true });

            document.head.appendChild(script);
        });
    }

    function showError(container, username) {
        var host = container.querySelector("[data-kafo-x-host]");
        var loading = container.querySelector("[data-kafo-x-loading]");

        if (loading) {
            loading.hidden = true;
        }

        if (!host) {
            return;
        }

        host.replaceChildren();

        var error = document.createElement("div");
        error.className = "kafo-x-error";

        var text = document.createElement("span");
        text.textContent = "تعذر تحميل منشورات الجمعية الآن.";

        var link = document.createElement("a");
        link.href = "https://x.com/" + encodeURIComponent(username);
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        link.textContent = "زيارة الحساب على X";

        error.append(text, link);
        host.appendChild(error);
    }

    async function renderFeed(container) {
        var username = normalizeUsername(container.dataset.xUsername);
        var height = Number.parseInt(container.dataset.xHeight || "850", 10);
        var host = container.querySelector("[data-kafo-x-host]");
        var loading = container.querySelector("[data-kafo-x-loading]");
        var token = ++renderToken;

        if (!username || !host) {
            showError(container, username);
            return;
        }

        if (loading) {
            loading.hidden = false;
        }

        host.replaceChildren();

        try {
            var twttr = await loadWidgetsScript();

            if (token !== renderToken) {
                return;
            }

            var widget = await twttr.widgets.createTimeline(
                {
                    sourceType: "profile",
                    screenName: username
                },
                host,
                {
                    lang: "ar",
                    theme: getTheme(),
                    height: Number.isFinite(height) ? Math.max(500, height) : 850,
                    chrome: "noheader nofooter transparent",
                    dnt: true,
                    related: username
                }
            );

            if (token !== renderToken) {
                return;
            }

            if (!widget) {
                throw new Error("لم تُرجع منصة X مكوّنًا صالحًا.");
            }

            if (loading) {
                loading.hidden = true;
            }
        } catch (error) {
            if (token === renderToken) {
                showError(container, username);
            }

            console.warn("Kafo X feed:", error);
        }
    }

    function initialize() {
        var container = document.querySelector("[data-kafo-x-feed]");
        if (!container) {
            return;
        }

        renderFeed(container);

        if (window.MutationObserver) {
            themeObserver = new MutationObserver(function (mutations) {
                var themeChanged = mutations.some(function (mutation) {
                    return (
                        mutation.type === "attributes" &&
                        mutation.attributeName === "data-theme"
                    );
                });

                if (themeChanged) {
                    window.setTimeout(function () {
                        renderFeed(container);
                    }, 80);
                }
            });

            themeObserver.observe(document.documentElement, {
                attributes: true,
                attributeFilter: ["data-theme"]
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialize, { once: true });
    } else {
        initialize();
    }
})();
