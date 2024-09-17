<p align="center">
  <img width="256" height="256" src="https://repository-images.githubusercontent.com/835197623/90745e81-40af-48c2-aaac-1c335ac079cf">
</p>

# FFXIV Sharky Patcher 🦈 鯊鯊補丁

[![Apply Patches and Build](https://github.com/sharkycorps/FFXIVSharkyPatcher/actions/workflows/apply-patches-and-build.yml/badge.svg)](https://nightly.link/sharkycorps/FFXIVSharkyPatcher/workflows/apply-patches-and-build/main/patched-artifact)

Patches which remove unreasonable ToS, analytics, plugin bans and integrity check from Dalamud and XIVLauncher CN.

鯊鯊補丁的目的是移除 Dalamud 和 XIVLauncher CN 中不合理的服務條款、分析、插件禁令和完整性檢查。

## Usage 🦈 使用

Download the zip file from the automatic build above and overwrite the contents of the XIVLauncherCN or Dalamud.Updater installation folder. 
Not using the Github Releases for transparency.

從 [自動構建](https://nightly.link/sharkycorps/FFXIVSharkyPatcher/workflows/apply-patches-and-build/main/patched-artifact) 中下載 zip 文件，並覆蓋 XIVLauncherCN 或者 Dalamud.Updater 安裝資料夾中的內容。
為了透明起見，暫時不用 Github Releases。

## Community 🦈 社区

Discord：https://discord.gg/6XQbvNgn

## Safety 🦈 安全性
### Diffs between goatcorp and ottercorp

goatcorp 和 ottercorp 代碼的區別如下，sharkycorps 只進行了必要的代碼移除，請自行檢查。

https://github.com/goatcorp/Dalamud/compare/master...ottercorp:master
https://github.com/goatcorp/FFXIVQuickLauncher/compare/master...ottercorp:FFXIVQuickLauncher:CN