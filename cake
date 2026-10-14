/* Birthday cake animation engine. Settings live in config.js. */
(() => {
  "use strict";
  const C = CONFIG;
  const S = C.canvasSize;
  const canvas = document.getElementById("stage");
  const ctx = canvas.getContext("2d");
  const dpr = Math.max(1, window.devicePixelRatio || 1);
  canvas.width = canvas.height = S * dpr;

  // Offscreen layer holding every word that isn't currently animating
  const off = document.createElement("canvas");
  off.width = off.height = S * dpr;
  const octx = off.getContext("2d");

  document.title = C.title;
  document.body.style.background = C.background;

  // ---------- helpers ----------
  const rgb = (c) => (Array.isArray(c) ? c : [1, 3, 5].map((i) => parseInt(c.slice(i, i + 2), 16) / 255));
  const clamp = (v) => Math.max(0, Math.min(1, v));
  const hx = (c) => "#" + c.map((v) => Math.round(clamp(v) * 255).toString(16).padStart(2, "0")).join("");
  const mix = (a, b, k) => a.map((p, i) => p + (b[i] - p) * k);
  const dim = (c, k) => c.map((v) => v * k);
  const cy = (y) => -(y + C.yOffset); // turtle y -> screen y (origin at center)

  function makeRng(seed) {
    let a = seed == null ? (Math.random() * 2 ** 32) >>> 0 : seed >>> 0;
    const next = () => {
      a = (a + 0x6d2b79f5) >>> 0;
      let t = a;
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
    return {
      random: next,
      uniform: (a, b) => a + (b - a) * next(),
      randint: (a, b) => a + Math.floor(next() * (b - a + 1)),
      choice: (arr) => arr[Math.floor(next() * arr.length)],
      shuffle: (arr) => {
        for (let i = arr.length - 1; i > 0; i--) {
          const j = Math.floor(next() * (i + 1));
          [arr[i], arr[j]] = [arr[j], arr[i]];
        }
      },
    };
  }

  // ---------- static config -> working data ----------
  const BG = rgb(C.background);
  const WHITE = [1, 1, 1];
  const ICING = rgb(C.icingColor);
  const FILL = rgb(C.fillingColor);
  const FLASH = rgb(C.shimmerColor);
  const SMOKE = rgb(C.smokeColor);
  const RED = rgb(C.flame.red), ORANGE = rgb(C.flame.orange);
  const YELLOW = rgb(C.flame.yellow), EMBER = rgb(C.flame.ember);

  const LAYERS = C.layers.map((l) => ({
    name: l.name, style: l.style || "layer",
    W: l.halfWidth, y1: l.bottom, y2: l.top,
    Rt: l.roundTop || 0, Rb: l.roundBottom || 0,
    color: rgb(l.color), icing: !!l.icing, filling: !!l.filling, phase: l.phase || 0,
  }));
  const layerByName = Object.fromEntries(LAYERS.map((l) => [l.name, l]));
  const YMAX = Math.max(...LAYERS.map((l) => l.y2));
  const YMIN = Math.min(...LAYERS.map((l) => l.y1));
  const MAXW = Math.max(...LAYERS.map((l) => l.W));

  const fontStr = (size) => `${C.fontWeight} ${(size * C.fontScale).toFixed(2)}px ${C.fontFamily}`;

  function halfW(l, y) {
    let w = l.W;
    if (l.Rt && y > l.y2 - l.Rt) {
      const d = y - (l.y2 - l.Rt);
      w = l.W - l.Rt + Math.sqrt(Math.max(0, l.Rt * l.Rt - d * d));
    } else if (l.Rb && y < l.y1 + l.Rb) {
      const d = l.y1 + l.Rb - y;
      w = l.W - l.Rb + Math.sqrt(Math.max(0, l.Rb * l.Rb - d * d));
    }
    return w;
  }
  const layerAt = (y) => LAYERS.find((l) => l.y1 <= y && y < l.y2);
  function spans(y) {
    const l = layerAt(y);
    if (!l) return [];
    const w = halfW(l, y);
    return w > 0 ? [[-w, w]] : [];
  }
  function intersect(A, B) {
    const out = [];
    for (const a of A) for (const b of B) {
      const lo = Math.max(a[0], b[0]), hi = Math.min(a[1], b[1]);
      if (hi - lo > 14) out.push([lo, hi]);
    }
    return out;
  }
  const layerOf = (y) => layerAt(y) || LAYERS[0];

  // ---------- run state ----------
  let rng, items, eatable, flames, smoke, removals, shimmers;
  let fr, eatDone, staticDirty, endFrame;

  function drip(l, x) {
    const a = 0.5 + 0.5 * Math.sin(x / 15 + l.phase);
    const b = 0.5 + 0.5 * Math.sin(x / 38 + l.phase * 2);
    return C.icingDripMin + C.icingDripVariation * a * b;
  }

  function colorFor(p) {
    const l = layerOf(p.y);
    let base = l.color;
    if (l.style === "plate") return mix(base, WHITE, rng.uniform(0, 0.35)).map((v) => Math.min(1, v));
    if (l.icing && p.y > l.y2 - drip(l, p.x)) base = mix(ICING, base, rng.uniform(0, 0.18));
    else if (l.filling && p.y < l.y1 + C.fillingHeight) base = mix(FILL, base, rng.uniform(0, 0.25));
    else base = mix(base, WHITE, rng.uniform(0, 0.18) + (p.edge ? 0.15 : 0));
    return base.map((v) => Math.min(1, v));
  }

  function build() {
    function* phraseCycle() {
      while (true) { const o = [...C.phrases]; rng.shuffle(o); yield* o; }
    }
    const cycle = phraseCycle();
    ctx.font = fontStr(C.textSizes[0]);
    const lineH = Math.max(...C.textSizes.map((z) => {
      ctx.font = fontStr(z);
      const m = ctx.measureText("Hg");
      const h = (m.fontBoundingBoxAscent || 0) + (m.fontBoundingBoxDescent || 0);
      return h || z * C.fontScale * 1.2;
    })) + 4;
    const half = lineH / 2;
    const placed = [];

    for (let y = YMAX - half; y > YMIN + half; y -= lineH) {
      const row = intersect(spans(y + half), spans(y - half));
      for (let [lo, hi] of row) {
        lo += 4; hi -= 4;
        let cur = lo + rng.uniform(0, 8);
        const line = [];
        for (;;) {
          let fit = null;
          for (let t = 0; t < 12; t++) {
            const text = cycle.next().value;
            const size = rng.choice(C.textSizes);
            ctx.font = fontStr(size);
            const w = ctx.measureText(text).width;
            if (cur + w <= hi) { fit = { text, size, w }; break; }
          }
          if (!fit) break;
          line.push({ x: cur + fit.w / 2, text: fit.text, size: fit.size, w: fit.w });
          cur += fit.w + C.wordGap;
        }
        if (!line.length) continue;
        if (line.length > 1) {
          const left = hi - (line[line.length - 1].x + line[line.length - 1].w / 2);
          line.forEach((it, i) => (it.x += (left * i) / (line.length - 1)));
        } else line[0].x = (lo + hi) / 2;
        line.forEach((it, i) => placed.push({
          x: it.x, y, text: it.text, size: it.size,
          edge: i === 0 || i === line.length - 1,
        }));
      }
    }

    for (const p of placed) {
      p.color = colorFor(p);
      p.kind = layerOf(p.y).style === "plate" ? "plate" : "body";
    }

    const extras = [];
    for (const d of C.decorations) {
      const y = d.layer ? layerByName[d.layer].y2 + (d.dy || 0) : d.y;
      extras.push({
        x: d.x, y, text: d.glyph, size: d.size, edge: false,
        color: rgb(d.color), kind: d.edible === false ? "decor" : "cherry",
      });
    }

    const cd = C.candles, candles = [];
    cd.list.forEach((c, ci) => {
      const y0 = c.y0 ?? cd.y0, y1 = c.y1 ?? cd.y1;
      let k = 0;
      for (let yy = y0; yy <= y1; yy += cd.stepY, k++) {
        candles.push({
          x: c.x, y: yy, text: cd.glyph, size: cd.size, edge: false,
          color: rgb(cd.stripeColors[k % cd.stripeColors.length]), kind: "candle", ci,
        });
      }
    });
    return [...placed, ...extras, ...candles];
  }

  function make(p) {
    p.px = p.x; p.py = cy(p.y);
    p.font = fontStr(p.size); p.fontBig = fontStr(p.size + 1);
    p.alive = true;
  }

  // ---------- drawing ----------
  function drawItem(g, p, ox, oy, u, flash) {
    const c = p.color, x = p.px + ox, y = p.py + oy;
    g.font = p.font;
    g.fillStyle = hx(mix(BG, dim(c, 0.3), 1 - u));
    g.fillText(p.text, x + 2, y + 3);
    g.font = p.fontBig;
    g.fillStyle = hx(mix(BG, dim(c, 0.2), 1 - u));
    g.fillText(p.text, x, y);
    g.font = p.font;
    g.fillStyle = hx(flash ? mix(c, FLASH, 0.6) : mix(c, BG, u));
    g.fillText(p.text, x, y);
  }

  function setupCtx(g) {
    g.setTransform(dpr, 0, 0, dpr, 0, 0);
    g.textAlign = "center";
    g.textBaseline = "middle";
  }

  function rebuildStatic() {
    setupCtx(octx);
    octx.clearRect(0, 0, S, S);
    octx.translate(S / 2, S / 2);
    for (const p of items) if (p.alive) drawItem(octx, p, 0, 0, 0, false);
    staticDirty = false;
  }

  function oval(g, b, fill) {
    const rx = (b[2] - b[0]) / 2, ry = (b[3] - b[1]) / 2;
    if (rx <= 0 || ry <= 0) return;
    g.fillStyle = fill;
    g.beginPath();
    g.ellipse((b[0] + b[2]) / 2, (b[1] + b[3]) / 2, rx, ry, 0, 0, Math.PI * 2);
    g.fill();
  }

  function draw() {
    setupCtx(ctx);
    ctx.fillStyle = C.background;
    ctx.fillRect(0, 0, S, S);
    ctx.save();
    ctx.translate(S / 2, S / 2);
    for (const f of flames) if (f.on) oval(ctx, f.glow, f.glowFill);
    ctx.restore();

    if (staticDirty) rebuildStatic();
    ctx.drawImage(off, 0, 0, S, S);

    ctx.save();
    ctx.translate(S / 2, S / 2);
    for (const s of shimmers) if (s.p.alive) drawItem(ctx, s.p, 0, 0, 0, true);
    for (const rm of removals) {
      const u = rm.f <= 0 ? 0 : clamp(rm.f / rm.N);
      const dy = rm.dy;
      const offY = u === 0 ? 0 : dy > 0 ? dy * u * u : dy * (1 - (1 - u) ** 2);
      drawItem(ctx, rm.p, rm.dx * u, offY, u, false);
    }
    for (const f of flames) if (f.on) {
      oval(ctx, f.outer, f.outerFill);
      oval(ctx, f.inner, f.innerFill);
    }
    for (const sm of smoke) if (sm.f > 0) oval(ctx, sm.b, sm.fill);
    ctx.restore();
  }

  // ---------- flames ----------
  function initFlames() {
    flames = C.candles.list.map((c) => {
      const fy = cy(c.flameY ?? C.candles.flameY);
      return {
        x: c.x, by: fy, on: true,
        glow: [c.x - 26, fy - 36, c.x + 26, fy + 16], glowFill: hx(dim(ORANGE, 0.15)),
        outer: [c.x - 8, fy - 24, c.x + 8, fy + 2], outerFill: hx(RED),
        inner: [c.x - 4, fy - 14, c.x + 4, fy + 1], innerFill: hx(YELLOW),
      };
    });
  }

  function flicker(f) {
    const { by, x: cx } = f;
    const k = rng.random(), h = rng.uniform(20, 29), w = rng.uniform(7, 10), sway = rng.uniform(-2.5, 2.5);
    f.outer = [cx - w + sway, by - h, cx + w + sway, by + 2];
    f.outerFill = hx(mix(RED, ORANGE, k));
    f.inner = [cx - w * 0.45 + sway * 0.6, by - h * 0.55, cx + w * 0.45 + sway * 0.6, by + 1];
    f.innerFill = hx(mix(ORANGE, YELLOW, rng.uniform(0.3, 1)));
    const g = rng.uniform(22, 30);
    f.glow = [cx - g, by - g - 10, cx + g, by + g - 14];
    f.glowFill = hx(dim(mix(RED, ORANGE, k), rng.uniform(0.13, 0.22)));
  }

  function blowStep(f, u) {
    const { by, x: cx } = f;
    const lean = 14 * u ** 1.4 + rng.uniform(-1.5, 1.5) * (1 - u);
    const h = 24 * (1 - u) ** 1.2 + 2 + rng.uniform(0, 4) * (1 - u);
    const w = 8 * (1 - 0.55 * u);
    f.outer = [cx - w + lean, by - h, cx + w + lean, by + 2];
    f.outerFill = hx(mix(mix(RED, ORANGE, 0.4), EMBER, u));
    f.inner = [cx - w * 0.45 + lean * 0.6, by - h * 0.55, cx + w * 0.45 + lean * 0.6, by + 1];
    f.innerFill = hx(mix(YELLOW, RED, clamp(u * 1.6)));
    const g = 28 * (1 - 0.75 * u);
    f.glow = [cx - g, by - g - 10, cx + g, by + g - 14];
    f.glowFill = hx(dim(ORANGE, 0.17 * (1 - u) ** 1.5));
  }

  function finishBlow(f) {
    f.on = false;
    for (let i = 0; i < 4; i++) {
      smoke.push({
        x: f.x + 8, y: f.by - 2, f: -rng.randint(0, 14), N: rng.randint(55, 80),
        ph: rng.uniform(0, 6), drift: rng.uniform(6, 16), b: [0, 0, 0, 0], fill: "#000",
      });
    }
  }

  function updateSmoke(sm) {
    sm.f++;
    if (sm.f <= 0) return true;
    const u = clamp(sm.f / sm.N);
    const x = sm.x + sm.drift * u + 5 * Math.sin(u * 7 + sm.ph);
    const y = sm.y - 60 * u;
    const rad = 2 + 8 * u;
    sm.b = [x - rad, y - rad, x + rad, y + rad];
    sm.fill = hx(mix(BG, SMOKE, 0.75 * (1 - u) ** 1.3));
    return u < 1;
  }

  // ---------- removal animation ----------
  function scheduleRemoval(p, dx, dy, n, delay = 0) {
    p.alive = false;
    staticDirty = true;
    removals.push({ p, f: -delay, N: n, dx, dy });
  }
  function updateRemoval(rm) {
    rm.f++;
    return rm.f < rm.N;
  }
  function liftCandle(ci) {
    for (const p of items) if (p.kind === "candle" && p.ci === ci && p.alive) scheduleRemoval(p, 0, -36, 24);
  }

  // ---------- eating ----------
  function finishEating() {
    eatDone = true;
    for (const p of items) if (p.kind === "plate" && p.alive) {
      const delay = Math.floor(((p.x + MAXW) / (2 * MAXW)) * 40);
      scheduleRemoval(p, rng.uniform(-6, 6), rng.uniform(20, 40), rng.randint(20, 28), delay);
    }
  }

  function bite() {
    const pool = eatable.filter((p) => p.alive);
    if (pool.length <= 5) {
      for (const p of pool) scheduleRemoval(p, rng.uniform(-8, 8), rng.uniform(16, 42), rng.randint(18, 26), rng.randint(0, 10));
      finishEating();
      return;
    }
    const th = rng.uniform(0, 2 * Math.PI);
    const vx = Math.cos(th), vy = Math.sin(th);
    let ext = pool[0], best = -Infinity;
    for (const p of pool) {
      const s = p.x * vx + p.y * vy;
      if (s > best) { best = s; ext = p; }
    }
    const R = rng.uniform(C.biteRadius[0], C.biteRadius[1]);
    for (const p of pool) {
      let d = Math.hypot((p.x - ext.x) / 1.6, (p.y - ext.y) * 1.2);
      d += rng.uniform(-10, 10);
      if (d <= R) scheduleRemoval(p, rng.uniform(-8, 8), rng.uniform(16, 42), rng.randint(18, 26), rng.randint(0, 8));
    }
  }

  function shimmer() {
    const alive = items.filter((p) => p.alive && p.kind !== "candle");
    if (!alive.length) return;
    shimmers.push({ p: rng.choice(alive), until: fr + 16 });
  }

  // ---------- main loop ----------
  const blowT0 = (i) => C.introFrames + i * C.blowGapFrames;

  function tick() {
    fr++;
    flames.forEach((f, i) => {
      const t0 = blowT0(i);
      if (fr < t0) { if (fr % 5 === 0) flicker(f); }
      else if (fr < t0 + C.blowFrames) blowStep(f, (fr - t0) / C.blowFrames);
      else if (fr === t0 + C.blowFrames) finishBlow(f);
      if (fr === t0 + C.blowFrames + 6) liftCandle(i);
    });

    smoke = smoke.filter(updateSmoke);

    const eatStart = blowT0(flames.length - 1) + C.blowFrames + 40;
    if (fr >= eatStart && !eatDone && (fr - eatStart) % C.biteGapFrames === 0) bite();

    removals = removals.filter(updateRemoval);
    shimmers = shimmers.filter((s) => s.until > fr);

    if (fr % C.shimmerEveryFrames === 0 && !eatDone) shimmer();

    if (eatDone && !removals.length && !smoke.length && endFrame == null) endFrame = fr;
    if (endFrame != null && C.loop && fr - endFrame > C.loopDelayFrames) reset();
  }

  function reset() {
    rng = makeRng(C.seed);
    items = build();
    items.forEach(make);
    eatable = items.filter((p) => p.kind === "body" || p.kind === "cherry");
    smoke = []; removals = []; shimmers = [];
    fr = 0; eatDone = false; endFrame = null; staticDirty = true;
    initFlames();
  }

  let last = performance.now(), acc = 0;
  function frame(now) {
    acc += Math.min(now - last, 200);
    last = now;
    while (acc >= C.tickMs) { tick(); acc -= C.tickMs; }
    draw();
    requestAnimationFrame(frame);
  }

  if (C.clickToReplay) canvas.addEventListener("click", reset);

  reset();
  requestAnimationFrame(frame);
})();