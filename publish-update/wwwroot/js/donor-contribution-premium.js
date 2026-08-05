(function () {
    const path = window.location.pathname.toLowerCase();
    const isDetails = path.includes('/portal/donor/contributions/details') || path.includes('/portal/contributions/details');
    if (!isDetails) return;

    document.documentElement.classList.add('donor-contribution-premium-root');
    document.body.classList.add('donor-contribution-premium');

    function tagFirstDetailsCard() {
        const stat = document.querySelector('.portal-stat, .stat-card, .metric-card');
        if (!stat) return;
        const card = stat.closest('.portal-card, .portal-panel, .kafo-card, .content-card, section, article');
        if (card) card.classList.add('donor-contribution-hero');
    }

    function normalizeProgress() {
        document.querySelectorAll('[style*="width:"]').forEach(function (el) {
            const style = el.getAttribute('style') || '';
            if (style.includes('%') && (el.className || '').toString().toLowerCase().includes('progress')) {
                el.classList.add('progress-bar');
            }
        });
    }

    function normalizeEmptyBlocks() {
        document.querySelectorAll('div, p').forEach(function (el) {
            const text = (el.textContent || '').trim();
            if (!text) return;
            if (text.includes('لا توجد تقارير') || text.includes('لا توجد تحديثات') || text.includes('لا توجد')) {
                if (text.length < 90) el.classList.add('portal-empty');
            }
        });
    }

    function markCodeCards() {
        document.querySelectorAll('strong, span').forEach(function (el) {
            const txt = (el.textContent || '').trim();
            if (/^KAFD\d{6}$/.test(txt)) {
                el.setAttribute('dir', 'ltr');
                el.classList.add('kafd-code');
            }
        });
    }

    function run() {
        tagFirstDetailsCard();
        normalizeProgress();
        normalizeEmptyBlocks();
        markCodeCards();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', run);
    } else {
        run();
    }
})();
