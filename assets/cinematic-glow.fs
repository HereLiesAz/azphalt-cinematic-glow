/*{
  "DESCRIPTION": "Filmic contrast, a soft highlight glow, a warm/cool split tone, and a gentle vignette — the single most-used AI video effect, done as one on-device pass.",
  "CATEGORIES": ["Guillotine", "Stylize"],
  "INPUTS": [
    { "NAME": "inputImage", "TYPE": "image" },
    { "NAME": "glow", "TYPE": "float", "DEFAULT": 0.5, "MIN": 0.0, "MAX": 1.0 },
    { "NAME": "vignette", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.0, "MAX": 1.0 }
  ]
}*/

void main() {
  vec2 uv = isf_FragNormCoord;
  vec2 texel = 1.0 / RENDERSIZE;
  vec4 c = IMG_THIS_PIXEL(inputImage);

  // A cheap 4-tap box blur approximates a soft bloom source without a second pass.
  vec3 blurred = c.rgb;
  blurred += IMG_NORM_PIXEL(inputImage, uv + vec2(texel.x * 2.0, 0.0)).rgb;
  blurred += IMG_NORM_PIXEL(inputImage, uv - vec2(texel.x * 2.0, 0.0)).rgb;
  blurred += IMG_NORM_PIXEL(inputImage, uv + vec2(0.0, texel.y * 2.0)).rgb;
  blurred += IMG_NORM_PIXEL(inputImage, uv - vec2(0.0, texel.y * 2.0)).rgb;
  blurred /= 5.0;

  // Only bright areas bloom, so the glow reads as light spilling from highlights, not a haze over everything.
  float luma = dot(blurred, vec3(0.299, 0.587, 0.114));
  float bloomMask = smoothstep(0.55, 1.0, luma);
  vec3 bloomed = c.rgb + blurred * bloomMask * glow * 0.6;

  // Filmic S-curve contrast.
  vec3 graded = smoothstep(0.0, 1.0, bloomed);

  // Warm highlights, cool shadows — a teal/orange split tone without a LUT.
  float shadowMix = 1.0 - smoothstep(0.0, 0.6, luma);
  vec3 splitToned = graded + vec3(0.04, 0.015, -0.03) * (1.0 - shadowMix) - vec3(-0.02, 0.0, 0.03) * shadowMix;

  float vig = 1.0 - vignette * smoothstep(0.35, 1.0, length(uv - vec2(0.5)));
  gl_FragColor = vec4(splitToned * vig, c.a);
}
