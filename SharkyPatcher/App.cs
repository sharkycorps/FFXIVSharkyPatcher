
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharkyPatcher.Common;
using SharkyPatcher.Patcher;
using static SharkyPatcher.Common.LoggerUtil;

namespace SharkyPatcher
{
    class App
    {
        static readonly string _launcherName = "XIVLauncherCN.exe";
        static readonly string _launcherCommonName = "XIVLauncher.Common.dll";
        static readonly string _updaterName = "Dalamud.Updater.exe";
        static readonly string _dalamudName = "Dalamud.dll";
        static readonly string _currentDir = Directory.GetCurrentDirectory();
        static readonly string _dalamudBaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN");

        static async Task Main()
        {
            string processToStart = "";

            try
            {
                // render sharky ascii art
                SharkyArt.Render();
                Log.Information("【鯊鯊補丁】正在啟動……");

                // patch xiv launcher
                if (CheckExists(_launcherName))
                {
                    Log.Information("==============================================================");

                    processToStart = _launcherName;
                    string launcherVersion = await GetLauncherVersionAsync();
                    string commonSubPath = Path.Combine(launcherVersion, _launcherCommonName);
                    if (CheckExists(commonSubPath))
                    {
                        BackupFile(commonSubPath);
                        XIVCommonPatcher launcherPatcher = new XIVCommonPatcher(
                            Path.Combine(_currentDir, launcherVersion), _launcherCommonName);
                        launcherPatcher.Patch();
                        launcherPatcher.Dispose();
                        await PatchDalamud(_dalamudBaseDir, _dalamudName);
                    }
                    else
                    {
                        Log.Information($"【鯊鯊補丁】<{launcherVersion}> 下未找到 <{_launcherCommonName}>，請確認目錄是否正確");
                        Exit();
                    }
                }
                else if (CheckExists(_updaterName))
                {
                    Log.Information("==============================================================");

                    processToStart = _updaterName;
                    BackupFile(_updaterName);
                    DUpdaterPatcher updaterPatcher = new DUpdaterPatcher(_currentDir, _updaterName);
                    updaterPatcher.Patch();
                    updaterPatcher.Dispose();
                    await PatchDalamud(_dalamudBaseDir, _dalamudName);
                }

                if (!string.IsNullOrEmpty(processToStart))
                {
                    Log.Information("==============================================================");
                    Log.Information("【鯊鯊補丁】補丁全部應用成功！");
                    Log.Information("==============================================================");
                    StartProcess(processToStart);
                    Log.Information("==============================================================");
                }
                else
                {
                    LogF.Information($"【鯊鯊補丁】當前目錄：{_currentDir}");
                    Log.Information("【鯊鯊補丁】當前目錄下未找到啟動器或者更新器，請確認目錄是否正確");
                    Exit();
                }

                ExitSuccess();
            }
            catch (Exception ex)
            {
                Log.Information("==============================================================");
                Log.Error("【鯊鯊補丁】補丁應用過程中出現異常！");
                Exit(ex);
            }
            finally
            {
                Serilog.Log.CloseAndFlush();
            }
        }

        static async Task PatchDalamud(string baseDir, string dalamudName)
        {
            Log.Information("==============================================================");

            bool isStaging = GetDalamudIsStaging(baseDir);
            DalamudVersionInfo dalamudVersion = await GetDalamudVersionAsync(isStaging);
            string dalamudPath = Path.Combine(baseDir, @"addon\Hooks", dalamudVersion.AssemblyVersion);
            bool dalamudExists = CheckExists(Path.Combine(dalamudPath, dalamudName));
            if (dalamudExists)
            {
                BackupFile(Path.Combine(dalamudPath, dalamudName));
                DalamudPatcher patcher = new DalamudPatcher(baseDir, dalamudVersion);
                patcher.Patch();
                patcher.Dispose();
            }
            else
            {
                Log.Information($"【鯊鯊補丁】<{dalamudPath}> 下未找到 <{dalamudName}>，請確認目錄是否正確");
                Exit();
            }
        }
        
