# Architecture

## Simulation stages

1. `InitializeSpectrum` creates deterministic Gaussian amplitudes and evaluates the JONSWAP spectrum
2. TMA correction adjusts the spectrum for finite water depth
3. Directional cosine-2s spreading distributes energy around the authored wind direction
4. `UpdateSpectrum` evolves each complex component with the linear dispersion relation
5. Bit reversal and staged butterflies execute a two-dimensional inverse FFT
6. `AssembleOcean` converts the spatial-domain fields into displacement, derivatives, and compression history
7. The URP surface shader samples all three cascades and fades them by view distance

Each cascade produces four packed complex fields during the FFT. The output textures store displacement, derivatives, and compression history. The surface shader combines derivatives from all cascades before reconstructing its normal.

## Spectral cascades

| Cascade | Domain | Wavelength band | Purpose |
|---|---:|---:|---|
| Large | 250 m | 2.833333 m to 100000 m | Swell and large silhouette |
| Mid | 17 m | 0.833333 m to 2.833333 m | Mid-frequency shape |
| Short | 5 m | 0.039 m to 0.833333 m | Capillary-scale detail |

The bands share boundaries and do not overlap.

## Foam

Horizontal displacement derivatives define the local deformation Jacobian. The simulation stores compressed regions and lets them recover over time. The surface shader maps this history to foam coverage.

## Clipmap geometry

`FFTOceanMesh` creates eight nested levels. Vertex spacing doubles between levels. The ocean root follows the camera with grid snapping, and vertices at each ring boundary morph to the next level's spacing.

## URP surface shading

The ocean shader combines:

- Fresnel-weighted environment reflection
- Main-light specular glitter
- View-dependent roughness and distant smoothing
- Subsurface crest tint
- Jacobian history foam and optional contact foam
- Separate front-face and underside response

Direct lighting uses URP's main light. Environment reflection is sampled through `GlossyEnvironmentReflection`.

## Underwater rendering

`FFTOceanUnderwaterFeature` runs a fullscreen pass before post-processing. `FFTOceanUnderwaterController` supplies surface height, optical coefficients, light direction, and caustics controls.

The pass reconstructs world position from the camera depth texture, computes the path length inside water, and applies:

- Beer-Lambert RGB absorption
- Distance-based in-scattering
- Waterline feathering
- Low-amplitude screen-space distortion
- Directional light shafts
- Depth-faded projected caustics on underwater geometry

Scene colour fades into the scattering colour over the last 30 percent of `Maximum Visibility`.

Caustics use world-space projection along the light direction. Two texture samples scroll in opposite directions and are combined with `min`. Contrast, water depth, and transmission control the final intensity.

## Main runtime files

| File | Responsibility |
|---|---|
| `FFTOcean.compute` | Spectrum generation, time evolution, inverse FFT, assembly, height sampling |
| `FFTOceanController.cs` | GPU resource lifecycle, dispatch order, parameter validation, texture binding |
| `FFTOceanMesh.cs` | Nested clipmap construction |
| `FFTOceanURP.shader` | Surface displacement and lighting |
| `FFTOceanUnderwaterFeature.cs` | URP renderer feature and fullscreen pass |
| `FFTOceanUnderwaterController.cs` | Waterline state and underwater material parameters |
| `FFTOceanUnderwaterURP.shader` | Underwater optical and caustics composition |
