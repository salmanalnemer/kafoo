(() => {
    "use strict";

    const form = document.getElementById("opportunityRequestForm");
    if (!form) return;

    const steps = Array.from(form.querySelectorAll(".org-request-step"));
    const progressSteps = Array.from(document.querySelectorAll(".org-progress-step"));
    const previousButton = document.getElementById("previousStepButton");
    const nextButton = document.getElementById("nextStepButton");
    const submitButton = document.getElementById("submitRequestButton");
    let currentStep = 0;

    const fieldByName = (name) => form.querySelector(`[name="${name}"]`);

    const safeValue = (name) => {
        const field = fieldByName(name);
        if (!field) return "—";
        const value = (field.value || "").trim();
        return value || "—";
    };

    const setReviewText = (id, value) => {
        const element = document.getElementById(id);
        if (element) element.textContent = value;
    };

    const updateReview = () => {
        setReviewText("reviewOpportunityType", safeValue("OpportunityType"));
        setReviewText("reviewTitle", safeValue("Title"));
        setReviewText("reviewAvailableCount", safeValue("AvailableCount"));
        setReviewText("reviewCity", safeValue("City"));
        setReviewText("reviewWorkNature", safeValue("WorkNature"));
        setReviewText("reviewEmploymentType", safeValue("EmploymentType"));
        setReviewText(
            "reviewSalaryAmount",
            safeValue("SalaryAmount") === "—" ? "—" : `${safeValue("SalaryAmount")} ر.س`
        );
        setReviewText(
            "reviewAnnualLeaveDays",
            safeValue("AnnualLeaveDays") === "—" ? "—" : `${safeValue("AnnualLeaveDays")} يومًا`
        );
        setReviewText("reviewWorkHours", safeValue("WorkHours"));
        setReviewText("reviewQualifications", safeValue("Qualifications"));
        setReviewText("reviewSkills", safeValue("Skills"));
        setReviewText("reviewSuitableDisabilityTypes", safeValue("SuitableDisabilityTypes"));
        setReviewText("reviewWorkLocation", safeValue("WorkLocation"));
        setReviewText("reviewDescription", safeValue("Description"));
    };

    const validateCurrentStep = () => {
        const fields = Array.from(
            steps[currentStep].querySelectorAll("input:not([type='hidden']), select, textarea")
        );

        for (const field of fields) {
            if (!field.checkValidity()) {
                field.reportValidity();
                field.focus();
                return false;
            }
        }
        return true;
    };

    const render = () => {
        steps.forEach((step, index) => {
            step.classList.toggle("is-active", index === currentStep);
        });

        progressSteps.forEach((step, index) => {
            step.classList.toggle("is-active", index === currentStep);
            step.classList.toggle("is-complete", index < currentStep);
        });

        previousButton.disabled = currentStep === 0;
        const isLastStep = currentStep === steps.length - 1;
        nextButton.style.display = isLastStep ? "none" : "inline-flex";
        submitButton.style.display = isLastStep ? "inline-flex" : "none";

        if (isLastStep) updateReview();

        const card = document.querySelector(".org-request-card");
        if (card && currentStep > 0) {
            card.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    };

    const closeAllMultiSelects = (except = null) => {
        document.querySelectorAll("[data-multi-select].is-open").forEach((item) => {
            if (item !== except) {
                item.classList.remove("is-open");
                item.querySelector(".org-multi-select-toggle")?.setAttribute("aria-expanded", "false");
            }
        });
    };

    document.querySelectorAll("[data-multi-select]").forEach((multiSelect) => {
        const targetName = multiSelect.dataset.target;
        const hiddenInput = fieldByName(targetName);
        const toggle = multiSelect.querySelector(".org-multi-select-toggle");
        const label = multiSelect.querySelector("[data-selection-label]");
        const checkboxes = Array.from(multiSelect.querySelectorAll('input[type="checkbox"]'));
        const selectAll = multiSelect.querySelector("[data-select-all]");

        const initialValues = (hiddenInput?.value || "")
            .split(",")
            .map((value) => value.trim())
            .filter(Boolean);

        checkboxes.forEach((checkbox) => {
            checkbox.checked = initialValues.includes(checkbox.value);
        });

        const sync = () => {
            if (selectAll?.checked) {
                checkboxes.forEach((checkbox) => {
                    checkbox.checked = checkbox === selectAll;
                });
            }

            const selected = checkboxes
                .filter((checkbox) => checkbox.checked)
                .map((checkbox) => checkbox.value);

            if (hiddenInput) {
                hiddenInput.value = selected.join(", ");
                hiddenInput.dispatchEvent(new Event("change", { bubbles: true }));
            }

            if (label) {
                if (selected.length === 0) {
                    label.textContent = targetName === "Skills"
                        ? "اختر المهارات"
                        : "اختر الفئات المناسبة";
                } else if (selected.length <= 2) {
                    label.textContent = selected.join("، ");
                } else {
                    label.textContent = `تم اختيار ${selected.length}`;
                }
            }
        };

        toggle?.addEventListener("click", (event) => {
            event.stopPropagation();
            const willOpen = !multiSelect.classList.contains("is-open");
            closeAllMultiSelects(multiSelect);
            multiSelect.classList.toggle("is-open", willOpen);
            toggle.setAttribute("aria-expanded", String(willOpen));
        });

        checkboxes.forEach((checkbox) => {
            checkbox.addEventListener("change", () => {
                if (checkbox !== selectAll && checkbox.checked && selectAll) {
                    selectAll.checked = false;
                }
                sync();
            });
        });

        sync();
    });

    document.addEventListener("click", () => closeAllMultiSelects());

    nextButton.addEventListener("click", () => {
        if (!validateCurrentStep()) return;
        if (currentStep < steps.length - 1) {
            currentStep += 1;
            render();
        }
    });

    previousButton.addEventListener("click", () => {
        if (currentStep > 0) {
            currentStep -= 1;
            render();
        }
    });

    form.addEventListener("submit", (event) => {
        if (!form.checkValidity()) {
            event.preventDefault();
            const invalidField = form.querySelector(":invalid");
            if (invalidField) {
                const parentStep = invalidField.closest(".org-request-step");
                const targetIndex = steps.indexOf(parentStep);
                if (targetIndex >= 0) {
                    currentStep = targetIndex;
                    render();
                }
                invalidField.reportValidity();
                invalidField.focus();
            }
            return;
        }

        submitButton.disabled = true;
        submitButton.textContent = submitButton.dataset.loadingText || "جاري حفظ الطلب...";
    });

    render();
})();
