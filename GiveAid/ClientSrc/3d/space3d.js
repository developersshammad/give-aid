// =====================================================================
//  Give-AID - fixed 3D background for every public page
//  Low-poly crystals, rings and small hearts float through the dark,
//  turning slowly. Scrolling moves them at different speeds (parallax),
//  so the page feels like it has real depth.
// =====================================================================
import * as THREE from "three";

const host = document.querySelector("[data-space3d]");
const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

if (host) {
    let renderer;
    try { renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: "low-power" }); }
    catch { renderer = null; }

    if (renderer) {
        renderer.setPixelRatio(1);
        host.appendChild(renderer.domElement);
        const scene = new THREE.Scene();
        scene.fog = new THREE.Fog(0x04130E, 8, 34);
        const camera = new THREE.PerspectiveCamera(55, 1, 0.1, 80);
        camera.position.z = 12;

        scene.add(new THREE.AmbientLight(0x5f8f7b, 1.2));
        const sun = new THREE.DirectionalLight(0xF5B342, 2.2);
        sun.position.set(-4, 6, 6);
        scene.add(sun);
        const rim = new THREE.DirectionalLight(0x34D399, 1.6);
        rim.position.set(6, -4, 3);
        scene.add(rim);

        // small extruded heart shape
        const hs = new THREE.Shape();
        hs.moveTo(0, -0.8);
        hs.bezierCurveTo(-0.2, -0.6, -1.1, -0.1, -1.1, 0.45);
        hs.bezierCurveTo(-1.1, 0.95, -0.65, 1.2, -0.35, 1.2);
        hs.bezierCurveTo(-0.1, 1.2, 0, 1.0, 0, 0.88);
        hs.bezierCurveTo(0, 1.0, 0.1, 1.2, 0.35, 1.2);
        hs.bezierCurveTo(0.65, 1.2, 1.1, 0.95, 1.1, 0.45);
        hs.bezierCurveTo(1.1, -0.1, 0.2, -0.6, 0, -0.8);
        const heartGeo = new THREE.ExtrudeGeometry(hs, { depth: 0.35, bevelEnabled: true, bevelSize: 0.12, bevelThickness: 0.12, bevelSegments: 3 });
        heartGeo.center();

        const geos = [
            new THREE.IcosahedronGeometry(0.7, 0),
            new THREE.OctahedronGeometry(0.7, 0),
            new THREE.TorusGeometry(0.6, 0.16, 10, 32),
            new THREE.DodecahedronGeometry(0.6, 0),
            heartGeo
        ];
        const mats = [
            new THREE.MeshStandardMaterial({ color: 0x1FA67A, flatShading: true, roughness: 0.35, metalness: 0.3 }),
            new THREE.MeshStandardMaterial({ color: 0xF5B342, flatShading: true, roughness: 0.3, metalness: 0.5 }),
            new THREE.MeshStandardMaterial({ color: 0x0F4535, flatShading: true, roughness: 0.5 }),
            new THREE.MeshBasicMaterial({ color: 0x34D399, wireframe: true, transparent: true, opacity: 0.35 })
        ];

        const items = [];
        const COUNT = window.innerWidth < 768 ? 14 : 28;
        for (let i = 0; i < COUNT; i++) {
            const g = geos[i % geos.length];
            const m = g === heartGeo ? mats[1] : mats[i % mats.length];
            const mesh = new THREE.Mesh(g, m);
            const depth = Math.random();                       // 0 = far, 1 = near
            mesh.position.set((Math.random() - 0.5) * 26, (Math.random() - 0.5) * 18, -16 + depth * 14);
            mesh.scale.setScalar(0.5 + Math.random() * 0.9);
            mesh.rotation.set(Math.random() * 6, Math.random() * 6, Math.random() * 6);
            scene.add(mesh);
            items.push({
                mesh, depth, baseY: mesh.position.y,
                spin: new THREE.Vector3((Math.random() - 0.5) * 0.4, (Math.random() - 0.5) * 0.5, (Math.random() - 0.5) * 0.3),
                bob: Math.random() * 6
            });
        }

        function resize() {
            renderer.setSize(window.innerWidth, window.innerHeight, false);
            camera.aspect = window.innerWidth / window.innerHeight;
            camera.updateProjectionMatrix();
        }
        resize();
        window.addEventListener("resize", resize);

        let mx = 0, my = 0;
        window.addEventListener("pointermove", (e) => {
            mx = e.clientX / window.innerWidth - 0.5;
            my = e.clientY / window.innerHeight - 0.5;
        }, { passive: true });

        const clock = new THREE.Clock();
        let raf = 0;
        function loop() {
            cancelAnimationFrame(raf);
            if (document.hidden) return;
            const dt = Math.min(clock.getDelta(), 0.05), t = clock.elapsedTime;
            const sy = window.scrollY / window.innerHeight;           // pages scrolled
            items.forEach((it) => {
                it.mesh.rotation.x += it.spin.x * dt;
                it.mesh.rotation.y += it.spin.y * dt;
                it.mesh.rotation.z += it.spin.z * dt;
                // nearer objects move faster when scrolling = parallax depth
                let y = it.baseY + Math.sin(t * 0.5 + it.bob) * 0.4 + sy * (2 + it.depth * 9);
                y = ((y + 12) % 24 + 24) % 24 - 12;                    // wrap around vertically
                it.mesh.position.y = y;
            });
            camera.position.x += (mx * 1.5 - camera.position.x) * 0.03;
            camera.position.y += (-my * 1.0 - camera.position.y) * 0.03;
            camera.lookAt(0, 0, -6);
            renderer.render(scene, camera);
            if (!reduce) raf = requestAnimationFrame(loop);
        }
        document.addEventListener("visibilitychange", loop);
        loop();
    }
}
