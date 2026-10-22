// =====================================================================
//  Give-AID admin - Nova helpers (no libraries)
//  - Night / Day theme switch (remembered in this browser)
//  - Live Karachi clock in the top bar
//  - Command palette: Ctrl + K (or "/") to jump to any page or action
//  - Dashboard: one entrance animation per session + numbers counting up
//  - Soft light that follows the pointer on the key-number cards
// =====================================================================
(function () {
    "use strict";
    var root = document.documentElement;
    var REDUCE = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var store = {
        get: function (k) { try { return localStorage.getItem(k); } catch (e) { return null; } },
        set: function (k, v) { try { localStorage.setItem(k, v); } catch (e) { /* private mode */ } }
    };

    // ---------- theme ----------
    function applyTheme(night) {
        root.classList.toggle("nova", night);
        root.setAttribute("data-bs-theme", night ? "dark" : "light");
        document.querySelectorAll("[data-theme-toggle]").forEach(function (b) {
            b.setAttribute("aria-pressed", String(night));
            b.innerHTML = night ? '<i class="bi bi-sun"></i><span class="lbl">Day</span>' : '<i class="bi bi-moon-stars"></i><span class="lbl">Night</span>';
            b.title = night ? "Switch to day mode" : "Switch to night mode";
        });
        document.dispatchEvent(new CustomEvent("ga:theme", { detail: { night: night } }));
    }
    function toggleTheme() {
        var night = !root.classList.contains("nova");
        store.set("ga-admin-theme", night ? "nova" : "day");
        applyTheme(night);
    }
    document.addEventListener("click", function (e) {
        if (e.target.closest("[data-theme-toggle]")) toggleTheme();
    });
    applyTheme(root.classList.contains("nova"));

    // ---------- clock ----------
    var clocks = document.querySelectorAll("[data-hud-clock]");
    if (clocks.length) {
        var fmt;
        try { fmt = new Intl.DateTimeFormat("en-GB", { timeZone: "Asia/Karachi", weekday: "short", hour: "2-digit", minute: "2-digit", second: "2-digit" }); }
        catch (e) { fmt = new Intl.DateTimeFormat("en-GB", { weekday: "short", hour: "2-digit", minute: "2-digit", second: "2-digit" }); }
        var tick = function () { var s = fmt.format(new Date()).replace(",", ""); clocks.forEach(function (c) { c.querySelector("span").textContent = s + " PKT"; }); };
        tick(); setInterval(tick, 1000);
    }

    // ---------- pointer light on cards ----------
    document.addEventListener("pointermove", function (e) {
        var card = e.target.closest && e.target.closest(".kpi");
        if (!card) return;
        var r = card.getBoundingClientRect();
        card.style.setProperty("--mx", (e.clientX - r.left) + "px");
        card.style.setProperty("--my", (e.clientY - r.top) + "px");
    }, { passive: true });

    // ---------- dashboard entrance (once per session) ----------
    var grid = document.querySelector(".kpi-grid");
    var seen = false;
    try { seen = sessionStorage.getItem("ga-boot") === "1"; sessionStorage.setItem("ga-boot", "1"); } catch (e) { /* ignore */ }
    if (grid && !seen && !REDUCE) {
        var content = document.querySelector(".admin-content");
        if (content) content.classList.add("boot");
        document.querySelectorAll(".kpi-value").forEach(function (el) {
            var text = el.textContent.trim(), m = text.match(/^(\D*)([\d,]+)(\D*)$/);
            if (!m) return;
            var target = parseInt(m[2].replace(/,/g, ""), 10);
            if (!(target > 0)) return;
            var start = performance.now(), dur = 1100;
            (function step(now) {
                var p = Math.min(1, (now - start) / dur), v = Math.round(target * (1 - Math.pow(1 - p, 3)));
                el.textContent = m[1] + v.toLocaleString("en-US") + m[3];
                if (p < 1) requestAnimationFrame(step); else el.textContent = text;
            })(start);
        });
    }

    // ---------- command palette ----------
    var actions = [
        { t: "Plan a programme with AI", i: "stars", h: "/Admin/Planner", g: "Actions", k: "planner ai plan" },
        { t: "Add a programme", i: "calendar-plus", h: "/Admin/Programmes/Create", g: "Actions", k: "new programme event" },
        { t: "Add a cause", i: "heart", h: "/Admin/Causes/Create", g: "Actions", k: "new cause" },
        { t: "Add an NGO", i: "building-add", h: "/Admin/Ngos/Create", g: "Actions", k: "new ngo" },
        { t: "Add a partner", i: "briefcase", h: "/Admin/Partners/Create", g: "Actions", k: "new partner" },
        { t: "Upload a photo", i: "upload", h: "/Admin/Gallery", g: "Actions", k: "gallery image" },
        { t: "Reply to queries", i: "chat-left-dots", h: "/Admin/Queries", g: "Actions", k: "support message" },
        { t: "Switch day / night theme", i: "circle-half", run: toggleTheme, g: "Actions", k: "dark light mode theme" },
        { t: "Open the website", i: "box-arrow-up-right", h: "/", g: "Actions", k: "site public home", blank: true }
    ];
    function pages() {
        return Array.prototype.map.call(document.querySelectorAll(".admin-menu a"), function (a) {
            var ic = a.querySelector("i");
            return { t: a.textContent.trim(), i: ic ? ic.className.replace(/^bi bi-/, "") : "arrow-right", h: a.getAttribute("href"), g: "Go to", k: "" };
        });
    }
    var box = null, input, list, items = [], sel = 0, lastFocus = null;
    function build() {
        box = document.createElement("div");
        box.className = "cmdk"; box.hidden = true;
        box.innerHTML = '<div class="cmdk-box" role="dialog" aria-modal="true" aria-label="Command palette">' +
            '<div class="cmdk-search"><i class="bi bi-search"></i><input type="text" placeholder="Search pages and actions…" aria-label="Search pages and actions" role="combobox" aria-expanded="true" aria-controls="cmdkList" autocomplete="off" spellcheck="false" /></div>' +
            '<ul class="cmdk-list" id="cmdkList" role="listbox"></ul>' +
            '<div class="cmdk-foot"><span><kbd>↑</kbd> <kbd>↓</kbd> choose</span><span><kbd>Enter</kbd> open</span><span><kbd>Esc</kbd> close</span></div></div>';
        document.body.appendChild(box);
        input = box.querySelector("input"); list = box.querySelector("ul");
        input.addEventListener("input", draw);
        box.addEventListener("click", function (e) {
            if (e.target === box) return close();
            var li = e.target.closest(".cmdk-item");
            if (li) run(items[+li.getAttribute("data-i")]);
        });
        box.addEventListener("keydown", function (e) {
            if (e.key === "Escape") { e.preventDefault(); close(); }
            else if (e.key === "ArrowDown") { e.preventDefault(); move(1); }
            else if (e.key === "ArrowUp") { e.preventDefault(); move(-1); }
            else if (e.key === "Enter") { e.preventDefault(); if (items[sel]) run(items[sel]); }
            else if (e.key === "Tab") { e.preventDefault(); input.focus(); }
        });
    }
    function esc(s) { return String(s).replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
    function draw() {
        var q = input.value.trim().toLowerCase();
        var all = pages().concat(actions);
        items = all.filter(function (x) { return !q || (x.t + " " + x.k).toLowerCase().indexOf(q) >= 0 || q.split(/\s+/).every(function (w) { return (x.t + " " + x.k).toLowerCase().indexOf(w) >= 0; }); });
        sel = 0;
        if (!items.length) { list.innerHTML = '<li class="cmdk-empty">Nothing matches "' + esc(input.value) + '". Try "programme" or "donations".</li>'; input.removeAttribute("aria-activedescendant"); return; }
        var html = "", group = "";
        items.forEach(function (x, i) {
            if (x.g !== group) { group = x.g; html += '<li class="cmdk-group" role="presentation">' + esc(group) + "</li>"; }
            html += '<li class="cmdk-item" role="option" id="cmdk-' + i + '" data-i="' + i + '" aria-selected="' + (i === 0) + '"><i class="bi bi-' + esc(x.i) + '"></i>' + esc(x.t) + (x.g === "Go to" ? "<small>page</small>" : "") + "</li>";
        });
        list.innerHTML = html;
        input.setAttribute("aria-activedescendant", "cmdk-0");
    }
    function move(d) {
        if (!items.length) return;
        sel = (sel + d + items.length) % items.length;
        list.querySelectorAll(".cmdk-item").forEach(function (li) { li.setAttribute("aria-selected", String(+li.getAttribute("data-i") === sel)); });
        var cur = list.querySelector("#cmdk-" + sel);
        if (cur) { cur.scrollIntoView({ block: "nearest" }); input.setAttribute("aria-activedescendant", cur.id); }
    }
    function run(x) {
        close();
        if (x.run) x.run();
        else if (x.blank) window.open(x.h, "_blank");
        else location.href = x.h;
    }
    function open() {
        if (!box) build();
        lastFocus = document.activeElement;
        box.hidden = false; input.value = ""; draw(); input.focus();
    }
    function close() {
        if (!box || box.hidden) return;
        box.hidden = true;
        if (lastFocus && lastFocus.focus) lastFocus.focus();
    }
    window.GA_CMDK = { open: open, close: close };
    document.addEventListener("click", function (e) { if (e.target.closest("[data-cmdk-open]")) { e.preventDefault(); open(); } });
    document.addEventListener("keydown", function (e) {
        var typing = /^(INPUT|TEXTAREA|SELECT)$/.test((e.target.tagName || "")) || e.target.isContentEditable;
        if ((e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K")) { e.preventDefault(); if (box && !box.hidden) close(); else open(); }
        else if (e.key === "/" && !typing && (!box || box.hidden)) { e.preventDefault(); open(); }
    });
})();
