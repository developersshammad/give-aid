// =====================================================================
//  Give-AID admin - "Nova" 3D background
//  A slowly turning constellation: every point is a donor or volunteer,
//  the lines are the NGOs that connect them, and a glowing horizon grid
//  sits underneath. Runs only in the Nova (night) theme, pauses when the
//  tab is hidden, and draws one still frame for reduced-motion users.
// =====================================================================
import * as THREE from "three";

const host = document.querySelector("[data-nova3d]");
const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

if (host) {
    let renderer = null;
    try { renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: "low-power" }); } catch { renderer = null; }

    if (renderer) {
        renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.5));
        host.appendChild(renderer.domElement);
        const scene = new THREE.Scene();
        scene.fog = new THREE.FogExp2(0x03100D, 0.045);
        const camera = new THREE.PerspectiveCamera(58, 1, 0.1, 120);
        camera.position.set(0, 1.2, 14);

        // ---------- the constellation ----------
        const small = window.innerWidth < 768;
        const N = small ? 110 : 220;
        const pts = [];
        for (let i = 0; i < N; i++) {
            // points on a thick shell, flattened a little (a galaxy more than a ball)
            const u = Math.random() * 2 - 1, th = Math.random() * Math.PI * 2, r = 4.2 + Math.random() * 2.6;
            const s = Math.sqrt(1 - u * u);
            pts.push(new THREE.Vector3(r * s * Math.cos(th), r * u * 0.62, r * s * Math.sin(th)));
        }
        const pos = new Float32Array(N * 3), col = new Float32Array(N * 3);
        const gold = new THREE.Color(0xF5B342), mint = new THREE.Color(0x45E0B4), pale = new THREE.Color(0xCFF5E8);
        pts.forEach((p, i) => {
            pos.set([p.x, p.y, p.z], i * 3);
            const c = i % 9 === 0 ? gold : i % 3 === 0 ? pale : mint;
            col.set([c.r, c.g, c.b], i * 3);
        });
        const pGeo = new THREE.BufferGeometry();
        pGeo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
        pGeo.setAttribute("color", new THREE.BufferAttribute(col, 3));

        // soft round sprite drawn on a canvas (no image file needed)
        const cv = document.createElement("canvas"); cv.width = cv.height = 64;
        const g2 = cv.getContext("2d"), grad = g2.createRadialGradient(32, 32, 0, 32, 32, 32);
        grad.addColorStop(0, "rgba(255,255,255,1)"); grad.addColorStop(0.25, "rgba(255,255,255,.85)"); grad.addColorStop(1, "rgba(255,255,255,0)");
        g2.fillStyle = grad; g2.fillRect(0, 0, 64, 64);
        const dotTex = new THREE.CanvasTexture(cv);
        const points = new THREE.Points(pGeo, new THREE.PointsMaterial({
            size: 0.22, map: dotTex, vertexColors: true, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending
        }));

        // lines between near neighbours
        const seg = [];
        for (let i = 0; i < N; i++) {
            let links = 0;
            for (let j = i + 1; j < N && links < 3; j++) {
                if (pts[i].distanceToSquared(pts[j]) < 2.1) { seg.push(pts[i].x, pts[i].y, pts[i].z, pts[j].x, pts[j].y, pts[j].z); links++; }
            }
        }
        const lGeo = new THREE.BufferGeometry();
        lGeo.setAttribute("position", new THREE.Float32BufferAttribute(seg, 3));
        const lines = new THREE.LineSegments(lGeo, new THREE.LineBasicMaterial({ color: 0x45E0B4, transparent: true, opacity: 0.16, blending: THREE.AdditiveBlending, depthWrite: false }));

        // a glowing core and an orbit ring
        const core = new THREE.Mesh(new THREE.IcosahedronGeometry(1.15, 1),
            new THREE.MeshBasicMaterial({ color: 0xF5B342, wireframe: true, transparent: true, opacity: 0.35 }));
        const ring = new THREE.Mesh(new THREE.TorusGeometry(7.4, 0.012, 6, 160),
            new THREE.MeshBasicMaterial({ color: 0xF5B342, transparent: true, opacity: 0.35 }));
        ring.rotation.x = Math.PI / 2.25;
        const ring2 = ring.clone(); ring2.material = ring.material.clone(); ring2.material.color.set(0x45E0B4); ring2.material.opacity = 0.22;
        ring2.scale.setScalar(1.18); ring2.rotation.x = Math.PI / 1.85; ring2.rotation.y = 0.5;

        const galaxy = new THREE.Group();
        galaxy.add(points, lines, core, ring, ring2);
        const login = !!document.querySelector(".admin-login");
        galaxy.position.set(small || login ? 0 : 4.5, login ? 0.9 : 0.6, login ? -1 : -2);
        if (login) galaxy.scale.setScalar(1.25);
        scene.add(galaxy);

        // horizon grid
        const grid = new THREE.GridHelper(120, 80, 0x45E0B4, 0x1A5A47);
        grid.material.transparent = true; grid.material.opacity = 0.22; grid.material.depthWrite = false;
        grid.position.y = -5.2;
        scene.add(grid);

        // far dust
        const D = small ? 300 : 700, dpos = new Float32Array(D * 3);
        for (let i = 0; i < D; i++) dpos.set([(Math.random() - 0.5) * 90, Math.random() * 40 - 6, -Math.random() * 70 + 10], i * 3);
        const dGeo = new THREE.BufferGeometry(); dGeo.setAttribute("position", new THREE.BufferAttribute(dpos, 3));
        const dust = new THREE.Points(dGeo, new THREE.PointsMaterial({ size: 0.08, color: 0x9FF0D6, transparent: true, opacity: 0.55, depthWrite: false }));
        scene.add(dust);

        function resize() {
            const w = window.innerWidth, h = window.innerHeight;
            renderer.setSize(w, h, false);
            camera.aspect = w / h;
            camera.updateProjectionMatrix();
        }
        resize();
        window.addEventListener("resize", resize);

        let mx = 0, my = 0;
        window.addEventListener("pointermove", (e) => { mx = e.clientX / window.innerWidth - 0.5; my = e.clientY / window.innerHeight - 0.5; }, { passive: true });

        const active = () => document.documentElement.classList.contains("nova") && !document.hidden;
        const clock = new THREE.Clock();
        let raf = 0;
        function frame() {
            cancelAnimationFrame(raf);
            if (!active()) return;
            const dt = Math.min(clock.getDelta(), 0.05), t = clock.elapsedTime;
            galaxy.rotation.y += dt * 0.06;
            core.rotation.x += dt * 0.25; core.rotation.y -= dt * 0.18;
            ring.rotation.z += dt * 0.05; ring2.rotation.z -= dt * 0.04;
            grid.position.z = (t * 0.9) % (120 / 80);                       // endless forward motion
            dust.rotation.y = t * 0.004;
            const sy = Math.min(window.scrollY / 900, 1);
            camera.position.x += (mx * 1.6 - camera.position.x) * 0.035;
            camera.position.y += (1.2 - my * 1.1 - sy * 1.5 - camera.position.y) * 0.035;
            camera.lookAt(galaxy.position.x * 0.4, 0, -4);
            renderer.render(scene, camera);
            if (!reduce) raf = requestAnimationFrame(frame);
        }
        document.addEventListener("visibilitychange", frame);
        document.addEventListener("ga:theme", () => { clock.getDelta(); frame(); });
        frame();
        requestAnimationFrame(() => host.classList.add("on"));
    }
}
