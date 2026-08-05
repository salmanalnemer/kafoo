(function () {
    "use strict";

    document.querySelectorAll("[data-account-toggle]").forEach(function (button) {
        button.addEventListener("click", function () {
            var panelId = button.getAttribute("data-account-toggle");
            var panel = panelId ? document.getElementById(panelId) : null;

            if (!panel) {
                return;
            }

            panel.classList.toggle("is-open");
            button.textContent = panel.classList.contains("is-open")
                ? "إغلاق الإدارة"
                : "إدارة الحساب";
        });
    });

    document.querySelectorAll("[data-password-reset-form]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            var confirmed = window.confirm(
                "سيتم إلغاء كلمة المرور السابقة وإنشاء كلمة مرور مؤقتة جديدة وإرسالها إلى البريد المسجل. هل تريد المتابعة؟"
            );

            if (!confirmed) {
                event.preventDefault();
                return;
            }

            var button = form.querySelector("button[type='submit']");
            if (button) {
                button.disabled = true;
                button.textContent = "جاري الإرسال...";
            }
        });
    });
})();
