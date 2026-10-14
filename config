/* ==========================================================================
   BIRTHDAY CAKE — CONFIG
   Edit this file to change the cake. You never need to touch cake.js.
   Colors are hex strings ("#ff8cb2"). Positions use the same coordinate
   system as the original turtle version: (0,0) is the center, +y is up.
   ========================================================================== */

const CONFIG = {
  // ---- Page / canvas -------------------------------------------------------
  title: "Happy Birthday",
  background: "#060208",
  canvasSize: 800,          // internal drawing size (page scales it to fit)
  yOffset: 56,              // shifts the whole scene up/down on screen
  seed: 11,                 // same seed = same layout every time. null = random each run
  loop: true,               // restart automatically when the cake is gone
  loopDelayFrames: 150,     // pause before restarting (60 frames ≈ 1 second)
  clickToReplay: true,

  // ---- Text ----------------------------------------------------------------
  fontFamily: "Arial, 'Noto Sans', 'Noto Sans CJK JP', sans-serif",
  fontWeight: "bold",
  fontScale: 1.333,         // converts the original "pt" sizes to canvas pixels
  textSizes: [9, 10, 11, 12], // each word picks one of these at random
  wordGap: 7,               // horizontal space between words

  // The cake is built out of these. Add, remove, or reorder freely.
  phrases: [
    "Happy Birthday",
    "İyi ki doğdun",
    "Feliz cumpleaños",
    "Joyeux anniversaire",
    "Alles Gute zum Geburtstag",
    "Buon compleanno",
    "Feliz aniversário",
    "С днём рождения",
    "생일 축하해요",
    "お誕生日おめでとう",
    "生日快乐",
    "जन्मदिन मुबारक हो",
    "Χρόνια πολλά",
    "Gefeliciteerd",
    "Grattis på födelsedagen",
    "Wszystkiego najlepszego",
    "สุขสันต์วันเกิด",
    "Chúc mừng sinh nhật",
    "Selamat ulang tahun",
    "З днем народження",
    "عيد ميلاد سعيد",
    "تولدت مبارک",
    "יום הולדת שמח",
    "Tillykke med fødselsdagen",
    "Všechno nejlepší",
  ],

  // ---- Cake layers (bottom to top order doesn't matter) --------------------
  // halfWidth   : half of the layer's width
  // bottom/top  : y range of the layer
  // roundTop / roundBottom : corner rounding radius (0 = square)
  // style       : "plate" = stays until the cake is eaten, then slides off
  //               "layer" = normal cake layer (gets eaten)
  // icing       : cream dripping from the top of the layer
  // filling     : cream-colored stripe at the bottom of the layer
  // phase       : shifts the icing drip pattern so layers don't match
  layers: [
    { name: "plate", style: "plate", halfWidth: 350, bottom: -302, top: -262, roundTop: 18, roundBottom: 18, color: "#f2b84d" },
    { name: "base",  style: "layer", halfWidth: 300, bottom: -262, top: -110, roundTop: 22, roundBottom: 10, color: "#cc1f6b", icing: true, filling: false, phase: 0.0 },
    { name: "mid",   style: "layer", halfWidth: 225, bottom: -110, top: -5,   roundTop: 22, roundBottom: 0,  color: "#f24d8c", icing: true, filling: true,  phase: 1.7 },
    { name: "top",   style: "layer", halfWidth: 150, bottom: -5,   top: 85,   roundTop: 20, roundBottom: 0,  color: "#ff8cb2", icing: true, filling: true,  phase: 3.1 },
  ],

  icingColor: "#ffe6ed",
  fillingColor: "#ffcc8c",
  icingDripMin: 22,         // shortest drip length
  icingDripVariation: 26,   // extra drip length on top of the minimum
  fillingHeight: 16,

  // ---- Decorations (cherries, sprinkles, stars... any text/emoji) ----------
  // layer + dy : place relative to a layer's top edge, OR use y for an absolute spot
  // edible     : true = gets eaten with the cake
  decorations: [
    { glyph: "●", x: -75, layer: "top", dy: -2, size: 13, color: "#eb0a1f", edible: true },
    { glyph: "●", x: -25, layer: "top", dy: -2, size: 13, color: "#eb0a1f", edible: true },
    { glyph: "●", x:  25, layer: "top", dy: -2, size: 13, color: "#eb0a1f", edible: true },
    { glyph: "●", x:  75, layer: "top", dy: -2, size: 13, color: "#eb0a1f", edible: true },
    // Example: uncomment to add a star on a lower layer
    // { glyph: "★", x: 0, layer: "mid", dy: -40, size: 16, color: "#ffd84d", edible: true },
  ],

  // ---- Candles -------------------------------------------------------------
  // Each candle can override y0 (stick bottom), y1 (stick top), flameY.
  candles: {
    glyph: "|",
    size: 12,
    stepY: 13,                          // vertical spacing between stripe segments
    stripeColors: ["#ffebf2", "#73ccff"], // alternating colors
    y0: 96,
    y1: 166,
    flameY: 170,
    list: [{ x: -100 }, { x: -50 }, { x: 0 }, { x: 50 }, { x: 100 }],
    // Example of a taller custom candle:  { x: 130, y1: 190, flameY: 194 }
  },

  // ---- Flame & smoke colors ------------------------------------------------
  flame: {
    red: "#f21a0d",
    orange: "#ff8c0d",
    yellow: "#ffcc40",
    ember: "#4d0a05",
  },
  smokeColor: "#9e9eae",
  shimmerColor: "#ffc7c7",   // brief sparkle flash on random words
  shimmerEveryFrames: 16,

  // ---- Timing (in frames, 60 frames ≈ 1 second) -----------------------------
  tickMs: 16,
  introFrames: 120,          // flames flicker for this long before the first blow
  blowGapFrames: 40,         // delay between each candle being blown out
  blowFrames: 26,            // how long one blow-out takes
  biteGapFrames: 46,         // delay between bites
  biteRadius: [85, 115],     // min/max bite size
};