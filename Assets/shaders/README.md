# Shaders Directory

This directory contains all custom shaders and materials for The Somnial Study project, organized by functionality.

## Directory Structure

### PostProcessing/
Post-processing effects and screen-space shaders.

- **RetroPixel/** - Retro pixelation effects
  - `RetroPixelScreen.shader` - Main retro pixel shader
  - `RetroPixelScreenWithExclusion.shader` - Retro pixel with stencil exclusion
  - `Materials/` - Related materials

- **DreamState/** - Dream state visual effects
  - `DreamStateShader.shader` - Dream-like visual distortion

- **FullScreen/** - Full-screen post-processing utilities
  - `FullScreenEffectWithExclusion.shader` - Full-screen effect with stencil support
  - `StencilWriter.shader` - Stencil buffer writer

- **SunkenPlace/** - "Sunken Place" effect
  - `SunkenPlace.shader` - Specific scene effect

### Effects/
Special visual effects and volumetric rendering.

- **BlackHole/** - Black hole visual effect
  - `BlackHoleURP.shader` - URP-compatible black hole shader

- **VolumetricCube/** - Volumetric cube rendering
  - `VolumetricCube.shader` - Main volumetric cube shader
  - `VolumetricCube_Backface.shader` - Backface rendering pass
  - `BackfaceRT.renderTexture` - Render texture for backface pass
  - `Materials/` - Related materials

- **VolumetricFog/** - Volumetric fog effects
  - `VolumetricFog.shader` - Volumetric fog shader
  - `Clouds Test_DetailNoise.asset` - Cloud noise texture
  - `Materials/` - Related materials

### Glass/
Glass and refraction shaders.

- `GlassRefraction.shader` - Legacy glass refraction
- `GlassRefraction_URP.shader` - URP glass refraction
- `GlassRefractionCooler_URP.shader` - Enhanced URP glass refraction
- `Materials/` - Glass materials

### _Archive/
Deprecated or old versions of shaders kept for reference.

- Old versions of RetroPixel shaders
- **Note:** Files in this directory are not actively used

## Naming Conventions

- Shaders: PascalCase (e.g., `RetroPixelScreen.shader`)
- Materials: Prefix with usage context (e.g., `Custom_RetroPixelScreenWithExclusion.mat`)
- URP shaders: Suffix with `_URP` (e.g., `GlassRefraction_URP.shader`)

## Notes

- All active shaders are URP (Universal Render Pipeline) compatible
- Materials are stored in `Materials/` subfolders within each category
- Legacy/deprecated shaders are in `_Archive/` for reference only
