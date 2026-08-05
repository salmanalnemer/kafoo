(function () {
    var path = window.location.pathname.toLowerCase();
    if (path.indexOf('/portal/donor/contributions/details') >= 0 || path.indexOf('/portal/contributions/details') >= 0) {
        document.body.classList.add('donor-contribution-details-page');
    }
})();
