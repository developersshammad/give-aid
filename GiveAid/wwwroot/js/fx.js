// =====================================================================
//  Give-AID - 3D effects for the public website (no libraries needed)
//  1. Preloader, scroll progress bar, sticky-nav shadow, pointer light
//  2. 3D tilt + glare on cards
//  3. 3D scroll reveal
//  4. Animated counters
//  5. 3D ring carousel (home gallery)
//  6. Infinite 3D logo marquee
//  7. Confetti burst (receipt page)
//  Everything is skipped when the visitor prefers reduced motion.
// =====================================================================
(function () {
    "use strict";

    var reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var finePointer = window.matchMedia("(hover: hover) and (pointer: fine)").matches;
    var root = document.documentElement;

    // ---------- 1. Preloader ----------
    function hidePreloader() {
        var p = document.querySelector(".preloader");
        if (p) p.classList.add("done");
    }
    window.addEventListener("load", hidePreloader);
    setTimeout(hidePreloader, 2500);   // never block the page for long

    // ---------- 1a. Headlines: every letter flips in, in 3D ----------
    if (!reduce) {
        document.querySelectorAll(".type-3d, .page-head h1").forEach(function (h) {
            var text = h.textContent.trim();
            h.setAttribute("aria-label", text);
            h.textContent = "";
            var n = 0;
            text.split(" ").forEach(function (word, w) {
                var ws = document.createElement("span");
                ws.className = "word";
                ws.setAttribute("aria-hidden", "true");
                word.split("").forEach(function (ch) {
                    var cs = document.createElement("span");
                    cs.className = "char";
                    cs.textContent = ch;
                    cs.style.setProperty("--n", n++);
                    ws.appendChild(cs);
                });
                h.appendChild(ws);
                if (w < text.split(" ").length - 1) h.appendChild(document.createTextNode(" "));
            });
            h.classList.add("split");
        });
    }

    // ---------- 1b. Scroll progress + nav shadow ----------
    var nav = document.querySelector(".site-nav");
    function onScroll() {
        var max = document.documentElement.scrollHeight - window.innerHeight;
        root.style.setProperty("--progress", max > 0 ? (window.scrollY / max).toFixed(4) : 0);
        if (nav) nav.classList.toggle("scrolled", window.scrollY > 10);
    }
    window.addEventListener("scroll", onScroll, { passive: true });
    onScroll();

    // ---------- 1c. Soft light that follows the pointer ----------
    if (finePointer && !reduce) {
        window.addEventListener("pointermove", function (e) {
            root.style.setProperty("--mx", e.clientX + "px");
            root.style.setProperty("--my", e.clientY + "px");
        }, { passive: true });
    }

    // ---------- 2. 3D tilt + glare ----------
    // Any element matching these selectors (or with data-tilt) leans toward the pointer.
    var tiltSelector = "[data-tilt], .cause-tile, .prog-card, .gallery-item, .partner-row, .ngo-row, .stat-row, .quick-give, .logo-strip > div";
    function addTilt(el) {
        var max = parseFloat(el.getAttribute("data-tilt")) || (el.classList.contains("quick-give") ? 6 : 10);
        el.classList.add("tilt");
        var glare = document.createElement("span");
        glare.className = "glare";
        el.appendChild(glare);
        var frame = 0;
        el.addEventListener("pointermove", function (e) {
            if (e.pointerType !== "mouse") return;
            cancelAnimationFrame(frame);
            frame = requestAnimationFrame(function () {
                var r = el.getBoundingClientRect();
                var x = (e.clientX - r.left) / r.width, y = (e.clientY - r.top) / r.height;
                el.classList.add("tilting");
                el.style.transform = "perspective(900px) rotateX(" + ((0.5 - y) * max).toFixed(2) + "deg) rotateY(" +
                    ((x - 0.5) * max).toFixed(2) + "deg) scale3d(1.02,1.02,1.02)";
                el.style.setProperty("--gx", (x * 100).toFixed(1) + "%");
                el.style.setProperty("--gy", (y * 100).toFixed(1) + "%");
            });
        });
        el.addEventListener("pointerleave", function () {
            cancelAnimationFrame(frame);
            el.classList.remove("tilting");
            el.style.transform = "";
        });
    }
    if (finePointer && !reduce) document.querySelectorAll(tiltSelector).forEach(addTilt);

    // ---------- 3. 3D scroll reveal ----------
    var revealSelector = ".section-head, .cause-tile, .prog-card, .gift-list, .gallery-item, .partner-row, .ngo-row, " +
                         ".panel, .stat-row, .table-wrap, .prose, .logo-strip, .ring-stage, .receipt, .summary-box, .side-nav";
    if (!reduce && "IntersectionObserver" in window) {
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) {
                if (en.isIntersecting) {
                    var el = en.target;
                    el.classList.add("in");
                    io.unobserve(el);
                    // once it has landed, drop the reveal classes so the tilt effect reacts instantly
                    setTimeout(function () { el.classList.remove("reveal", "in"); el.style.removeProperty("--delay"); }, 1600);
                }
            });
        }, { threshold: 0.12, rootMargin: "0px 0px -40px 0px" });

        document.querySelectorAll(revealSelector).forEach(function (el) {
            if (el.closest(".hero") || el.closest(".modal")) return;
            // stagger siblings in the same grid / row
            var siblings = el.parentElement ? Array.prototype.indexOf.call(el.parentElement.children, el) : 0;
            el.style.setProperty("--delay", Math.min(siblings, 6) * 0.08 + "s");
            el.classList.add("reveal");
            io.observe(el);
        });
    }

    // ---------- 4. Counters: <strong data-count="269000" data-prefix="Rs "> ----------
    function runCounter(el) {
        var target = parseFloat(el.getAttribute("data-count")) || 0;
        var prefix = el.getAttribute("data-prefix") || "";
        if (reduce) { el.textContent = prefix + target.toLocaleString("en-US"); return; }
        var start = performance.now(), dur = 1800;
        (function step(now) {
            var t = Math.min(1, (now - start) / dur);
            var eased = 1 - Math.pow(1 - t, 4);
            el.textContent = prefix + Math.round(target * eased).toLocaleString("en-US");
            if (t < 1) requestAnimationFrame(step);
        })(start);
    }
    var counters = document.querySelectorAll("[data-count]");
    if (counters.length && "IntersectionObserver" in window) {
        var cio = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) { if (en.isIntersecting) { runCounter(en.target); cio.unobserve(en.target); } });
        }, { threshold: 0.4 });
        counters.forEach(function (c) { cio.observe(c); });
    }

    // ---------- 5. 3D ring carousel ----------
    document.querySelectorAll("[data-ring]").forEach(function (stage) {
        var ring = stage.querySelector(".ring");
        var items = ring.querySelectorAll("figure");
        var n = items.length;
        if (!n) return;
        var step = 360 / n;
        var w = ring.offsetWidth;
        var radius = Math.round((w / 2) / Math.tan(Math.PI / Math.max(n, 3))) + 40;
        items.forEach(function (f, i) { f.style.transform = "rotateY(" + (i * step) + "deg) translateZ(" + radius + "px)"; });

        var angle = 0, velocity = reduce ? 0 : -0.12, dragging = false, lastX = 0, hover = false, visible = true;
        function render() {
            ring.style.transform = "translateZ(" + (-radius) + "px) rotateX(-6deg) rotateY(" + angle + "deg)";
            // photos at the back of the ring fade into the dark, the front one is brightest
            items.forEach(function (f, i) {
                var facing = Math.cos(((i * step + angle) * Math.PI) / 180);   // 1 = front, -1 = back
                f.style.opacity = (0.25 + 0.75 * (facing + 1) / 2).toFixed(2);
                f.style.zIndex = Math.round((facing + 1) * 50);
            });
        }
        render();

        stage.addEventListener("pointerdown", function (e) { dragging = true; lastX = e.clientX; stage.setPointerCapture(e.pointerId); });
        stage.addEventListener("pointermove", function (e) {
            if (!dragging) return;
            var dx = e.clientX - lastX; lastX = e.clientX;
            angle += dx * 0.35; velocity = dx * 0.35; render();
        });
        stage.addEventListener("pointerup", function () { dragging = false; });
        stage.addEventListener("pointercancel", function () { dragging = false; });
        stage.addEventListener("mouseenter", function () { hover = true; });
        stage.addEventListener("mouseleave", function () { hover = false; });
        // keyboard: arrow keys turn the ring
        stage.setAttribute("tabindex", "0");
        stage.addEventListener("keydown", function (e) {
            if (e.key === "ArrowLeft") { angle += step; render(); e.preventDefault(); }
            if (e.key === "ArrowRight") { angle -= step; render(); e.preventDefault(); }
        });
        if ("IntersectionObserver" in window) {
            new IntersectionObserver(function (en) { visible = en[0].isIntersecting; }).observe(stage);
        }
        if (reduce) return;
        (function loop() {
            if (!dragging && visible) {
                var idle = hover ? 0 : -0.12;
                velocity += (idle - velocity) * 0.03;   // ease back to slow auto-spin
                angle += velocity;
                render();
            }
            requestAnimationFrame(loop);
        })();
    });

    // ---------- 6. Marquee: duplicate items so the loop is seamless ----------
    document.querySelectorAll(".marquee-track").forEach(function (track) {
        Array.prototype.slice.call(track.children).forEach(function (child) {
            var copy = child.cloneNode(true);
            copy.setAttribute("aria-hidden", "true");
            copy.setAttribute("tabindex", "-1");
            track.appendChild(copy);
        });
    });

    // ---------- 7. Confetti (3D-looking spinning paper) ----------
    window.giveAidConfetti = function () {
        if (reduce) return;
        var c = document.createElement("canvas");
        c.className = "confetti-canvas";
        document.body.appendChild(c);
        var ctx = c.getContext("2d");
        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        c.width = innerWidth * dpr; c.height = innerHeight * dpr; ctx.scale(dpr, dpr);
        var colours = ["#F5B342", "#FFD27A", "#34D399", "#7FF0C4", "#ffffff", "#E09A22"];
        var bits = [];
        for (var i = 0; i < 160; i++) {
            bits.push({
                x: innerWidth / 2 + (Math.random() - 0.5) * 120, y: innerHeight * 0.35,
                vx: (Math.random() - 0.5) * 16, vy: -Math.random() * 16 - 6,
                w: 6 + Math.random() * 8, h: 10 + Math.random() * 10,
                rot: Math.random() * 6, vr: (Math.random() - 0.5) * 0.3,
                flip: Math.random() * 6, vf: 0.1 + Math.random() * 0.25,   // "3D" flip = scale on one axis
                col: colours[i % colours.length]
            });
        }
        var t0 = performance.now();
        (function frame(now) {
            ctx.clearRect(0, 0, innerWidth, innerHeight);
            bits.forEach(function (b) {
                b.vy += 0.35; b.vx *= 0.99; b.x += b.vx; b.y += b.vy; b.rot += b.vr; b.flip += b.vf;
                ctx.save();
                ctx.translate(b.x, b.y); ctx.rotate(b.rot); ctx.scale(Math.cos(b.flip), 1);
                ctx.fillStyle = b.col; ctx.fillRect(-b.w / 2, -b.h / 2, b.w, b.h);
                ctx.restore();
            });
            if (now - t0 < 4500) requestAnimationFrame(frame); else c.remove();
        })(t0);
    };
    if (document.querySelector("[data-confetti]")) setTimeout(window.giveAidConfetti, 500);

    // ---------- Confirm dialogs + auto-hide alerts (same as site.js) ----------
    document.addEventListener("submit", function (e) {
        var msg = e.target.getAttribute("data-confirm");
        if (msg && !window.confirm(msg)) e.preventDefault();
    });
    setTimeout(function () {
        document.querySelectorAll(".alert-ga.alert-dismissible").forEach(function (el) {
            if (window.bootstrap) bootstrap.Alert.getOrCreateInstance(el).close();
        });
    }, 9000);
})();
