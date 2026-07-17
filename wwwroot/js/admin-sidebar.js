(function () {
    const body = document.body;
    const sidebar = document.getElementById("adminSidebar");
    const toggle = document.getElementById("sidebarToggle");
    const mobileToggle = document.getElementById("mobileSidebarToggle");
    const overlay = document.getElementById("sidebarOverlay");

    const sidebarStorageKey = "kafo-admin-sidebar-collapsed";
    const groupsStorageKey = "kafo-admin-sidebar-groups";

    function getSavedGroups() {
        try {
            return JSON.parse(localStorage.getItem(groupsStorageKey) || "{}");
        } catch {
            return {};
        }
    }

    function saveGroups(groups) {
        localStorage.setItem(groupsStorageKey, JSON.stringify(groups));
    }

    function applySavedSidebarState() {
        const collapsed = localStorage.getItem(sidebarStorageKey) === "1";
        body.classList.toggle("sidebar-collapsed", collapsed);
    }

    function toggleSidebar() {
        body.classList.toggle("sidebar-collapsed");
        localStorage.setItem(sidebarStorageKey, body.classList.contains("sidebar-collapsed") ? "1" : "0");
    }

    function openMobileSidebar() {
        body.classList.add("sidebar-mobile-open");
    }

    function closeMobileSidebar() {
        body.classList.remove("sidebar-mobile-open");
    }

    function setupActiveLinks() {
        if (!sidebar) return;

        const currentPath = window.location.pathname.toLowerCase();
        const links = sidebar.querySelectorAll("a[href]");

        links.forEach(function (link) {
            const href = link.getAttribute("href");
            if (!href || href === "/" || href === "#") return;

            if (currentPath.startsWith(href.toLowerCase())) {
                link.classList.add("active");

                const group = link.closest(".kafo-menu-group");
                if (group) {
                    group.classList.remove("is-collapsed");
                }
            }
        });
    }

    function setupCollapsibleGroups() {
        if (!sidebar) return;

        const savedGroups = getSavedGroups();
        const groups = sidebar.querySelectorAll(".kafo-menu-group");

        groups.forEach(function (group, index) {
            const title = group.querySelector(".kafo-menu-title");
            if (!title) return;

            const titleText = title.textContent.trim();
            const groupKey = titleText || `group-${index}`;

            title.setAttribute("role", "button");
            title.setAttribute("tabindex", "0");
            title.setAttribute("aria-expanded", "true");

            if (!title.querySelector(".kafo-menu-arrow")) {
                const arrow = document.createElement("i");
                arrow.className = "fa-solid fa-chevron-down kafo-menu-arrow";
                title.appendChild(arrow);
            }

            if (savedGroups[groupKey] === "collapsed") {
                group.classList.add("is-collapsed");
                title.setAttribute("aria-expanded", "false");
            }

            const activeLink = group.querySelector("a.active");
            if (activeLink) {
                group.classList.remove("is-collapsed");
                title.setAttribute("aria-expanded", "true");
                savedGroups[groupKey] = "open";
                saveGroups(savedGroups);
            }

            function toggleGroup() {
                group.classList.toggle("is-collapsed");

                const isCollapsed = group.classList.contains("is-collapsed");
                title.setAttribute("aria-expanded", isCollapsed ? "false" : "true");

                const latestSaved = getSavedGroups();
                latestSaved[groupKey] = isCollapsed ? "collapsed" : "open";
                saveGroups(latestSaved);
            }

            title.addEventListener("click", toggleGroup);

            title.addEventListener("keydown", function (event) {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    toggleGroup();
                }
            });
        });
    }

    applySavedSidebarState();

    if (toggle) {
        toggle.addEventListener("click", toggleSidebar);
    }

    if (mobileToggle) {
        mobileToggle.addEventListener("click", openMobileSidebar);
    }

    if (overlay) {
        overlay.addEventListener("click", closeMobileSidebar);
    }

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            closeMobileSidebar();
        }
    });

    setupActiveLinks();
    setupCollapsibleGroups();
})();

/* Admin sidebar groups */
(function () {
    function initAdminGroups() {
        document.querySelectorAll("[data-admin-group-toggle]").forEach(function (button) {
            button.addEventListener("click", function () {
                var group = button.closest(".admin-sidebar-group");
                if (group) {
                    group.classList.toggle("is-open");
                }
            });
        });

        var path = window.location.pathname.toLowerCase();

        document.querySelectorAll(".admin-sidebar-group").forEach(function (group) {
            var hasActiveLink = false;

            group.querySelectorAll("a[href]").forEach(function (link) {
                var href = (link.getAttribute("href") || "").toLowerCase();

                if (href && path.startsWith(href)) {
                    link.classList.add("is-active");
                    hasActiveLink = true;
                }
            });

            if (hasActiveLink) {
                group.classList.add("is-open");
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initAdminGroups);
    } else {
        initAdminGroups();
    }
})();
