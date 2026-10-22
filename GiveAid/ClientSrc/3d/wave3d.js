// =====================================================================
//  Give-AID - WebGL particle wave behind every inner page header
//  A field of glowing points ripples like water ("ripples of giving").
// =====================================================================
import * as THREE from "three";

const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

document.querySelectorAll("[data-wave3d]").forEach((host) => {
    let renderer;
    try { renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true }); } catch { return; }
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5));
    host.appendChild(renderer.domElement);
    renderer.domElement.style.cssText = "display:block;width:100%;height:100%";

    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 100);
    camera.position.set(0, 4.2, 9);
    camera.lookAt(0, 0, -2);

    // grid of points on the x/z plane
    const COLS = 110, ROWS = 36, GAP = 0.32;
    const count = COLS * ROWS;
    const pos = new Float32Array(count * 3), col = new Float32Array(count * 3);
    const a = new THREE.Color(0x34D399), b = new THREE.Color(0xF5B342), c = new THREE.Color();
    for (let i = 0, k = 0; i < COLS; i++) {
        for (let j = 0; j < ROWS; j++, k++) {
            pos[k * 3] = (i - COLS / 2) * GAP;
            pos[k * 3 + 2] = -j * GAP * 1.4 + 2;
            c.copy(a).lerp(b, Math.pow(i / COLS, 2.2));
            col.set([c.r, c.g, c.b], k * 3);
        }
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const dot = document.createElement("canvas");
    dot.width = dot.height = 64;
    const g = dot.getContext("2d");
    const grd = g.createRadialGradient(32, 32, 0, 32, 32, 32);
    grd.addColorStop(0, "rgba(255,255,255,1)"); grd.addColorStop(1, "rgba(255,255,255,0)");
    g.fillStyle = grd; g.fillRect(0, 0, 64, 64);
    const points = new THREE.Points(geo, new THREE.PointsMaterial({
        size: 0.11, vertexColors: true, map: new THREE.CanvasTexture(dot), transparent: true,
        depthWrite: false, blending: THREE.AdditiveBlending
    }));
    scene.add(points);

    function resize() {
        const w = host.clientWidth, h = host.clientHeight;
        renderer.setSize(w, h, false);
        camera.aspect = w / h;
        camera.updateProjectionMatrix();
    }
    resize();
    new ResizeObserver(resize).observe(host);

    let mouseX = 0;
    window.addEventListener("pointermove", (e) => { mouseX = e.clientX / window.innerWidth - 0.5; }, { passive: true });

    let visible = true, raf = 0;
    new IntersectionObserver((en) => { visible = en[0].isIntersecting; if (visible) loop(); }).observe(host);

    const clock = new THREE.Clock();
    function loop() {
        cancelAnimationFrame(raf);
        if (!visible) return;
        const t = clock.getElapsedTime();
        const p = geo.attributes.position.array;
        for (let k = 0; k < count; k++) {
            const x = p[k * 3], z = p[k * 3 + 2];
            p[k * 3 + 1] = Math.sin(x * 0.45 + t * 1.1) * 0.45 + Math.cos(z * 0.6 + t * 0.8) * 0.35
                         + Math.sin(Math.hypot(x - 6, z + 3) * 0.8 - t * 2) * 0.18;
        }
        geo.attributes.position.needsUpdate = true;
        points.rotation.y += ((mouseX * 0.25) - points.rotation.y) * 0.03;
        renderer.render(scene, camera);
        if (!reduce) raf = requestAnimationFrame(loop);
    }
    loop();
});
