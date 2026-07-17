(function () {
    const root = document.querySelector("[data-admin-notifications]");
    if (!root) return;

    const button = root.querySelector("[data-admin-bell-button]");
    const countEl = root.querySelector("[data-admin-bell-count]");
    const listEl = root.querySelector("[data-admin-notifications-list]");
    const titleEl = root.querySelector("[data-admin-notifications-title]");
    const toast = document.querySelector("[data-admin-bell-toast]");

    let previousCount = Number(sessionStorage.getItem("kafo-admin-pending-notifications-count") || "0");
    let firstLoad = true;


    function showToast(message) {
        if (!toast) return;
        toast.textContent = message;
        toast.classList.add("is-visible");
        window.setTimeout(function () {
            toast.classList.remove("is-visible");
        }, 4200);
    }

    function normalizeLocalUrl(value) {
        const candidate = String(value || "/Admin/Donors");
        try {
            const parsed = new URL(candidate, window.location.origin);
            if (parsed.origin !== window.location.origin) return "/Admin/Donors";
            return parsed.pathname + parsed.search + parsed.hash;
        } catch (_) {
            return "/Admin/Donors";
        }
    }

    function renderItems(items) {
        listEl.replaceChildren();

        if (!Array.isArray(items) || items.length === 0) {
            const empty = document.createElement("div");
            empty.className = "admin-notifications-empty";
            empty.textContent = "لا توجد إشعارات حالياً";
            listEl.appendChild(empty);
            return;
        }

        items.forEach(function (item) {
            const link = document.createElement("a");
            link.className = "admin-notification-item" + (item.isActionRequired ? " is-action" : "");
            link.href = normalizeLocalUrl(item.url);

            const title = document.createElement("strong");
            title.textContent = String(item.title || "إشعار جديد");

            const message = document.createElement("p");
            message.textContent = String(item.message || "");

            const createdAt = document.createElement("time");
            createdAt.textContent = String(item.createdAt || "");

            link.append(title, message, createdAt);
            listEl.appendChild(link);
        });
    }

    async function refresh() {
        try {
            const response = await fetch("/Admin/NotificationsApi/Summary", {
                headers: { "Accept": "application/json" },
                cache: "no-store"
            });

            if (!response.ok) return;

            const data = await response.json();
            const count = Number(data.unreadCount || 0);

            if (titleEl) titleEl.textContent = data.title || "الإشعارات";

            if (countEl) {
                countEl.textContent = count > 99 ? "99+" : String(count);
                countEl.classList.toggle("is-hidden", count <= 0);
            }

            if (button) {
                button.classList.toggle("has-alert", count > 0);
                button.classList.toggle("is-flashing", count > 0);

                if (!firstLoad && count > previousCount) {
                    button.classList.add("admin-bell-shake");
                    showToast("يوجد طلب داعم جديد بانتظار المراجعة");
                    window.setTimeout(function () {
                        button.classList.remove("admin-bell-shake");
                    }, 800);
                }
            }

            renderItems(data.items || []);
            previousCount = count;
            sessionStorage.setItem("kafo-admin-pending-notifications-count", String(count));
            firstLoad = false;
        } catch (_) {
            // لا تعطل الصفحة إذا تعذر جلب الإشعارات مؤقتاً
        }
    }

    if (button) {
        button.addEventListener("click", function (event) {
            event.stopPropagation();
            root.classList.toggle("is-open");
        });
    }

    document.addEventListener("click", function (event) {
        if (!root.contains(event.target)) {
            root.classList.remove("is-open");
        }
    });

    refresh();
    window.setInterval(refresh, 10000);
})();
