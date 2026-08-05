(function () {
    const track = document.getElementById("kafoSlider");
    const next = document.getElementById("nextSlide");
    const prev = document.getElementById("prevSlide");

    if (track) {
        const slides = track.querySelectorAll(".kafo-slide");

        if (slides && slides.length > 1) {
            let index = 0;
            let timer = null;

            function updateSlider() {
                track.style.transform = `translate3d(-${index * 100}%, 0, 0)`;
            }

            function goNext() {
                index = (index + 1) % slides.length;
                updateSlider();
            }

            function goPrev() {
                index = (index - 1 + slides.length) % slides.length;
                updateSlider();
            }

            function startAutoPlay() {
                stopAutoPlay();
                timer = setInterval(goNext, 6000);
            }

            function stopAutoPlay() {
                if (timer) {
                    clearInterval(timer);
                    timer = null;
                }
            }

            if (next) {
                next.addEventListener("click", function () {
                    goNext();
                    startAutoPlay();
                });
            }

            if (prev) {
                prev.addEventListener("click", function () {
                    goPrev();
                    startAutoPlay();
                });
            }

            updateSlider();
            startAutoPlay();
        }
    }

    const counters = document.querySelectorAll(".counter-number");
    let counterStarted = false;

    function formatNumber(value) {
        return new Intl.NumberFormat("en-US").format(value);
    }

    function animateCounter(counter) {
        const target = parseInt(counter.dataset.target || "0", 10);
        const duration = 1400;
        const startTime = performance.now();

        function update(now) {
            const progress = Math.min((now - startTime) / duration, 1);
            const eased = 1 - Math.pow(1 - progress, 3);
            const current = Math.floor(target * eased);

            counter.textContent = formatNumber(current);

            if (progress < 1) {
                requestAnimationFrame(update);
            } else {
                counter.textContent = formatNumber(target);
            }
        }

        requestAnimationFrame(update);
    }

    const statsSection = document.getElementById("homeStats");

    if (statsSection && counters.length) {
        const observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting && !counterStarted) {
                    counterStarted = true;
                    counters.forEach(animateCounter);
                    observer.disconnect();
                }
            });
        }, { threshold: 0.35 });

        observer.observe(statsSection);
    }
})();

/* =====================================================
   Home Programs Filter
===================================================== */
(function () {
    const tabs = document.querySelectorAll(".home-program-tab");
    const cards = document.querySelectorAll(".home-program-card");

    if (!tabs.length || !cards.length) return;

    tabs.forEach(tab => {
        tab.addEventListener("click", function () {
            const filter = tab.getAttribute("data-program-filter");

            tabs.forEach(x => x.classList.remove("active"));
            tab.classList.add("active");

            cards.forEach(card => {
                const category = card.getAttribute("data-program-category");

                if (filter === "all" || category === filter) {
                    card.style.display = "";
                } else {
                    card.style.display = "none";
                }
            });
        });
    });
})();
