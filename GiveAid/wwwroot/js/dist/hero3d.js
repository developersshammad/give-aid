import{A as Pe,B as pt,C as mt,E as gt,F as Mt,G as vt,I as Ae,J as He,K as Tt,M as xt,N as Et,O as bt,Q as wt,R as Ct,T as Be,U as Fe,V as Rt,W as St,X as de,Y as _t,a as it,b as G,c as st,d as rt,e as ot,f as ce,g as at,h as nt,i as O,j as lt,k as ht,l as T,m as ut,n as V,o as b,p as w,q as fe,r as J,s as ye,t as Q,u as k,v as L,w as B,x as ct,y as ft,z as dt}from"./three-MD4TW6IJ.js";var pe={name:"CopyShader",uniforms:{tDiffuse:{value:null},opacity:{value:1}},vertexShader:`

		varying vec2 vUv;

		void main() {

			vUv = uv;
			gl_Position = projectionMatrix * modelViewMatrix * vec4( position, 1.0 );

		}`,fragmentShader:`

		uniform float opacity;

		uniform sampler2D tDiffuse;

		varying vec2 vUv;

		void main() {

			vec4 texel = texture2D( tDiffuse, vUv );
			gl_FragColor = opacity * texel;


		}`};var x=class{constructor(){this.isPass=!0,this.enabled=!0,this.needsSwap=!0,this.clear=!1,this.renderToScreen=!1}setSize(){}render(){console.error("THREE.Pass: .render() must be implemented in derived pass.")}dispose(){}},Gt=new dt(-1,1,1,-1,0,1),De=class extends Q{constructor(){super(),this.setAttribute("position",new ye([-1,3,0,-1,-1,0,3,-1,0],3)),this.setAttribute("uv",new ye([0,2,0,0,2,0],2))}},Ot=new De,F=class{constructor(e){this._mesh=new k(Ot,e)}dispose(){this._mesh.geometry.dispose()}render(e){e.render(this._mesh,Gt)}get material(){return this._mesh.material}set material(e){this._mesh.material=e}};var me=class extends x{constructor(e,r){super(),this.textureID=r!==void 0?r:"tDiffuse",e instanceof B?(this.uniforms=e.uniforms,this.material=e):e&&(this.uniforms=L.clone(e.uniforms),this.material=new B({name:e.name!==void 0?e.name:"unspecified",defines:Object.assign({},e.defines),uniforms:this.uniforms,vertexShader:e.vertexShader,fragmentShader:e.fragmentShader})),this.fsQuad=new F(this.material)}render(e,r,o){this.uniforms[this.textureID]&&(this.uniforms[this.textureID].value=o.texture),this.fsQuad.material=this.material,this.renderToScreen?(e.setRenderTarget(null),this.fsQuad.render(e)):(e.setRenderTarget(r),this.clear&&e.clear(e.autoClearColor,e.autoClearDepth,e.autoClearStencil),this.fsQuad.render(e))}dispose(){this.material.dispose(),this.fsQuad.dispose()}};var ee=class extends x{constructor(e,r){super(),this.scene=e,this.camera=r,this.clear=!0,this.needsSwap=!1,this.inverse=!1}render(e,r,o){let a=e.getContext(),s=e.state;s.buffers.color.setMask(!1),s.buffers.depth.setMask(!1),s.buffers.color.setLocked(!0),s.buffers.depth.setLocked(!0);let h,m;this.inverse?(h=0,m=1):(h=1,m=0),s.buffers.stencil.setTest(!0),s.buffers.stencil.setOp(a.REPLACE,a.REPLACE,a.REPLACE),s.buffers.stencil.setFunc(a.ALWAYS,h,4294967295),s.buffers.stencil.setClear(m),s.buffers.stencil.setLocked(!0),e.setRenderTarget(o),this.clear&&e.clear(),e.render(this.scene,this.camera),e.setRenderTarget(r),this.clear&&e.clear(),e.render(this.scene,this.camera),s.buffers.color.setLocked(!1),s.buffers.depth.setLocked(!1),s.buffers.color.setMask(!0),s.buffers.depth.setMask(!0),s.buffers.stencil.setLocked(!1),s.buffers.stencil.setFunc(a.EQUAL,1,4294967295),s.buffers.stencil.setOp(a.KEEP,a.KEEP,a.KEEP),s.buffers.stencil.setLocked(!0)}},ge=class extends x{constructor(){super(),this.needsSwap=!1}render(e){e.state.buffers.stencil.setLocked(!1),e.state.buffers.stencil.setTest(!1)}};var Me=class{constructor(e,r){if(this.renderer=e,this._pixelRatio=e.getPixelRatio(),r===void 0){let o=e.getSize(new T);this._width=o.width,this._height=o.height,r=new V(this._width*this._pixelRatio,this._height*this._pixelRatio,{type:O}),r.texture.name="EffectComposer.rt1"}else this._width=r.width,this._height=r.height;this.renderTarget1=r,this.renderTarget2=r.clone(),this.renderTarget2.texture.name="EffectComposer.rt2",this.writeBuffer=this.renderTarget1,this.readBuffer=this.renderTarget2,this.renderToScreen=!0,this.passes=[],this.copyPass=new me(pe),this.copyPass.material.blending=it,this.clock=new de}swapBuffers(){let e=this.readBuffer;this.readBuffer=this.writeBuffer,this.writeBuffer=e}addPass(e){this.passes.push(e),e.setSize(this._width*this._pixelRatio,this._height*this._pixelRatio)}insertPass(e,r){this.passes.splice(r,0,e),e.setSize(this._width*this._pixelRatio,this._height*this._pixelRatio)}removePass(e){let r=this.passes.indexOf(e);r!==-1&&this.passes.splice(r,1)}isLastEnabledPass(e){for(let r=e+1;r<this.passes.length;r++)if(this.passes[r].enabled)return!1;return!0}render(e){e===void 0&&(e=this.clock.getDelta());let r=this.renderer.getRenderTarget(),o=!1;for(let a=0,s=this.passes.length;a<s;a++){let h=this.passes[a];if(h.enabled!==!1){if(h.renderToScreen=this.renderToScreen&&this.isLastEnabledPass(a),h.render(this.renderer,this.writeBuffer,this.readBuffer,e,o),h.needsSwap){if(o){let m=this.renderer.getContext(),p=this.renderer.state.buffers.stencil;p.setFunc(m.NOTEQUAL,1,4294967295),this.copyPass.render(this.renderer,this.writeBuffer,this.readBuffer,e),p.setFunc(m.EQUAL,1,4294967295)}this.swapBuffers()}ee!==void 0&&(h instanceof ee?o=!0:h instanceof ge&&(o=!1))}}this.renderer.setRenderTarget(r)}reset(e){if(e===void 0){let r=this.renderer.getSize(new T);this._pixelRatio=this.renderer.getPixelRatio(),this._width=r.width,this._height=r.height,e=this.renderTarget1.clone(),e.setSize(this._width*this._pixelRatio,this._height*this._pixelRatio)}this.renderTarget1.dispose(),this.renderTarget2.dispose(),this.renderTarget1=e,this.renderTarget2=e.clone(),this.writeBuffer=this.renderTarget1,this.readBuffer=this.renderTarget2}setSize(e,r){this._width=e,this._height=r;let o=this._width*this._pixelRatio,a=this._height*this._pixelRatio;this.renderTarget1.setSize(o,a),this.renderTarget2.setSize(o,a);for(let s=0;s<this.passes.length;s++)this.passes[s].setSize(o,a)}setPixelRatio(e){this._pixelRatio=e,this.setSize(this._width,this._height)}dispose(){this.renderTarget1.dispose(),this.renderTarget2.dispose(),this.copyPass.dispose()}};var ve=class extends x{constructor(e,r,o=null,a=null,s=null){super(),this.scene=e,this.camera=r,this.overrideMaterial=o,this.clearColor=a,this.clearAlpha=s,this.clear=!0,this.clearDepth=!1,this.needsSwap=!1,this._oldClearColor=new w}render(e,r,o){let a=e.autoClear;e.autoClear=!1;let s,h;this.overrideMaterial!==null&&(h=this.scene.overrideMaterial,this.scene.overrideMaterial=this.overrideMaterial),this.clearColor!==null&&(e.getClearColor(this._oldClearColor),e.setClearColor(this.clearColor,e.getClearAlpha())),this.clearAlpha!==null&&(s=e.getClearAlpha(),e.setClearAlpha(this.clearAlpha)),this.clearDepth==!0&&e.clearDepth(),e.setRenderTarget(this.renderToScreen?null:o),this.clear===!0&&e.clear(e.autoClearColor,e.autoClearDepth,e.autoClearStencil),e.render(this.scene,this.camera),this.clearColor!==null&&e.setClearColor(this._oldClearColor),this.clearAlpha!==null&&e.setClearAlpha(s),this.overrideMaterial!==null&&(this.scene.overrideMaterial=h),e.autoClear=a}};var yt={name:"LuminosityHighPassShader",shaderID:"luminosityHighPass",uniforms:{tDiffuse:{value:null},luminosityThreshold:{value:1},smoothWidth:{value:1},defaultColor:{value:new w(0)},defaultOpacity:{value:0}},vertexShader:`

		varying vec2 vUv;

		void main() {

			vUv = uv;

			gl_Position = projectionMatrix * modelViewMatrix * vec4( position, 1.0 );

		}`,fragmentShader:`

		uniform sampler2D tDiffuse;
		uniform vec3 defaultColor;
		uniform float defaultOpacity;
		uniform float luminosityThreshold;
		uniform float smoothWidth;

		varying vec2 vUv;

		void main() {

			vec4 texel = texture2D( tDiffuse, vUv );

			float v = luminance( texel.xyz );

			vec4 outputColor = vec4( defaultColor.rgb, defaultOpacity );

			float alpha = smoothstep( luminosityThreshold, luminosityThreshold + smoothWidth, v );

			gl_FragColor = mix( outputColor, texel, alpha );

		}`};var W=class M extends x{constructor(e,r,o,a){super(),this.strength=r!==void 0?r:1,this.radius=o,this.threshold=a,this.resolution=e!==void 0?new T(e.x,e.y):new T(256,256),this.clearColor=new w(0,0,0),this.renderTargetsHorizontal=[],this.renderTargetsVertical=[],this.nMips=5;let s=Math.round(this.resolution.x/2),h=Math.round(this.resolution.y/2);this.renderTargetBright=new V(s,h,{type:O}),this.renderTargetBright.texture.name="UnrealBloomPass.bright",this.renderTargetBright.texture.generateMipmaps=!1;for(let E=0;E<this.nMips;E++){let I=new V(s,h,{type:O});I.texture.name="UnrealBloomPass.h"+E,I.texture.generateMipmaps=!1,this.renderTargetsHorizontal.push(I);let j=new V(s,h,{type:O});j.texture.name="UnrealBloomPass.v"+E,j.texture.generateMipmaps=!1,this.renderTargetsVertical.push(j),s=Math.round(s/2),h=Math.round(h/2)}let m=yt;this.highPassUniforms=L.clone(m.uniforms),this.highPassUniforms.luminosityThreshold.value=a,this.highPassUniforms.smoothWidth.value=.01,this.materialHighPassFilter=new B({uniforms:this.highPassUniforms,vertexShader:m.vertexShader,fragmentShader:m.fragmentShader}),this.separableBlurMaterials=[];let p=[3,5,7,9,11];s=Math.round(this.resolution.x/2),h=Math.round(this.resolution.y/2);for(let E=0;E<this.nMips;E++)this.separableBlurMaterials.push(this.getSeperableBlurMaterial(p[E])),this.separableBlurMaterials[E].uniforms.invSize.value=new T(1/s,1/h),s=Math.round(s/2),h=Math.round(h/2);this.compositeMaterial=this.getCompositeMaterial(this.nMips),this.compositeMaterial.uniforms.blurTexture1.value=this.renderTargetsVertical[0].texture,this.compositeMaterial.uniforms.blurTexture2.value=this.renderTargetsVertical[1].texture,this.compositeMaterial.uniforms.blurTexture3.value=this.renderTargetsVertical[2].texture,this.compositeMaterial.uniforms.blurTexture4.value=this.renderTargetsVertical[3].texture,this.compositeMaterial.uniforms.blurTexture5.value=this.renderTargetsVertical[4].texture,this.compositeMaterial.uniforms.bloomStrength.value=r,this.compositeMaterial.uniforms.bloomRadius.value=.1;let C=[1,.8,.6,.4,.2];this.compositeMaterial.uniforms.bloomFactors.value=C,this.bloomTintColors=[new b(1,1,1),new b(1,1,1),new b(1,1,1),new b(1,1,1),new b(1,1,1)],this.compositeMaterial.uniforms.bloomTintColors.value=this.bloomTintColors;let U=pe;this.copyUniforms=L.clone(U.uniforms),this.blendMaterial=new B({uniforms:this.copyUniforms,vertexShader:U.vertexShader,fragmentShader:U.fragmentShader,blending:G,depthTest:!1,depthWrite:!1,transparent:!0}),this.enabled=!0,this.needsSwap=!1,this._oldClearColor=new w,this.oldClearAlpha=1,this.basic=new fe,this.fsQuad=new F(null)}dispose(){for(let e=0;e<this.renderTargetsHorizontal.length;e++)this.renderTargetsHorizontal[e].dispose();for(let e=0;e<this.renderTargetsVertical.length;e++)this.renderTargetsVertical[e].dispose();this.renderTargetBright.dispose();for(let e=0;e<this.separableBlurMaterials.length;e++)this.separableBlurMaterials[e].dispose();this.compositeMaterial.dispose(),this.blendMaterial.dispose(),this.basic.dispose(),this.fsQuad.dispose()}setSize(e,r){let o=Math.round(e/2),a=Math.round(r/2);this.renderTargetBright.setSize(o,a);for(let s=0;s<this.nMips;s++)this.renderTargetsHorizontal[s].setSize(o,a),this.renderTargetsVertical[s].setSize(o,a),this.separableBlurMaterials[s].uniforms.invSize.value=new T(1/o,1/a),o=Math.round(o/2),a=Math.round(a/2)}render(e,r,o,a,s){e.getClearColor(this._oldClearColor),this.oldClearAlpha=e.getClearAlpha();let h=e.autoClear;e.autoClear=!1,e.setClearColor(this.clearColor,0),s&&e.state.buffers.stencil.setTest(!1),this.renderToScreen&&(this.fsQuad.material=this.basic,this.basic.map=o.texture,e.setRenderTarget(null),e.clear(),this.fsQuad.render(e)),this.highPassUniforms.tDiffuse.value=o.texture,this.highPassUniforms.luminosityThreshold.value=this.threshold,this.fsQuad.material=this.materialHighPassFilter,e.setRenderTarget(this.renderTargetBright),e.clear(),this.fsQuad.render(e);let m=this.renderTargetBright;for(let p=0;p<this.nMips;p++)this.fsQuad.material=this.separableBlurMaterials[p],this.separableBlurMaterials[p].uniforms.colorTexture.value=m.texture,this.separableBlurMaterials[p].uniforms.direction.value=M.BlurDirectionX,e.setRenderTarget(this.renderTargetsHorizontal[p]),e.clear(),this.fsQuad.render(e),this.separableBlurMaterials[p].uniforms.colorTexture.value=this.renderTargetsHorizontal[p].texture,this.separableBlurMaterials[p].uniforms.direction.value=M.BlurDirectionY,e.setRenderTarget(this.renderTargetsVertical[p]),e.clear(),this.fsQuad.render(e),m=this.renderTargetsVertical[p];this.fsQuad.material=this.compositeMaterial,this.compositeMaterial.uniforms.bloomStrength.value=this.strength,this.compositeMaterial.uniforms.bloomRadius.value=this.radius,this.compositeMaterial.uniforms.bloomTintColors.value=this.bloomTintColors,e.setRenderTarget(this.renderTargetsHorizontal[0]),e.clear(),this.fsQuad.render(e),this.fsQuad.material=this.blendMaterial,this.copyUniforms.tDiffuse.value=this.renderTargetsHorizontal[0].texture,s&&e.state.buffers.stencil.setTest(!0),this.renderToScreen?(e.setRenderTarget(null),this.fsQuad.render(e)):(e.setRenderTarget(o),this.fsQuad.render(e)),e.setClearColor(this._oldClearColor,this.oldClearAlpha),e.autoClear=h}getSeperableBlurMaterial(e){let r=[];for(let o=0;o<e;o++)r.push(.39894*Math.exp(-.5*o*o/(e*e))/e);return new B({defines:{KERNEL_RADIUS:e},uniforms:{colorTexture:{value:null},invSize:{value:new T(.5,.5)},direction:{value:new T(.5,.5)},gaussianCoefficients:{value:r}},vertexShader:`varying vec2 vUv;
				void main() {
					vUv = uv;
					gl_Position = projectionMatrix * modelViewMatrix * vec4( position, 1.0 );
				}`,fragmentShader:`#include <common>
				varying vec2 vUv;
				uniform sampler2D colorTexture;
				uniform vec2 invSize;
				uniform vec2 direction;
				uniform float gaussianCoefficients[KERNEL_RADIUS];

				void main() {
					float weightSum = gaussianCoefficients[0];
					vec3 diffuseSum = texture2D( colorTexture, vUv ).rgb * weightSum;
					for( int i = 1; i < KERNEL_RADIUS; i ++ ) {
						float x = float(i);
						float w = gaussianCoefficients[i];
						vec2 uvOffset = direction * invSize * x;
						vec3 sample1 = texture2D( colorTexture, vUv + uvOffset ).rgb;
						vec3 sample2 = texture2D( colorTexture, vUv - uvOffset ).rgb;
						diffuseSum += (sample1 + sample2) * w;
						weightSum += 2.0 * w;
					}
					gl_FragColor = vec4(diffuseSum/weightSum, 1.0);
				}`})}getCompositeMaterial(e){return new B({defines:{NUM_MIPS:e},uniforms:{blurTexture1:{value:null},blurTexture2:{value:null},blurTexture3:{value:null},blurTexture4:{value:null},blurTexture5:{value:null},bloomStrength:{value:1},bloomFactors:{value:null},bloomTintColors:{value:null},bloomRadius:{value:0}},vertexShader:`varying vec2 vUv;
				void main() {
					vUv = uv;
					gl_Position = projectionMatrix * modelViewMatrix * vec4( position, 1.0 );
				}`,fragmentShader:`varying vec2 vUv;
				uniform sampler2D blurTexture1;
				uniform sampler2D blurTexture2;
				uniform sampler2D blurTexture3;
				uniform sampler2D blurTexture4;
				uniform sampler2D blurTexture5;
				uniform float bloomStrength;
				uniform float bloomRadius;
				uniform float bloomFactors[NUM_MIPS];
				uniform vec3 bloomTintColors[NUM_MIPS];

				float lerpBloomFactor(const in float factor) {
					float mirrorFactor = 1.2 - factor;
					return mix(factor, mirrorFactor, bloomRadius);
				}

				void main() {
					gl_FragColor = bloomStrength * ( lerpBloomFactor(bloomFactors[0]) * vec4(bloomTintColors[0], 1.0) * texture2D(blurTexture1, vUv) +
						lerpBloomFactor(bloomFactors[1]) * vec4(bloomTintColors[1], 1.0) * texture2D(blurTexture2, vUv) +
						lerpBloomFactor(bloomFactors[2]) * vec4(bloomTintColors[2], 1.0) * texture2D(blurTexture3, vUv) +
						lerpBloomFactor(bloomFactors[3]) * vec4(bloomTintColors[3], 1.0) * texture2D(blurTexture4, vUv) +
						lerpBloomFactor(bloomFactors[4]) * vec4(bloomTintColors[4], 1.0) * texture2D(blurTexture5, vUv) );
				}`})}};W.BlurDirectionX=new T(1,0);W.BlurDirectionY=new T(0,1);var Pt={name:"OutputShader",uniforms:{tDiffuse:{value:null},toneMappingExposure:{value:1}},vertexShader:`
		precision highp float;

		uniform mat4 modelViewMatrix;
		uniform mat4 projectionMatrix;

		attribute vec3 position;
		attribute vec2 uv;

		varying vec2 vUv;

		void main() {

			vUv = uv;
			gl_Position = projectionMatrix * modelViewMatrix * vec4( position, 1.0 );

		}`,fragmentShader:`
	
		precision highp float;

		uniform sampler2D tDiffuse;

		#include <tonemapping_pars_fragment>
		#include <colorspace_pars_fragment>

		varying vec2 vUv;

		void main() {

			gl_FragColor = texture2D( tDiffuse, vUv );

			// tone mapping

			#ifdef LINEAR_TONE_MAPPING

				gl_FragColor.rgb = LinearToneMapping( gl_FragColor.rgb );

			#elif defined( REINHARD_TONE_MAPPING )

				gl_FragColor.rgb = ReinhardToneMapping( gl_FragColor.rgb );

			#elif defined( CINEON_TONE_MAPPING )

				gl_FragColor.rgb = CineonToneMapping( gl_FragColor.rgb );

			#elif defined( ACES_FILMIC_TONE_MAPPING )

				gl_FragColor.rgb = ACESFilmicToneMapping( gl_FragColor.rgb );

			#elif defined( AGX_TONE_MAPPING )

				gl_FragColor.rgb = AgXToneMapping( gl_FragColor.rgb );

			#elif defined( NEUTRAL_TONE_MAPPING )

				gl_FragColor.rgb = NeutralToneMapping( gl_FragColor.rgb );

			#endif

			// color space

			#ifdef SRGB_TRANSFER

				gl_FragColor = sRGBTransferOETF( gl_FragColor );

			#endif

		}`};var Te=class extends x{constructor(){super();let e=Pt;this.uniforms=L.clone(e.uniforms),this.material=new Ct({name:e.name,uniforms:this.uniforms,vertexShader:e.vertexShader,fragmentShader:e.fragmentShader}),this.fsQuad=new F(this.material),this._outputColorSpace=null,this._toneMapping=null}render(e,r,o){this.uniforms.tDiffuse.value=o.texture,this.uniforms.toneMappingExposure.value=e.toneMappingExposure,(this._outputColorSpace!==e.outputColorSpace||this._toneMapping!==e.toneMapping)&&(this._outputColorSpace=e.outputColorSpace,this._toneMapping=e.toneMapping,this.material.defines={},ut.getTransfer(this._outputColorSpace)===ht&&(this.material.defines.SRGB_TRANSFER=""),this._toneMapping===st?this.material.defines.LINEAR_TONE_MAPPING="":this._toneMapping===rt?this.material.defines.REINHARD_TONE_MAPPING="":this._toneMapping===ot?this.material.defines.CINEON_TONE_MAPPING="":this._toneMapping===ce?this.material.defines.ACES_FILMIC_TONE_MAPPING="":this._toneMapping===at?this.material.defines.AGX_TONE_MAPPING="":this._toneMapping===nt&&(this.material.defines.NEUTRAL_TONE_MAPPING=""),this.material.needsUpdate=!0),this.renderToScreen===!0?(e.setRenderTarget(null),this.fsQuad.render(e)):(e.setRenderTarget(r),this.clear&&e.clear(e.autoClearColor,e.autoClearDepth,e.autoClearStencil),this.fsQuad.render(e))}dispose(){this.material.dispose(),this.fsQuad.dispose()}};var R=document.querySelector("[data-hero3d]"),Le=window.matchMedia("(prefers-reduced-motion: reduce)").matches;function Vt(){try{let M=document.createElement("canvas");return!!(window.WebGLRenderingContext&&(M.getContext("webgl2")||M.getContext("webgl")))}catch{return!1}}if(R&&Vt()){let j=function(i,t){let u=document.createElement("canvas");u.width=u.height=128;let n=u.getContext("2d"),l=n.createRadialGradient(64,64,0,64,64,64);l.addColorStop(0,i),l.addColorStop(.35,i.replace(/[\d.]+\)$/,"0.45)")),l.addColorStop(1,t),n.fillStyle=l,n.fillRect(0,0,128,128);let c=new Tt(u);return c.colorSpace=lt,c},At=function(i){let t=Math.random()*Math.PI*2,u=16*Math.pow(Math.sin(t),3),n=13*Math.cos(t)-5*Math.cos(2*t)-2*Math.cos(3*t)-Math.cos(4*t),l=.82+Math.random()*.22,c=.2,v=(Math.random()-.5)*2*1.5*Math.sqrt(Math.max(0,1-Math.pow((u*u+n*n)/300,1)));i.set(u*c*l,n*c*l+.3,v)},Ht=function(i,t){if(t%3===0){let n=Math.floor(Math.random()*12)/12*Math.PI*2,l=(Math.random()-.5)*Math.PI;i.set(3.4*Math.cos(l)*Math.cos(n),3.4*Math.sin(l),3.4*Math.cos(l)*Math.sin(n))}else if(t%3===1){let n=((Math.floor(Math.random()*7)+1)/8-.5)*Math.PI,l=Math.random()*Math.PI*2;i.set(3.4*Math.cos(n)*Math.cos(l),3.4*Math.sin(n),3.4*Math.cos(n)*Math.sin(l))}else{let n=Math.random()*2-1,l=Math.random()*Math.PI*2,c=3.4*(.98+Math.random()*.04);i.set(c*Math.sqrt(1-n*n)*Math.cos(l),c*n,c*Math.sqrt(1-n*n)*Math.sin(l))}},Bt=function(i,t){let u=t%6,n=u/6*Math.PI*2+Math.PI/2,l=Math.random()*2-1,c=Math.random()*Math.PI*2,v=.85*Math.cbrt(Math.random());i.set(Math.cos(n)*3.2+v*Math.sqrt(1-l*l)*Math.cos(c),Math.sin(n)*3.2+v*l,v*Math.sqrt(1-l*l)*Math.sin(c))},We=function(){let i=R.clientWidth,t=R.clientHeight;a.setSize(i,t,!1),m.setSize(i,t),p.setSize(i,t),h.aspect=i/t,h.updateProjectionMatrix(),o()?(C.position.set(1.2,4.2,-6),C.scale.setScalar(.7)):(C.position.set(4.7,1.2,0),C.scale.setScalar(.95))},zt=function(i){let t=xe+Ke,u=t*X.length,n=i%u,l=Math.floor(n/t),c=n-l*t,v=c<xe?0:It((c-xe)/Ke);return{a:X[l],b:X[(l+1)%X.length],mix:v,idx:l}},ae=function(){if(cancelAnimationFrame($e),!oe||document.hidden)return;let i=Math.min(Ze.getDelta(),.1),t=Ze.elapsedTime,u=Math.min(window.scrollY/window.innerHeight,1),n=1e9,l=1e9;qe&&!Le&&(Xe.setFromCamera(je,h),Xe.ray.intersectPlane(Ut,Ye)&&(se.copy(Ye),te.worldToLocal(se),n=se.x,l=se.y));let{a:c,b:v,mix:Ee,idx:Je}=zt(t),be=1+u*2.2,Z=i*60,we=Math.min(.035*Z,.5),Ce=Math.pow(.88,Z);for(let d=0;d<y;d++){let f=d*3,D=c[f]+(v[f]-c[f])*Ee,H=c[f+1]+(v[f+1]-c[f+1])*Ee,$=c[f+2]+(v[f+2]-c[f+2])*Ee;D*=be,H*=be,$*=be;let et=1+Math.sin(t*1.6+d*.01)*.015;D*=et,H*=et;let le=S[f]+(D-g[f])*we,he=S[f+1]+(H-g[f+1])*we,ue=S[f+2]+($-g[f+2])*we,Re=g[f]-n,Se=g[f+1]-l,tt=Re*Re+Se*Se;if(tt<2.2){let _e=(2.2-tt)*.03;le+=Re*_e,he+=Se*_e,ue+=(Math.random()-.5)*_e}le*=Ce,he*=Ce,ue*=Ce,S[f]=le,S[f+1]=he,S[f+2]=ue,g[f]+=le*Z,g[f+1]+=he*Z,g[f+2]+=ue*Z}Y.attributes.position.needsUpdate=!0,te.rotation.y=Je===1?t*.25:Math.sin(t*.45)*.5,te.rotation.z=Je===2?t*.15:0;let Nt=1+Math.pow(Math.max(0,Math.sin(t*2.6)),8)*.07+re*.3;re*=.93,z.rotation.y=Math.sin(t*.5)*.6+A.x*.4,z.rotation.x=Math.sin(t*.35)*.12-A.y*.25,z.position.y=Math.sin(t*.9)*.18+.3,z.scale.setScalar(ze*Nt),p.strength=.7+Math.pow(Math.max(0,Math.sin(t*2.6)),8)*.2+re*.35,Ve.forEach(d=>{let f=d.phase+t*d.speed;d.orb.position.set(Math.cos(f)*d.radius,Math.sin(f)*d.radius,0);let D=d.trail.geometry.attributes.position;for(let H=0;H<D.count;H++){let $=f-H*.035;D.setXYZ(H,Math.cos($)*d.radius,Math.sin($)*d.radius,0)}D.needsUpdate=!0});let ne=K.attributes.position.array;for(let d=0;d<N;d++)ne[d*3+1]+=q[d]*i*.8,ne[d*3]+=Math.sin(t*.6+d)*i*.08,q[d]>1&&(q[d]-=i*2),ne[d*3+1]>11&&(ne[d*3+1]=-11);K.attributes.position.needsUpdate=!0,A.x+=(ie.x-A.x)*.04,A.y+=(ie.y-A.y)*.04,h.position.x=A.x*1.6,h.position.y=-A.y*1-u*2,h.position.z=14+u*6,h.lookAt(o()?0:1.4,-u*1.5,0),m.render(),Le||($e=requestAnimationFrame(ae))},o=()=>R.clientWidth<992,a=new pt({antialias:!1,powerPreference:"high-performance"});a.setPixelRatio(Math.min(window.devicePixelRatio,1.5)),a.setClearColor(267022,1),a.toneMapping=ce,a.toneMappingExposure=.95,R.appendChild(a.domElement);let s=new gt;s.fog=new mt(267022,.04);let h=new ct(42,1,.1,100);h.position.set(0,0,14);let m=new Me(a);m.addPass(new ve(s,h));let p=new W(new T(1,1),.8,.4,.45);m.addPass(p),m.addPass(new Te);let C=new Pe;s.add(C),s.add(new St(4221788,.8));let U=new Rt(16777215,2);U.position.set(5,6,8),s.add(U);let E=new Fe(16102210,50,30,1.6);E.position.set(0,0,3),C.add(E);let I=new Fe(3462041,35,30,1.6);I.position.set(-6,-3,2),s.add(I);let Ue=j("rgba(255,255,255,1)","rgba(255,255,255,0)"),_=new xt;_.moveTo(0,-1.6),_.bezierCurveTo(-.4,-1.2,-2.2,-.2,-2.2,.9),_.bezierCurveTo(-2.2,1.9,-1.3,2.4,-.7,2.4),_.bezierCurveTo(-.2,2.4,0,2,0,1.75),_.bezierCurveTo(0,2,.2,2.4,.7,2.4),_.bezierCurveTo(1.3,2.4,2.2,1.9,2.2,.9),_.bezierCurveTo(2.2,-.2,.4,-1.2,0,-1.6);let Ie=new Et(_,{depth:.8,bevelEnabled:!0,bevelThickness:.32,bevelSize:.28,bevelSegments:10,curveSegments:40});Ie.center();let z=new k(Ie,new Be({color:16102210,emissive:8012544,emissiveIntensity:.35,metalness:.4,roughness:.2,clearcoat:1,clearcoatRoughness:.1})),ze=.5;z.scale.setScalar(ze),C.add(z);let y=o()?3200:7e3,X=[At,Ht,Bt].map(i=>{let t=new Float32Array(y*3),u=new b;for(let n=0;n<y;n++)i(u,n),t.set([u.x,u.y,u.z],n*3);return t}),g=new Float32Array(y*3),S=new Float32Array(y*3),Ne=new Float32Array(y*3),Ge=new w(16102210),Oe=new w(3462041),Ft=new w(16774364),P=new w;for(let i=0;i<y;i++){g[i*3]=(Math.random()-.5)*40,g[i*3+1]=(Math.random()-.5)*30,g[i*3+2]=(Math.random()-.5)*30;let t=Math.random();P.copy(t<.55?Ge:t<.9?Oe:Ft).multiplyScalar(.9),Ne.set([P.r,P.g,P.b],i*3)}let Y=new Q;Y.setAttribute("position",new J(g,3)),Y.setAttribute("color",new J(Ne,3));let te=new He(Y,new Ae({size:o()?.14:.12,map:Ue,vertexColors:!0,transparent:!0,depthWrite:!1,blending:G,sizeAttenuation:!0}));C.add(te);let Ve=[],Dt=new bt(.3,3),Lt=new fe({color:3462041,transparent:!0,opacity:.18});for(let i=0;i<6;i++){let t=new Pe;t.rotation.set(Math.PI/2+(i%3-1)*.55,i*Math.PI/3,(i%2?1:-1)*.35),C.add(t);let u=4.4+i%3*.5;t.add(new k(new wt(u,.01,6,180),Lt));let n=new k(Dt,new Be({color:i%2?3462041:11006928,emissive:3462041,emissiveIntensity:.6,metalness:.2,roughness:.15,clearcoat:1}));t.add(n);let l=new Q().setFromPoints(new Array(24).fill(0).map(()=>new b)),c=new vt(l,new Mt({color:3462041,transparent:!0,opacity:.55,blending:G}));t.add(c),Ve.push({orb:n,trail:c,radius:u,speed:.3+i*.05,phase:i/6*Math.PI*2})}let N=o()?400:900,Qe=new Float32Array(N*3),ke=new Float32Array(N*3),q=new Float32Array(N);for(let i=0;i<N;i++)Qe.set([(Math.random()-.5)*40,(Math.random()-.5)*22,(Math.random()-.5)*20-4],i*3),P.copy(Ge).lerp(Oe,Math.random()<.7?Math.random()*.2:.85).multiplyScalar(.6),ke.set([P.r,P.g,P.b],i*3),q[i]=.2+Math.random()*.8;let K=new Q;K.setAttribute("position",new J(Qe,3)),K.setAttribute("color",new J(ke,3)),s.add(new He(K,new Ae({size:.13,vertexColors:!0,map:Ue,transparent:!0,depthWrite:!1,blending:G}))),We(),new ResizeObserver(We).observe(R);let ie={x:0,y:0},A={x:0,y:0},je=new T,Xe=new _t,Ut=new ft(new b(0,0,1),0),Ye=new b,se=new b,qe=!1;window.addEventListener("pointermove",i=>{ie.x=i.clientX/window.innerWidth*2-1,ie.y=i.clientY/window.innerHeight*2-1;let t=R.getBoundingClientRect();qe=i.clientY>=t.top&&i.clientY<=t.bottom,je.set((i.clientX-t.left)/t.width*2-1,-((i.clientY-t.top)/t.height)*2+1)},{passive:!0});let re=0;R.parentElement.addEventListener("click",i=>{if(!i.target.closest("a, button, input, select, label, form")){re=1;for(let t=0;t<y;t++){let u=g[t*3],n=g[t*3+1],l=g[t*3+2],c=Math.hypot(u,n,l)||1,v=.4+Math.random()*.9;S[t*3]+=u/c*v,S[t*3+1]+=n/c*v,S[t*3+2]+=l/c*v}for(let t=0;t<N;t++)q[t]+=3}});let xe=5.5,Ke=2.2,It=i=>i<.5?4*i*i*i:1-Math.pow(-2*i+2,3)/2,oe=!0;new IntersectionObserver(i=>{oe=i[0].isIntersecting,oe&&ae()}).observe(R),document.addEventListener("visibilitychange",()=>{!document.hidden&&oe&&ae()});let Ze=new de,$e=0;Le&&(g.set(X[0]),Y.attributes.position.needsUpdate=!0),ae(),R.classList.add("ready")}
