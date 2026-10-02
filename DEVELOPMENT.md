# Development / 开发记录

[English](#english) · [简体中文](#简体中文)

## English

Development was tracked with Unity Version Control / Plastic SCM before the project moved to GitHub. The first Git commit contains the implementation at the time of migration.

### Implementation

| Stage | Changes | Code |
| --- | --- | --- |
| FFT simulation | Complex spectrum evolution, two-dimensional inverse FFT, and displacement assembly | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute) |
| Sea state | JONSWAP/TMA spectrum, directional spreading, and three wavelength bands | [FFTOceanController](Assets/FFT-Ocean/Scripts/FFTOceanController.cs) |
| Wave shape and foam | Horizontal displacement, derivative normals, and Jacobian compression history | [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| Ocean mesh | Eight-level clipmap, camera following, and edge morphing | [FFTOceanMesh](Assets/FFT-Ocean/Scripts/FFTOceanMesh.cs) |
| Surface rendering | Fresnel reflection, main-light highlights, crest tint, and underside shading | [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| Underwater | Surface-height sampling, waterline transitions, absorption, and scattering | [FFTOceanUnderwaterController](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterController.cs), [FFTOceanUnderwaterFeature](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterFeature.cs) |
| Caustics | Replace computed caustics with two counter-scrolling texture layers projected onto underwater geometry | [FFTOceanUnderwaterURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanUnderwaterURP.shader) |
| Scene tools | Scene builder and free camera controls | [FFTOceanSceneBuilder](Assets/FFT-Ocean/Editor/FFTOceanSceneBuilder.cs), [FFTOceanShowcaseCamera](Assets/FFT-Ocean/Scripts/FFTOceanShowcaseCamera.cs) |

### Validation

Unity 2022.3.62f2, URP 14.0.12, Windows, Direct3D 11. The development player builds and runs, including parameter changes and underwater captures.

See [architecture](docs/ARCHITECTURE.md) for implementation details and [Git history](https://github.com/supercoderrrrr/FFT-Ocean-URP/commits/main/) for later changes.

---

## 简体中文

开发阶段使用 Unity Version Control / Plastic SCM 管理版本，之后迁移到 GitHub。首次 Git 提交保存了迁移时的实现。

### 实现演进

| 阶段 | 修改内容 | 代码 |
| --- | --- | --- |
| FFT 模拟 | 复数频谱演化、二维反向 FFT 与位移组装 | [FFTOcean.compute](Assets/FFT-Ocean/Compute/FFTOcean.compute) |
| 海况 | JONSWAP/TMA 频谱、方向扩散与三层波长范围 | [FFTOceanController](Assets/FFT-Ocean/Scripts/FFTOceanController.cs) |
| 浪形与泡沫 | 水平位移、导数法线与 Jacobian 压缩历史 | [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| 海面网格 | 八层 Clipmap、相机跟随与边缘形变 | [FFTOceanMesh](Assets/FFT-Ocean/Scripts/FFTOceanMesh.cs) |
| 海面渲染 | Fresnel 反射、主光高光、浪尖颜色与水面底侧着色 | [FFTOceanURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanURP.shader) |
| 水下 | 水面高度采样、水线切换、吸收与散射 | [FFTOceanUnderwaterController](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterController.cs)、[FFTOceanUnderwaterFeature](Assets/FFT-Ocean/Scripts/FFTOceanUnderwaterFeature.cs) |
| 焦散 | 将计算焦散替换为双层反向滚动贴图，投影到水下物体 | [FFTOceanUnderwaterURP.shader](Assets/FFT-Ocean/Shaders/FFTOceanUnderwaterURP.shader) |
| 场景工具 | 场景生成工具与自由相机控制 | [FFTOceanSceneBuilder](Assets/FFT-Ocean/Editor/FFTOceanSceneBuilder.cs)、[FFTOceanShowcaseCamera](Assets/FFT-Ocean/Scripts/FFTOceanShowcaseCamera.cs) |

### 验证环境

Unity 2022.3.62f2、URP 14.0.12、Windows、Direct3D 11。开发版播放器可正常构建和运行，已验证参数切换与水下画面采集。

实现细节见[架构文档](docs/ARCHITECTURE.md)，后续修改见 [Git 提交记录](https://github.com/supercoderrrrr/FFT-Ocean-URP/commits/main/)。
