// =====================================================================
//  Give-AID - WebGL hero scene (three.js + bloom post-processing)
//
//  * A solid glowing marigold heart in the centre.
//  * A sculpture of thousands of light particles around it that MORPHS:
//      heart  ->  globe (the world)  ->  six clusters (the six causes)  -> heart ...
//  * Particles flee from the mouse pointer and spring back.
//  * Click anywhere on the scene: the sculpture explodes and re-assembles.
//  * Six emerald cause orbs circle the heart; lanterns rise in the night.
//  * Scrolling scatters the sculpture and pulls the camera back.
//  * Bloom (UnrealBloomPass) makes everything glow like real light.
// =====================================================================
import * as THREE from "three";
import { EffectComposer } from "three/addons/postprocessing/EffectComposer.js";
import { RenderPass } from "three/addons/postprocessing/RenderPass.js";
import { UnrealBloomPass } from "three/addons/postprocessing/UnrealBloomPass.js";
import { OutputPass } from "three/addons/postprocessing/OutputPass.js";

const host = document.querySelector("[data-hero3d]");
const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

function webglAvailable() {
    try {
        const c = document.createElement("canvas");
        return !!(window.WebGLRenderingContext && (c.getContext("webgl2") || c.getContext("webgl")));
    } catch { return false; }
}

