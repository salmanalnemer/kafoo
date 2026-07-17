(function () {
    function ready(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn);
        } else {
            fn();
        }
    }

    ready(function () {
        var sections = document.querySelectorAll(".kafo-menu-section");

        sections.forEach(function (section, index) {
            var title = section.querySelector(".kafo-menu-title");
            if (!title) return;

            var titleText = (title.textContent || "").trim();
            var key = "kafo-admin-sidebar-section-" + index + "-" + titleText;
            var hasActiveLink = section.querySelector("a.is-active") !== null;

            title.setAttribute("role", "button");
            title.setAttribute("tabindex", "0");

            if (!title.querySelector(".kafo-menu-arrow")) {
                var arrow = document.createElement("span");
                arrow.className = "kafo-menu-arrow";
                arrow.textContent = "⌄";
                title.appendChild(arrow);
            }

            var saved = localStorage.getItem(key);

            if (saved === "closed" && !hasActiveLink) {
                section.classList.add("is-collapsed");
            } else {
                section.classList.remove("is-collapsed");
            }

            function toggleSection() {
                section.classList.toggle("is-collapsed");

                if (section.classList.contains("is-collapsed")) {
                    localStorage.setItem(key, "closed");
                } else {
                    localStorage.setItem(key, "open");
                }
            }

            title.addEventListener("click", toggleSection);

            title.addEventListener("keydown", function (event) {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    toggleSection();
                }
            });
        });
    });
})();
