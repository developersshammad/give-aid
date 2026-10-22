// =====================================================================
//  Give-AID - small helpers used on every page
// =====================================================================
(function () {
    "use strict";

    // Confirm before any form marked data-confirm is submitted (e.g. delete buttons)
    document.addEventListener("submit", function (e) {
        var msg = e.target.getAttribute("data-confirm");
        if (msg && !window.confirm(msg)) e.preventDefault();
    });

    // Auto-hide success messages after 8 seconds
    setTimeout(function () {
        document.querySelectorAll(".alert-ga.alert-dismissible").forEach(function (el) {
            if (window.bootstrap) bootstrap.Alert.getOrCreateInstance(el).close();
        });
    }, 8000);
})();
