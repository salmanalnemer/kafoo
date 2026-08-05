(function () {
    "use strict";

    var root = document.querySelector("[data-portal-notifications]");
    if (!root) return;

    var toggle = root.querySelector("[data-notification-toggle]");
    var panel = root.querySelector("[data-notification-panel]");
    var countEl = root.querySelector("[data-notification-count]");
    var listEl = root.querySelector("[data-notification-list]");
    var markReadButton = root.querySelector("[data-notification-mark-read]");
    var notificationsPage = root.getAttribute("data-portal-notifications-page") || "/Portal/Donor/Notifications";
    var storageKey = "kafoPortalLatestNotificationKey";
    var latestKey = sessionStorage.getItem(storageKey) || "";
    var firstLoad = true;
    var isLoading = false;

    function getAntiForgeryToken() {
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : "";
    }


    function setCount(count) {
        var unreadCount = Number(count || 0);
        countEl.textContent = unreadCount > 99 ? "+99" : String(unreadCount);
        countEl.hidden = unreadCount <= 0;
        toggle.classList.toggle("has-unread", unreadCount > 0);
        root.classList.toggle("has-unread", unreadCount > 0);
    }

    function normalizeLocalUrl(value) {
        var candidate = String(value || notificationsPage);
        try {
            var parsed = new URL(candidate, window.location.origin);
            if (parsed.origin !== window.location.origin) return notificationsPage;
            return parsed.pathname + parsed.search + parsed.hash;
        } catch (_) {
            return notificationsPage;
        }
    }

    function renderItems(items) {
        listEl.replaceChildren();

        if (!Array.isArray(items) || items.length === 0) {
            var empty = document.createElement("div");
            empty.className = "portal-notification-empty";
            empty.textContent = "لا توجد إشعارات حتى الآن.";
            listEl.appendChild(empty);
            return;
        }

        items.forEach(function (item) {
            var isUnread = item.isRead === false || item.IsRead === false;
            var link = document.createElement("a");
            link.className = "portal-notification-item" + (isUnread ? " is-unread" : "");
            link.href = normalizeLocalUrl(item.url || item.Url || notificationsPage);

            var title = document.createElement("strong");
            title.textContent = String(item.title || item.Title || "إشعار جديد");

            var message = document.createElement("p");
            message.textContent = String(item.message || item.Message || "");

            var createdAt = document.createElement("small");
            createdAt.textContent = String(item.createdAtText || item.CreatedAtText || "");

            link.append(title, message, createdAt);
            listEl.appendChild(link);
        });
    }

    function ringBell() {
        if (!toggle) return;
        toggle.classList.remove("is-ringing");
        void toggle.offsetWidth;
        toggle.classList.add("is-ringing");
        window.setTimeout(function () {
            toggle.classList.remove("is-ringing");
        }, 2400);
    }

    function flashTitle(unreadCount) {
        if (unreadCount <= 0 || document.hidden === false) return;
        var original = document.title;
        var alt = "(" + unreadCount + ") إشعار جديد - كفو";
        var switched = false;
        var timer = window.setInterval(function () {
            document.title = switched ? original : alt;
            switched = !switched;
        }, 900);

        window.setTimeout(function () {
            window.clearInterval(timer);
            document.title = original;
        }, 7200);
    }

    async function loadNotifications(allowRing) {
        if (isLoading) return;
        isLoading = true;

        try {
            var response = await fetch("/Portal/Notifications/Summary", {
                method: "GET",
                credentials: "same-origin",
                headers: {
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (!response.ok) return;

            var data = await response.json();
            var newLatestKey = data.latestKey || data.LatestKey || "";
            var unreadCount = Number(data.unreadCount ?? data.UnreadCount ?? 0);
            var items = data.items || data.Items || [];

            setCount(unreadCount);
            renderItems(items);

            var hasNewNotification = unreadCount > 0 && newLatestKey && latestKey && newLatestKey !== latestKey;

            if ((firstLoad && unreadCount > 0) || (allowRing && hasNewNotification)) {
                ringBell();
                flashTitle(unreadCount);
            }

            if (newLatestKey) {
                latestKey = newLatestKey;
                sessionStorage.setItem(storageKey, latestKey);
            }
        } catch (error) {
            console.warn("Portal notifications refresh failed", error);
        } finally {
            firstLoad = false;
            isLoading = false;
        }
    }

    async function markAllRead() {
        var token = getAntiForgeryToken();

        try {
            var response = await fetch("/Portal/Notifications/MarkAllRead", {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Accept": "application/json",
                    "X-Requested-With": "XMLHttpRequest",
                    "RequestVerificationToken": token
                }
            });

            if (response.ok) {
                setCount(0);
                await loadNotifications(false);
            }
        } catch (error) {
            console.warn("Portal notifications mark-read failed", error);
        }
    }

    if (toggle && panel) {
        toggle.addEventListener("click", function (event) {
            event.stopPropagation();
            var shouldOpen = panel.hidden;
            panel.hidden = !shouldOpen;
            toggle.setAttribute("aria-expanded", shouldOpen ? "true" : "false");
            if (shouldOpen) {
                loadNotifications(false);
            }
        });
    }

    if (markReadButton) {
        markReadButton.addEventListener("click", function (event) {
            event.preventDefault();
            markAllRead();
        });
    }

    document.addEventListener("click", function (event) {
        if (!root.contains(event.target)) {
            panel.hidden = true;
            toggle.setAttribute("aria-expanded", "false");
        }
    });

    loadNotifications(false);
    window.setInterval(function () {
        loadNotifications(true);
    }, 10000);
})();
