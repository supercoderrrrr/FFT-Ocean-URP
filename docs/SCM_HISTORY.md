# Source control history

## English

The FFT ocean was developed in a larger Unity project that also contains a Boids simulation. That workspace uses Plastic SCM / Unity Version Control, not Git

The GitHub repository is intentionally created as one clean export snapshot. Multiple artificial Git commits were not generated after the fact because that would imply a development history that did not occur in Git

Relevant original Plastic SCM changesets:

| Changeset | Date | Original scope |
|---:|---|---|
| 1 | 2026-08-03 | Initial Boids architecture and steering behaviours |
| 2 | 2026-08-04 | Boids optimisation |
| 3 | 2026-08-04 | Environment, post-processing, underwater fog, and current distortion |
| 4 | 2026-08-05 | Initial caustics effect |
| 5 | 2026-08-16 | URP FFT ocean with JONSWAP/TMA, three cascades, IFFT, clipmap, foam, and portfolio scene |

The final export also includes later working-copy improvements to underwater rendering and projected caustics. Those files were audited as part of the release snapshot

Suggested final Plastic SCM check-in comment:

```text
feat(FFT Ocean): finalize the standalone portfolio implementation

- complete the three-cascade JONSWAP/TMA GPU spectrum and inverse FFT pipeline
- preserve Jacobian compression history for coherent breaking-wave foam
- finalize camera-following clipmap geometry and URP ocean lighting
- add stable above-water and underwater rendering transitions
- replace computed caustics experiments with two-layer projected caustics
- tune portfolio scene defaults and validate Direct3D 11 Play Mode
- prepare the FFT Ocean assets for an honest standalone Git export
```

## 中文

FFT 海洋最初开发在一个同时包含 Boids 集群模拟的 Unity 大工程中，原工作区使用 Plastic SCM / Unity Version Control，而不是 Git

GitHub 仓库会以一次干净的导出快照开始，不会在事后伪造多条 Git 提交来假装开发过程原本发生在 Git 中

与本功能有关的原 Plastic SCM 记录：

| Changeset | 日期 | 原始内容 |
|---:|---|---|
| 1 | 2026-08-03 | Boids 基础结构与 Steering Behaviour |
| 2 | 2026-08-04 | Boids 优化 |
| 3 | 2026-08-04 | 环境、后处理、水下雾和洋流扰动 |
| 4 | 2026-08-05 | 初版焦散效果 |
| 5 | 2026-08-16 | URP FFT 海洋、JONSWAP/TMA、三层频谱、IFFT、Clipmap、泡沫和作品集场景 |

最终导出还包含之后在工作副本中完成的水下渲染与投影焦散改进，这些文件在发布快照中重新做了依赖与隐私审计

建议用于最后一次 Plastic SCM 提交的中文日志：

```text
feat(FFT Ocean): 完成独立作品集版本

- 完成三层 JONSWAP/TMA GPU 频谱与反向 FFT 流程
- 使用 Jacobian 压缩历史生成连贯的破碎浪花泡沫
- 完成相机跟随 Clipmap 网格与 URP 海面光照
- 完善水上与水下的稳定切换和水下光学效果
- 将计算焦散实验替换为双层相对偏移的投影焦散
- 调整作品集场景默认参数并通过 Direct3D 11 Play Mode 验证
- 整理 FFT Ocean 资源，用于真实且可说明来源的独立 Git 导出
```