if (host && webglAvailable()) {
    const MARIGOLD = 0xF5B342, EMERALD = 0x34D399, NIGHT = 0x04130E;
    const narrow = () => host.clientWidth < 992;

    // ---------- renderer, scene, camera ----------
    const renderer = new THREE.WebGLRenderer({ antialias: false, powerPreference: "high-performance" });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5));
    renderer.setClearColor(NIGHT, 1);
    renderer.toneMapping = THREE.ACESFilmicToneMapping;   // keeps bright glow from turning white
    renderer.toneMappingExposure = 0.95;
    host.appendChild(renderer.domElement);

    const scene = new THREE.Scene();
    scene.fog = new THREE.FogExp2(NIGHT, 0.04);
    const camera = new THREE.PerspectiveCamera(42, 1, 0.1, 100);
    camera.position.set(0, 0, 14);

    // bloom pipeline: render -> glow -> colour output
    const composer = new EffectComposer(renderer);
    composer.addPass(new RenderPass(scene, camera));
    const bloom = new UnrealBloomPass(new THREE.Vector2(1, 1), 0.8, 0.4, 0.45);
    composer.addPass(bloom);
    composer.addPass(new OutputPass());

    const world = new THREE.Group();
    scene.add(world);

    // ---------- lights ----------
    scene.add(new THREE.AmbientLight(0x406b5c, 0.8));
    const key = new THREE.DirectionalLight(0xffffff, 2.0);
    key.position.set(5, 6, 8);
    scene.add(key);
    const warm = new THREE.PointLight(MARIGOLD, 50, 30, 1.6);
    warm.position.set(0, 0, 3);
    world.add(warm);
    const cool = new THREE.PointLight(EMERALD, 35, 30, 1.6);
    cool.position.set(-6, -3, 2);
    scene.add(cool);

    // ---------- helper: soft round glow texture ----------
    function glowTexture(inner, outer) {
        const c = document.createElement("canvas");
        c.width = c.height = 128;
        const g = c.getContext("2d");
        const grd = g.createRadialGradient(64, 64, 0, 64, 64, 64);
        grd.addColorStop(0, inner);
        grd.addColorStop(0.35, inner.replace(/[\d.]+\)$/, "0.45)"));
        grd.addColorStop(1, outer);
        g.fillStyle = grd;
        g.fillRect(0, 0, 128, 128);
        const tex = new THREE.CanvasTexture(c);
        tex.colorSpace = THREE.SRGBColorSpace;
        return tex;
    }
    const dotTex = glowTexture("rgba(255,255,255,1)", "rgba(255,255,255,0)");

    // ---------- the solid heart (bezier shape extruded into 3D) ----------
    const s = new THREE.Shape();
    s.moveTo(0, -1.6);
    s.bezierCurveTo(-0.4, -1.2, -2.2, -0.2, -2.2, 0.9);
    s.bezierCurveTo(-2.2, 1.9, -1.3, 2.4, -0.7, 2.4);
    s.bezierCurveTo(-0.2, 2.4, 0, 2.0, 0, 1.75);
    s.bezierCurveTo(0, 2.0, 0.2, 2.4, 0.7, 2.4);
    s.bezierCurveTo(1.3, 2.4, 2.2, 1.9, 2.2, 0.9);
    s.bezierCurveTo(2.2, -0.2, 0.4, -1.2, 0, -1.6);
    const heartGeo = new THREE.ExtrudeGeometry(s, {
        depth: 0.8, bevelEnabled: true, bevelThickness: 0.32, bevelSize: 0.28, bevelSegments: 10, curveSegments: 40
    });
    heartGeo.center();
    const heart = new THREE.Mesh(heartGeo, new THREE.MeshPhysicalMaterial({
        color: MARIGOLD, emissive: 0x7a4300, emissiveIntensity: 0.35,
        metalness: 0.4, roughness: 0.2, clearcoat: 1, clearcoatRoughness: 0.1
    }));
    const HEART_SCALE = 0.5;
    heart.scale.setScalar(HEART_SCALE);
    world.add(heart);

    // ---------- particle sculpture that morphs between shapes ----------
    const N = narrow() ? 3200 : 7000;

    // shape 1: a 3D heart shell (classic heart curve, given thickness)
    function heartPoint(out) {
        const t = Math.random() * Math.PI * 2;
        const x = 16 * Math.pow(Math.sin(t), 3);
        const y = 13 * Math.cos(t) - 5 * Math.cos(2 * t) - 2 * Math.cos(3 * t) - Math.cos(4 * t);
        const r = 0.82 + Math.random() * 0.22;                // shell, slightly fuzzy
        const k = 0.2;                                       // overall size
        const depth = (Math.random() - 0.5) * 2 * 1.5 * Math.sqrt(Math.max(0, 1 - Math.pow((x * x + y * y) / 300, 1)));
        out.set(x * k * r, y * k * r + 0.3, depth);
    }
    // shape 2: a globe drawn with latitude / longitude rings
    function globePoint(out, i) {
        const R = 3.4;
        if (i % 3 === 0) {                                   // longitudes
            const lon = (Math.floor(Math.random() * 12) / 12) * Math.PI * 2, lat = (Math.random() - 0.5) * Math.PI;
            out.set(R * Math.cos(lat) * Math.cos(lon), R * Math.sin(lat), R * Math.cos(lat) * Math.sin(lon));
        } else if (i % 3 === 1) {                            // latitudes
            const lat = ((Math.floor(Math.random() * 7) + 1) / 8 - 0.5) * Math.PI, lon = Math.random() * Math.PI * 2;
            out.set(R * Math.cos(lat) * Math.cos(lon), R * Math.sin(lat), R * Math.cos(lat) * Math.sin(lon));
        } else {                                             // faint surface dust
            const u = Math.random() * 2 - 1, th = Math.random() * Math.PI * 2, rr = R * (0.98 + Math.random() * 0.04);
            out.set(rr * Math.sqrt(1 - u * u) * Math.cos(th), rr * u, rr * Math.sqrt(1 - u * u) * Math.sin(th));
        }
    }
    // shape 3: six glowing clusters (one per cause) on a hexagon
    function causesPoint(out, i) {
        const c = i % 6, a = (c / 6) * Math.PI * 2 + Math.PI / 2;
        const u = Math.random() * 2 - 1, th = Math.random() * Math.PI * 2, rr = 0.85 * Math.cbrt(Math.random());
        out.set(Math.cos(a) * 3.2 + rr * Math.sqrt(1 - u * u) * Math.cos(th),
                Math.sin(a) * 3.2 + rr * u,
                rr * Math.sqrt(1 - u * u) * Math.sin(th));
    }

    const shapes = [heartPoint, globePoint, causesPoint].map((fn) => {
        const arr = new Float32Array(N * 3), v = new THREE.Vector3();
        for (let i = 0; i < N; i++) { fn(v, i); arr.set([v.x, v.y, v.z], i * 3); }
        return arr;
    });

    const pos = new Float32Array(N * 3), vel = new Float32Array(N * 3), col = new Float32Array(N * 3);
    const cGold = new THREE.Color(MARIGOLD), cGreen = new THREE.Color(EMERALD), cWhite = new THREE.Color(0xFFF4DC), tmp = new THREE.Color();
    for (let i = 0; i < N; i++) {
        // start scattered far away, then fly in to form the heart
        pos[i * 3] = (Math.random() - 0.5) * 40; pos[i * 3 + 1] = (Math.random() - 0.5) * 30; pos[i * 3 + 2] = (Math.random() - 0.5) * 30;
        const r = Math.random();
        tmp.copy(r < 0.55 ? cGold : r < 0.9 ? cGreen : cWhite).multiplyScalar(0.9);
        col.set([tmp.r, tmp.g, tmp.b], i * 3);
    }
    const sculptGeo = new THREE.BufferGeometry();
    sculptGeo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    sculptGeo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const sculpture = new THREE.Points(sculptGeo, new THREE.PointsMaterial({
        size: narrow() ? 0.14 : 0.12, map: dotTex, vertexColors: true, transparent: true,
        depthWrite: false, blending: THREE.AdditiveBlending, sizeAttenuation: true
    }));
    world.add(sculpture);

    // ---------- orbit rings + six cause orbs ----------
    const orbits = [];
    const orbGeo = new THREE.IcosahedronGeometry(0.3, 3);
    const ringMat = new THREE.MeshBasicMaterial({ color: EMERALD, transparent: true, opacity: 0.18 });
    for (let i = 0; i < 6; i++) {
        const pivot = new THREE.Group();
        pivot.rotation.set(Math.PI / 2 + (i % 3 - 1) * 0.55, (i * Math.PI) / 3, (i % 2 ? 1 : -1) * 0.35);
        world.add(pivot);
        const radius = 4.4 + (i % 3) * 0.5;
        pivot.add(new THREE.Mesh(new THREE.TorusGeometry(radius, 0.01, 6, 180), ringMat));
        const orb = new THREE.Mesh(orbGeo, new THREE.MeshPhysicalMaterial({
            color: i % 2 ? EMERALD : 0xA7F3D0, emissive: EMERALD, emissiveIntensity: 0.6,
            metalness: 0.2, roughness: 0.15, clearcoat: 1
        }));
        pivot.add(orb);
        // short glowing trail behind each orb
        const trailGeo = new THREE.BufferGeometry().setFromPoints(new Array(24).fill(0).map(() => new THREE.Vector3()));
        const trail = new THREE.Line(trailGeo, new THREE.LineBasicMaterial({ color: EMERALD, transparent: true, opacity: 0.55, blending: THREE.AdditiveBlending }));
        pivot.add(trail);
        orbits.push({ orb, trail, radius, speed: 0.3 + i * 0.05, phase: (i / 6) * Math.PI * 2 });
    }

    // ---------- rising lanterns ----------
    const L = narrow() ? 400 : 900;
    const lpos = new Float32Array(L * 3), lcol = new Float32Array(L * 3), lspeed = new Float32Array(L);
    for (let i = 0; i < L; i++) {
        lpos.set([(Math.random() - 0.5) * 40, (Math.random() - 0.5) * 22, (Math.random() - 0.5) * 20 - 4], i * 3);
        tmp.copy(cGold).lerp(cGreen, Math.random() < 0.7 ? Math.random() * 0.2 : 0.85).multiplyScalar(0.6);
        lcol.set([tmp.r, tmp.g, tmp.b], i * 3);
        lspeed[i] = 0.2 + Math.random() * 0.8;
    }
    const lGeo = new THREE.BufferGeometry();
    lGeo.setAttribute("position", new THREE.BufferAttribute(lpos, 3));
    lGeo.setAttribute("color", new THREE.BufferAttribute(lcol, 3));
    scene.add(new THREE.Points(lGeo, new THREE.PointsMaterial({
        size: 0.13, vertexColors: true, map: dotTex, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending
    })));

    // ---------- layout / resize ----------
    function layout() {
        const w = host.clientWidth, h = host.clientHeight;
        renderer.setSize(w, h, false);
        composer.setSize(w, h);
        bloom.setSize(w, h);
        camera.aspect = w / h;
        camera.updateProjectionMatrix();
        if (narrow()) { world.position.set(1.2, 4.2, -6); world.scale.setScalar(0.7); }
        else { world.position.set(4.7, 1.2, 0); world.scale.setScalar(0.95); }
    }
    layout();
    new ResizeObserver(layout).observe(host);

    // ---------- pointer (projected into the sculpture's space) ----------
    const pointer = { x: 0, y: 0 }, eased = { x: 0, y: 0 };
    const ndc = new THREE.Vector2(), ray = new THREE.Raycaster();
    const plane = new THREE.Plane(new THREE.Vector3(0, 0, 1), 0);
    const hitWorld = new THREE.Vector3(), hitLocal = new THREE.Vector3();
    let pointerActive = false;
    window.addEventListener("pointermove", (e) => {
        pointer.x = (e.clientX / window.innerWidth) * 2 - 1;
        pointer.y = (e.clientY / window.innerHeight) * 2 - 1;
        const r = host.getBoundingClientRect();
        pointerActive = e.clientY >= r.top && e.clientY <= r.bottom;
        ndc.set(((e.clientX - r.left) / r.width) * 2 - 1, -((e.clientY - r.top) / r.height) * 2 + 1);
    }, { passive: true });

    // click on the scene (not on buttons/forms) = explosion
    let beat = 0;
    host.parentElement.addEventListener("click", (e) => {
        if (e.target.closest("a, button, input, select, label, form")) return;
        beat = 1;
        for (let i = 0; i < N; i++) {
            const x = pos[i * 3], y = pos[i * 3 + 1], z = pos[i * 3 + 2];
            const len = Math.hypot(x, y, z) || 1, f = 0.4 + Math.random() * 0.9;
            vel[i * 3] += (x / len) * f; vel[i * 3 + 1] += (y / len) * f; vel[i * 3 + 2] += (z / len) * f;
        }
        for (let i = 0; i < L; i++) lspeed[i] += 3;
    });

    // ---------- morph timeline ----------
    const HOLD = 5.5, MORPH = 2.2;                           // seconds per shape / per transition
    const ease = (t) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);
    function currentTargets(t) {
        const cycle = HOLD + MORPH, total = cycle * shapes.length;
        const local = t % total, idx = Math.floor(local / cycle), within = local - idx * cycle;
        const mix = within < HOLD ? 0 : ease((within - HOLD) / MORPH);
        return { a: shapes[idx], b: shapes[(idx + 1) % shapes.length], mix, idx };
    }

    // ---------- animation loop (pauses when off-screen / tab hidden) ----------
    let running = true;
    new IntersectionObserver((en) => { running = en[0].isIntersecting; if (running) tick(); }).observe(host);
    document.addEventListener("visibilitychange", () => { if (!document.hidden && running) tick(); });

    const clock = new THREE.Clock();
    let raf = 0;
    function tick() {
        cancelAnimationFrame(raf);
        if (!running || document.hidden) return;
        const dt = Math.min(clock.getDelta(), 0.1);           // capped so slow PCs still converge smoothly
        const t = clock.elapsedTime;
        const scroll = Math.min(window.scrollY / window.innerHeight, 1);

        // pointer position in the sculpture's local space (for the "flee" effect)
        let px = 1e9, py = 1e9;
        if (pointerActive && !reduce) {
            ray.setFromCamera(ndc, camera);
            if (ray.ray.intersectPlane(plane, hitWorld)) {
                hitLocal.copy(hitWorld);
                sculpture.worldToLocal(hitLocal);
                px = hitLocal.x; py = hitLocal.y;
            }
        }

        // particles: spring toward the morphing target, flee the pointer, scatter on scroll
        const { a, b, mix, idx } = currentTargets(t);
        const scatter = 1 + scroll * 2.2;
        const f = dt * 60;                                   // physics per 1/60 s, same on fast and slow PCs
        const pull = Math.min(0.035 * f, 0.5), damp = Math.pow(0.88, f);
        for (let i = 0; i < N; i++) {
            const j = i * 3;
            let tx = a[j] + (b[j] - a[j]) * mix, ty = a[j + 1] + (b[j + 1] - a[j + 1]) * mix, tz = a[j + 2] + (b[j + 2] - a[j + 2]) * mix;
            tx *= scatter; ty *= scatter; tz *= scatter;
            // gentle breathing
            const breathe = 1 + Math.sin(t * 1.6 + i * 0.01) * 0.015;
            tx *= breathe; ty *= breathe;

            let vx = vel[j] + (tx - pos[j]) * pull;
            let vy = vel[j + 1] + (ty - pos[j + 1]) * pull;
            let vz = vel[j + 2] + (tz - pos[j + 2]) * pull;

            const dx = pos[j] - px, dy = pos[j + 1] - py, d2 = dx * dx + dy * dy;
            if (d2 < 2.2) { const f = (2.2 - d2) * 0.03; vx += dx * f; vy += dy * f; vz += (Math.random() - 0.5) * f; }

            vx *= damp; vy *= damp; vz *= damp;
            vel[j] = vx; vel[j + 1] = vy; vel[j + 2] = vz;
            pos[j] += vx * f; pos[j + 1] += vy * f; pos[j + 2] += vz * f;
        }
        sculptGeo.attributes.position.needsUpdate = true;
        sculpture.rotation.y = idx === 1 ? t * 0.25 : Math.sin(t * 0.45) * 0.5;   // the globe spins
        sculpture.rotation.z = idx === 2 ? t * 0.15 : 0;                            // the causes wheel turns

        // solid heart: float + heartbeat
        const pulse = 1 + Math.pow(Math.max(0, Math.sin(t * 2.6)), 8) * 0.07 + beat * 0.3;
        beat *= 0.93;
        heart.rotation.y = Math.sin(t * 0.5) * 0.6 + eased.x * 0.4;
        heart.rotation.x = Math.sin(t * 0.35) * 0.12 - eased.y * 0.25;
        heart.position.y = Math.sin(t * 0.9) * 0.18 + 0.3;
        heart.scale.setScalar(HEART_SCALE * pulse);
        bloom.strength = 0.7 + Math.pow(Math.max(0, Math.sin(t * 2.6)), 8) * 0.2 + beat * 0.35;

        // orbs + trails
        orbits.forEach((o) => {
            const ang = o.phase + t * o.speed;
            o.orb.position.set(Math.cos(ang) * o.radius, Math.sin(ang) * o.radius, 0);
            const tp = o.trail.geometry.attributes.position;
            for (let k = 0; k < tp.count; k++) {
                const a2 = ang - k * 0.035;
                tp.setXYZ(k, Math.cos(a2) * o.radius, Math.sin(a2) * o.radius, 0);
            }
            tp.needsUpdate = true;
        });

        // lanterns rise and wrap
        const lp = lGeo.attributes.position.array;
        for (let i = 0; i < L; i++) {
            lp[i * 3 + 1] += lspeed[i] * dt * 0.8;
            lp[i * 3] += Math.sin(t * 0.6 + i) * dt * 0.08;
            if (lspeed[i] > 1) lspeed[i] -= dt * 2;
            if (lp[i * 3 + 1] > 11) lp[i * 3 + 1] = -11;
        }
        lGeo.attributes.position.needsUpdate = true;

        // camera follows the pointer, pulls back with scroll
        eased.x += (pointer.x - eased.x) * 0.04;
        eased.y += (pointer.y - eased.y) * 0.04;
        camera.position.x = eased.x * 1.6;
        camera.position.y = -eased.y * 1.0 - scroll * 2;
        camera.position.z = 14 + scroll * 6;
        camera.lookAt(narrow() ? 0 : 1.4, -scroll * 1.5, 0);

        composer.render();
        if (!reduce) raf = requestAnimationFrame(tick);
    }

    if (reduce) {
        // reduced motion: show the finished heart immediately, no animation
        pos.set(shapes[0]);
        sculptGeo.attributes.position.needsUpdate = true;
    }
    tick();
    host.classList.add("ready");
}
