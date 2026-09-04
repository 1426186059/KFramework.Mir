// 兼容壳全局 using：还原被排除的原版 Mir3/Client/GlobalUsings.cs 内容，
// 并补 System.Windows.Forms，使原版游戏层在浏览器 WASM 下能编过。
// 真实交互/显示后续用 Canvas 逐个替换（见 Shims/README）。
global using Shared.Envir;
global using Shared.Rendering;
global using System.Windows.Forms;
