// =====================================================================
//  Give-AID asset build (optional - the built files are already in wwwroot)
//  1. Bundles the 3D scripts (ClientSrc/3d) with ONLY the parts of three.js
//     they use (tree-shaking) into wwwroot/js/dist  - smaller than the full library.
//  2. Minifies the site's own CSS and JS into *.min.css / *.min.js.
//     The layouts load the readable files in Development and the .min files in
//     Production (see <environment> tags in _Layout.cshtml / _AdminLayout.cshtml).
//  Usage:  cd ClientSrc  ->  npm install  ->  npm run build
// =====================================================================
import * as esbuild from "esbuild";
import { readFileSync, writeFileSync, rmSync } from "node:fs";

const www = new URL("../wwwroot/", import.meta.url).pathname;

// 1. 3D bundles (shared three.js chunk)
rmSync(www + "js/dist", { recursive: true, force: true });
await esbuild.build({
    entryPoints: ["3d/hero3d.js", "3d/space3d.js", "3d/wave3d.js", "3d/admin3d.js"],
    bundle: true, splitting: true, format: "esm", minify: true, target: "es2020",
    outdir: www + "js/dist", chunkNames: "three-[hash]", legalComments: "none", logLevel: "info"
});

// 2. CSS bundles
async function css(files, out) {
    const src = files.map((f) => readFileSync(www + "css/" + f, "utf8")).join("\n");
    const r = await esbuild.transform(src, { loader: "css", minify: true, legalComments: "none" });
    writeFileSync(www + "css/" + out, r.code);
    console.log("css/" + out, (r.code.length / 1024).toFixed(1) + " KB");
}
await css(["theme3d.css"], "public.min.css");
await css(["site.css", "admin.css", "admin-nova.css"], "admin.min.css");

// 3. classic scripts
for (const f of ["site", "fx", "donate", "admin-nova"]) {
    const r = await esbuild.transform(readFileSync(www + `js/${f}.js`, "utf8"), { loader: "js", minify: true, target: "es2017", legalComments: "none" });
    writeFileSync(www + `js/${f}.min.js`, r.code);
    console.log(`js/${f}.min.js`, (r.code.length / 1024).toFixed(1) + " KB");
}