        static bool GetDalamudIsStaging(string baseDir) {
            Log.Information("【鯊鯊補丁】獲取框架發行分支中……");

            string dalamudConfigPath = Path.Combine(baseDir, "dalamudConfig.json");
            JObject dalamudConfigObj = JObject.Parse(File.ReadAllText(dalamudConfigPath));
            string dalamudBetaKey = dalamudConfigObj.GetValue("DalamudBetaKey").Value<string>();

            LogF.Information($"【鯊鯊補丁】框架 Beta Key：{dalamudBetaKey}");

            if (dalamudBetaKey != null) {
                return true;
            }
            
            return false;
        }

        static async Task<DalamudVersionInfo> GetDalamudVersionAsync(bool isStaging)
        {
            Log.Information("【鯊鯊補丁】獲取框架版本中……");

            string track = isStaging ? "staging" : "release";
            HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            string versionJson = await client.GetStringAsync($"https://aonyx.ffxiv.wang/Dalamud/Release/VersionInfo?track={track}").ConfigureAwait(false);
            DalamudVersionInfo dalamudVersion = JsonConvert.DeserializeObject<DalamudVersionInfo>(versionJson);
            LogF.Information($"【鯊鯊補丁】框架版本：{versionJson}");
            Log.Information($"【鯊鯊補丁】當前 Dalamud 發行分支：<{track}>");
            Log.Information($"【鯊鯊補丁】當前 Dalamud 版本：<{dalamudVersion.AssemblyVersion}>");

            return dalamudVersion;
        }
        
        static async Task<string> GetLauncherVersionAsync()
        {
            Log.Information("【鯊鯊補丁】獲取啟動器版本中……");

            // HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };
            // client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            // string versionJson = await client.GetStringAsync("https://aonyx.ffxiv.wang/Launcher/GetLease").ConfigureAwait(false);
            // JObject launcherVersion = JObject.Parse(versionJson);
            // string releasesList = launcherVersion.GetValue("releasesList").Value<string>();
            // string latestRelease = releasesList.Split('\n').Last();
            // string nupkgVersion = latestRelease.Split().ElementAt(1);
            // string appVersion = nupkgVersion.Replace("XIVLauncherCN", "app")
            //     .Replace("-full.nupkg", "")
            //     .Replace("-delta.nupkg", "");
            // LogF.Information($"【鯊鯊補丁】啟動器版本：{releasesList}");
            string appVersion = "current";
            Log.Information($"【鯊鯊補丁】當前啟動器版本：<{appVersion}>");

            return appVersion;
        }

        static void StartProcess(string fileName) {
            string programPath = Path.Combine(_currentDir, fileName);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = programPath,
                UseShellExecute = true
            };

            Process process = Process.Start(startInfo);
            if (process != null)
            {
                Log.Information($"【鯊鯊補丁】<{fileName}> 已啟動");
            }
            else
            {
                Log.Error($"【鯊鯊補丁】<{fileName}> 啟動失敗，請嘗試手動啟動");
            }
        }

        static bool CheckExists(string fileName)
        {
            string filePath = fileName;
            bool fileExists = File.Exists(filePath);
            if (!fileExists)
            {
                return false;
            }

            return true;
        }

        static void BackupFile(string fileName) {
            string filePath = fileName;

            try
            {
                string bakFilePath = filePath + ".spbak";
                bool bakFileExists = File.Exists(bakFilePath);
                if (!bakFileExists)
                {
                    File.Copy(filePath, bakFilePath, overwrite: true);
                    Log.Information($"【鯊鯊補丁】<{fileName}> 備份文件已創建");
                }
                else
                {
                    Log.Information($"【鯊鯊補丁】<{fileName}> 備份文件已存在");
                }
            }
            catch (Exception ex)
            {
                Log.Error("【鯊鯊補丁】備份文件創建失敗！");
                Exit(ex);
            }
        }
    }
}