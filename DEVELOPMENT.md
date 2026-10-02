# Development Archive / 开发归档

[English](#english) · [简体中文](#简体中文)

## English

### Why the initial Git commit imports an existing project

This project grew from a personal study of spectral ocean simulation and real-time water rendering in Unity URP. Development iterations were tracked with Unity Version Control / Plastic SCM. The completed implementation was migrated to GitHub for portfolio presentation and continued maintenance.

The initial Git commit captures the implementation at migration time, including the completed underwater rendering and projected-caustics improvements. Earlier development records remain in Unity Version Control, while subsequent changes are recorded in this repository's Git history.

This document summarizes the technical evolution of the implementation. It is a development retrospective rather than a raw export of the original changeset records.

### Implementation evolution before migration

The following stages describe the implementation and its iterations. The code entry points link to the current versions.

| Stage | Work | Current entry points |
| --- | --- | --- |
| GPU FFT pipeline | Complex spectrum evolution, bit reversal, horizontal and vertical inverse FFT butterflies, and spatial displacement assembly | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute), [FFTOceanController](Assets/FFT-Ocean/Scripts/FFTOceanController.cs) |
| Spectral sea state | JONSWAP energy distribution, TMA finite-depth correction, directional spreading, and three non-overlapping wavelength bands | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute), [FFTOceanController](Assets/FFT-Ocean/Scripts/FFTOceanController.cs) |
| Wave profiles and foam | Choppy horizontal displacement, derivative-based normal reconstruction, and temporal Jacobian compression history for breaking-wave foam | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute), [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| Large ocean presentation | Eight camera-following clipmap levels, grid snapping, edge morphing, and distance-based cascade weighting | [FFTOceanMesh](Assets/FFT-Ocean/Scripts/FFTOceanMesh.cs), [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| Surface rendering | Fresnel reflection, main-light glitter, subsurface crest tint, distance roughness, and separate surface-underside shading | [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| Underwater rendering | GPU surface-height sampling, waterline hysteresis, RGB absorption, scattering, distortion, and directional light shafts | [FFTOceanUnderwaterController](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterController.cs), [FFTOceanUnderwaterFeature](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterFeature.cs) |
| Projected caustics | Replace the computed-caustics experiment with a grayscale texture projection; use two oppositely scrolling layers to keep the projection anchor stable | [FFTOceanUnderwaterURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanUnderwaterURP.shader) |
| Showcase setup | Tune material and sea-state parameters, add free camera controls, and provide an editor scene builder | [FFTOceanSceneBuilder](Assets/FFT-Ocean/Editor/FFTOceanSceneBuilder.cs), [FFTOceanShowcaseCamera](Assets/FFT-Ocean/Scripts/FFTOceanShowcaseCamera.cs) |

### Development after migration

| Update | Work | Record |
| --- | --- | --- |
| Standalone project import | Publish the Unity project with its scene, URP settings, runtime code, shaders, dependencies, and bilingual documentation | [f85a781](https://github.com/supercoderrrrr/FFT-Ocean-URP/commit/f85a781cef4dc6a1f3d9b1622fef37d8363cf201) |
| Reproducible showcase capture | Add a development-build capture runner and runtime parameter controls; capture three sea-state presets, a GIF, an MP4, and underwater imagery | [23cafde](https://github.com/supercoderrrrr/FFT-Ocean-URP/commit/23cafde9b495300329b46c3f9dacdcf572a3aafc) |
| Underwater presentation refinement | Reduce the scene's light-shaft strength from 0.68 to 0.12, fade distant water smoothly, and replace the underwater preview with a new scene capture | [eccdb6d](https://github.com/supercoderrrrr/FFT-Ocean-URP/commit/eccdb6d48e67028350f4938f892d60c85e0d200a) |

The standalone development player was built and run using Unity 2022.3.62f2 with Direct3D 11. The showcase capture completed without shader or runtime errors. The capture output demonstrates the scene's appearance; it is not a frame-rate benchmark.

### Future work

1. Add numerical regression tests for the inverse FFT, spectral band boundaries, and surface-height sampling.
2. Profile compute passes, clipmap rendering, and underwater composition separately across several FFT resolutions.
3. Validate additional graphics APIs and GPUs, including depth reconstruction and waterline transitions.
4. Improve underwater receivers, shadow-aware caustics, and scene lighting to support more varied demonstration environments.

These are follow-up directions rather than completed features. Current implementation details are documented in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

---

## 简体中文

### 为什么首次 Git 提交包含完整项目

本项目从个人对频谱海洋模拟和 Unity URP 实时水体渲染的学习逐步迭代，开发阶段使用 Unity Version Control / Plastic SCM 保存版本记录。完成实现后，项目迁移到 GitHub，用于作品集展示与后续维护。

首次 Git 提交保存了迁移时的完整实现，包括已经完成的水下渲染与投影焦散改进。迁移前的开发记录保留在 Unity Version Control 中，迁移后的修改继续通过本仓库的 Git 历史记录。

本文件按技术阶段整理实现的演进过程，属于开发归档，并非原始 changeset 记录的直接导出。

### 迁移前的实现演进

下面整理了项目实现及其迭代，代码入口链接指向当前版本。

| 阶段 | 内容 | 当前代码入口 |
| --- | --- | --- |
| GPU FFT 流程 | 复数频谱随时间演化、位反转、水平与竖直反向 FFT 蝶形运算、空间域位移组装 | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute)、[FFTOceanController](Assets/FFT-Ocean/Scripts/FFTOceanController.cs) |
| 频谱海况 | JONSWAP 能量分布、TMA 有限水深修正、方向扩散、三层互不重叠的波长范围 | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute)、[FFTOceanController](Assets/FFT-Ocean/Scripts/FFTOceanController.cs) |
| 浪形与泡沫 | Choppy 水平位移、基于导数的法线重建、Jacobian 压缩历史驱动的破碎浪花泡沫 | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute)、[FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| 大范围海面展示 | 八层相机跟随 Clipmap、网格吸附、边缘形变、基于距离的频谱级联权重 | [FFTOceanMesh](Assets/FFT-Ocean/Scripts/FFTOceanMesh.cs)、[FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| 海面渲染 | Fresnel 反射、主光高光、浪尖次表面颜色、远景粗糙度、水面底侧独立着色 | [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| 水下渲染 | GPU 水面高度采样、水线上下切换迟滞、RGB 吸收、散射、屏幕扰动和方向光束 | [FFTOceanUnderwaterController](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterController.cs)、[FFTOceanUnderwaterFeature](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterFeature.cs) |
| 投影焦散 | 将计算焦散实验替换为灰度贴图投影，通过两层反向滚动保持投影中心稳定 | [FFTOceanUnderwaterURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanUnderwaterURP.shader) |
| 展示场景整理 | 调整材质和海况参数，加入自由相机控制，提供编辑器场景生成工具 | [FFTOceanSceneBuilder](Assets/FFT-Ocean/Editor/FFTOceanSceneBuilder.cs)、[FFTOceanShowcaseCamera](Assets/FFT-Ocean/Scripts/FFTOceanShowcaseCamera.cs) |

### 迁移后的开发记录

| 更新 | 内容 | 记录 |
| --- | --- | --- |
| 独立工程导入 | 发布包含场景、URP 配置、运行时代码、Shader、依赖和双语文档的 Unity 工程 | [f85a781](https://github.com/supercoderrrrr/FFT-Ocean-URP/commit/f85a781cef4dc6a1f3d9b1622fef37d8363cf201) |
| 可重复的展示采集 | 加入开发版采集脚本和运行时参数接口，录制三档海况变化、GIF、MP4 与水下截图 | [23cafde](https://github.com/supercoderrrrr/FFT-Ocean-URP/commit/23cafde9b495300329b46c3f9dacdcf572a3aafc) |
| 水下展示优化 | 将场景光束强度从 0.68 降至 0.12，加入远景水色平滑渐隐，并用重新渲染的水下截图更新展示 | [eccdb6d](https://github.com/supercoderrrrr/FFT-Ocean-URP/commit/eccdb6d48e67028350f4938f892d60c85e0d200a) |

独立开发版播放器已在 Unity 2022.3.62f2 和 Direct3D 11 环境下完成构建与运行，展示采集未发现 Shader 或运行时错误。采集素材用于展示场景效果，不作为帧率性能测试。

### 后续方向

1. 补充反向 FFT、频谱范围边界和水面高度采样的数值回归测试。
2. 分别测量不同 FFT 分辨率下的计算着色器、Clipmap 渲染和水下合成开销。
3. 验证更多图形 API 与显卡，重点检查深度重建和水线上下切换。
4. 完善水下受光物体、带阴影的焦散及场景光照，支持更多展示环境。

这些属于后续改进方向，当前实现细节见 [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)。
