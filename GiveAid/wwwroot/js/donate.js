// =====================================================================
//  Give-AID - donation form
//  1. Live summary (cause, amount, card type)
//  2. Card number formatting + brand detection
//  3. Client-side card validation (same rules as Helpers/CardValidator.cs):
//     brand, length, Luhn check-digit, expiry MM/YY, CVV, name on card
// =====================================================================
(function () {
    "use strict";

    var form = document.getElementById("donateForm");
    if (!form) return;

    var amount = document.getElementById("Amount");
    var number = document.getElementById("CardNumber");
    var holder = document.getElementById("CardHolder");
    var expiry = document.getElementById("Expiry");
    var cvv = document.getElementById("Cvv");
    var badge = document.getElementById("brandBadge");

    // ---------- helpers ----------
    function digitsOnly(s) { return (s || "").replace(/\D/g, ""); }

    function detectBrand(d) {
        if (/^4/.test(d)) return "Visa";
        if (/^(5[1-5]|2(2[2-9]|[3-6]\d|7[01]|720))/.test(d)) return "MasterCard";
        if (/^3[47]/.test(d)) return "American Express";
        if (/^(6011|65|64[4-9])/.test(d)) return "Discover";
        if (/^62/.test(d)) return "UnionPay";
        return "";
    }

    function lengthsFor(brand) {
        return { "American Express": [15], "Visa": [13, 16, 19], "UnionPay": [16, 17, 18, 19] }[brand] || [16];
    }

    // Luhn algorithm: double every second digit from the right; total must be divisible by 10
    function luhn(d) {
        var sum = 0, dbl = false;
        for (var i = d.length - 1; i >= 0; i--) {
            var n = +d[i];
            if (dbl) { n *= 2; if (n > 9) n -= 9; }
            sum += n; dbl = !dbl;
        }
        return sum % 10 === 0;
    }

    function formatRupees(n) { return "Rs " + Math.round(n || 0).toLocaleString("en-US"); }

    function showError(input, message) {
        var span = document.getElementById(input.id + "-error") ||
                   form.querySelector('[data-valmsg-for="' + input.name + '"]');
        input.classList.toggle("input-validation-error", !!message);
        input.setAttribute("aria-invalid", message ? "true" : "false");
        if (span) {
            span.textContent = message || "";
            span.className = message ? "field-validation-error text-danger" : "field-validation-valid text-danger";
        }
    }

    // ---------- validation rules (return "" when valid) ----------
    function checkNumber() {
        var d = digitsOnly(number.value), brand = detectBrand(d);
        if (!/^\d{12,19}$/.test(d)) return "Enter a valid card number (digits only).";
        if (!brand) return "Card type not supported. Use Visa, MasterCard, American Express, Discover or UnionPay.";
        if (lengthsFor(brand).indexOf(d.length) === -1) return "A " + brand + " card number cannot have " + d.length + " digits.";
        if (!luhn(d)) return "This card number is not valid (check-digit failed).";
        return "";
    }
    function checkHolder() {
        return /^[A-Za-z][A-Za-z .'-]{1,98}$/.test(holder.value.trim()) ? "" : "Enter the name exactly as printed on the card (letters only).";
    }
    function checkExpiry() {
        var m = /^\s*(0[1-9]|1[0-2])\s*\/\s*(\d{2})\s*$/.exec(expiry.value);
        if (!m) return "Use the format MM/YY.";
        var month = +m[1], year = 2000 + (+m[2]);
        var endOfMonth = new Date(year, month, 0, 23, 59, 59);   // last day of expiry month
        var now = new Date();
        if (endOfMonth < now) return "This card has expired.";
        if (year > now.getFullYear() + 20) return "Expiry year is not valid.";
        return "";
    }
    function checkCvv() {
        var len = detectBrand(digitsOnly(number.value)) === "American Express" ? 4 : 3;
        return new RegExp("^\\d{" + len + "}$").test(cvv.value) ? "" : "CVV must be " + len + " digits.";
    }
    var rules = [[number, checkNumber], [holder, checkHolder], [expiry, checkExpiry], [cvv, checkCvv]];

    // ---------- live formatting ----------
    number.addEventListener("input", function () {
        var d = digitsOnly(number.value).slice(0, 19);
        var brand = detectBrand(d);
        // Amex groups 4-6-5, others 4-4-4-4
        number.value = brand === "American Express"
            ? d.replace(/^(\d{0,4})(\d{0,6})(\d{0,5}).*/, function (_, a, b, c) { return [a, b, c].filter(Boolean).join(" "); })
            : d.replace(/(\d{4})(?=\d)/g, "$1 ");
        badge.hidden = !brand;
        badge.textContent = brand;
        cvv.maxLength = brand === "American Express" ? 4 : 3;
    });

    expiry.addEventListener("input", function (e) {
        var d = digitsOnly(expiry.value).slice(0, 4);
        if (d.length === 1 && +d > 1) d = "0" + d;          // "4" -> "04"
        expiry.value = d.length > 2 ? d.slice(0, 2) + "/" + d.slice(2) : d;
    });
    cvv.addEventListener("input", function () { cvv.value = digitsOnly(cvv.value); });

    // validate each field when the user leaves it
    rules.forEach(function (r) {
        r[0].addEventListener("blur", function () { if (r[0].value) showError(r[0], r[1]()); });
    });

    // ---------- live 3D card preview ----------
    var card3d = document.getElementById("card3d");
    function updateCard3d() {
        if (!card3d) return;
        var d = digitsOnly(number.value), brand = detectBrand(d);
        var shown = number.value || "•••• •••• •••• ••••";
        document.getElementById("c3Number").textContent = shown;
        document.getElementById("c3Holder").textContent = holder.value.trim() || "Your name";
        document.getElementById("c3Expiry").textContent = expiry.value || "MM/YY";
        document.getElementById("c3Brand").textContent = brand || "Give-AID";
        document.getElementById("c3Cvv").textContent = cvv.value ? cvv.value.replace(/./g, "•") : "•••";
        card3d.setAttribute("data-brand", brand);
    }
    [number, holder, expiry, cvv].forEach(function (el) { el.addEventListener("input", updateCard3d); });
    cvv.addEventListener("focus", function () { if (card3d) card3d.classList.add("flipped"); });
    cvv.addEventListener("blur", function () { if (card3d) card3d.classList.remove("flipped"); });
    updateCard3d();

    // ---------- amount presets ----------
    document.querySelectorAll("#amountPresets [data-amount]").forEach(function (btn) {
        btn.addEventListener("click", function () {
            amount.value = btn.getAttribute("data-amount");
            amount.dispatchEvent(new Event("input", { bubbles: true }));
            amount.dispatchEvent(new Event("change", { bubbles: true }));
        });
    });

    // ---------- live summary ----------
    function updateSummary() {
        var chosen = form.querySelector('input[name="CauseId"]:checked');
        document.getElementById("sumCause").textContent = chosen ? chosen.getAttribute("data-name") : "Not chosen";
        document.getElementById("sumAmount").textContent = formatRupees(parseFloat(amount.value));
        var ct = form.querySelector('input[name="CardType"]:checked');
        var brand = detectBrand(digitsOnly(number.value));
        document.getElementById("sumCard").textContent = (brand ? brand + " " : "") + (ct ? ct.value.toLowerCase() : "credit") + " card";
        document.querySelectorAll("#amountPresets [data-amount]").forEach(function (b) {
            var on = b.getAttribute("data-amount") === amount.value;
            b.classList.toggle("btn-ga", on);
            b.classList.toggle("btn-outline-ga", !on);
            b.setAttribute("aria-pressed", on ? "true" : "false");
        });
    }
    // ---------- 3D coin stack: more coins for bigger gifts ----------
    var stack = document.getElementById("coinStack");
    function updateCoins() {
        if (!stack) return;
        var amt = parseFloat(amount.value) || 0;
        var want = amt <= 0 ? 0 : Math.max(1, Math.min(14, Math.round(Math.log10(amt) * 4 - 6)));
        while (stack.children.length < want) {
            var c = document.createElement("span");
            c.className = "coin3d";
            c.style.setProperty("--i", stack.children.length);
            stack.appendChild(c);
        }
        while (stack.children.length > want) stack.removeChild(stack.lastChild);
    }
    amount.addEventListener("input", updateCoins);
    amount.addEventListener("change", updateCoins);
    updateCoins();

    form.addEventListener("input", updateSummary);
    form.addEventListener("change", updateSummary);
    updateSummary();

    // ---------- on submit: run card rules (jQuery unobtrusive handles the other fields) ----------
    form.addEventListener("submit", function (e) {
        var firstBad = null;
        rules.forEach(function (r) {
            var msg = r[1]();
            showError(r[0], msg);
            if (msg && !firstBad) firstBad = r[0];
        });
        if (firstBad) {
            e.preventDefault();
            firstBad.focus();
        }
    });
})();
