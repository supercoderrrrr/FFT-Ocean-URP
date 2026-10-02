# Architecture

## Simulation stages

1. `InitializeSpectrum` creates deterministic Gaussian amplitudes and evaluates the JONSWAP spectrum
2. TMA correction reduces physically unsupported long-wave energy at finite depth
3. Directional cosine-2s spreading distributes energy around the authored wind direction
4. `UpdateSpectrum` evolves each complex component with the linear dispersion relation
5. Bit reversal and staged butterflies execute a two-dimensional inverse FFT
6. `AssembleOcean` converts the spatial-domain fields into displacement, derivatives, and compression history
7. The URP surface shader samples all three cascades and fades them by view distance

Each cascade produces four packed complex fields during the FFT. The final textures contain vertical and horizontal displacement plus surface derivatives used to rebuild the normal. Combining derivatives before normal reconstruction prevents the regular cross-hatch pattern caused by blending independently normalized maps

## Spectral cascades

| Cascade | Domain | Wavelength band | Purpose |
|---|---:|---:|---|
| Large | 250 m | 2.833333 m to 100000 m | Swell and large silhouette |
| Mid | 17 m | 0.833333 m to 2.833333 m | Mid-frequency shape |
| Short | 5 m | 0.039 m to 0.833333 m | Capillary-scale detail |

Only the shared band boundaries touch. Energy is not duplicated between cascades

## Foam

The horizontal displacement derivatives define the local deformation Jacobian. Compression below the configured threshold represents folding or breaking regions. The simulation stores the minimum recent Jacobian and lets it recover over time, producing foam that follows wave crests instead of flashing independently each frame

## Clipmap geometry

`FFTOceanMesh` creates eight nested mesh levels around the presentation camera. Near levels use small vertex spacing while distant rings grow geometrically. The ocean root snaps to a world-space grid, so the visible area can follow the camera without swimming continuously beneath it

## URP surface shading

The ocean shader combines:

- Fresnel-weighted environment reflection
- Main-light specular glitter
- View-dependent roughness and distant smoothing
- Subsurface crest tint
- Jacobian history foam and optional contact foam
- Separate front-face and underside response

The shader uses URP lighting and reflection-probe functions, so the water responds to the active main light and environment rather than a hard-coded fake sun

## Underwater rendering

`FFTOceanUnderwaterFeature` injects a fullscreen pass after opaque rendering. `FFTOceanUnderwaterController` supplies the sampled surface height, optical coefficients, light direction, and caustics controls

The pass reconstructs world position from the camera depth texture, computes the path length inside water, and applies:

- Beer-Lambert RGB absorption
- Distance-based in-scattering
- Waterline feathering
- Low-amplitude screen-space distortion
- Directional light shafts
- Depth-faded projected caustics on underwater geometry

The last 30 percent of `Maximum Visibility` smoothly removes the remaining transmitted scene colour and completes the transition into the scattering colour. Distant seabed geometry and empty water therefore converge to the same colour instead of revealing a hard geometry boundary

The showcase scene uses `Light Shaft Strength = 0.12` to retain subtle directional illumination while keeping the projected caustics readable

The two caustics layers move with equal and opposite offsets. Their average projection anchor remains fixed, so the pattern changes internally without the entire projection oscillating across the seabed

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
