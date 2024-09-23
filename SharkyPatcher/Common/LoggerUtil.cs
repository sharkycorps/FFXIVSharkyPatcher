using System;
using System.Threading;
using Serilog;

namespace SharkyPatcher.Common
{
    public static class LoggerUtil
    {
        private static readonly ILogger _fLogger;
        private static readonly ILogger _cLogger;
        static LoggerUtil()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Logger(lc => lc
                    .Filter.ByExcluding(e => e.Properties.ContainsKey("ConsoleOnly"))
                    .WriteTo.File("SharkyPatcher.log"))
                .WriteTo.Logger(lc => lc
                    .Filter.ByExcluding(e => e.Properties.ContainsKey("FileOnly"))
                    .WriteTo.Console())
                .CreateLogger();
            _cLogger = Serilog.Log.ForContext("ConsoleOnly", true);
            _fLogger = Serilog.Log.ForContext("FileOnly", true);
        }

        public static ILogger Log
        {
            get
            {
                return Serilog.Log.Logger;
            }
        }
        public static ILogger LogF
        {
            get
            {
                return _fLogger;
            }
        }
        public static ILogger LogC
        {
            get
            {
                return _cLogger;
            }
        }

        public static void ExitSuccess()
        {
            LogC.Information("本程序完全開源免費，請確保從鯊鯊軍團官方鏈接下載 https://github.com/sharkycorps");
            LogC.Information("窗口將在20秒後自動關閉……");
            Serilog.Log.CloseAndFlush();
            Thread.Sleep(20000);
            Environment.Exit(0);
        }
        public static void Exit()
        {
            LogC.Error($"【鯊鯊補丁】已中止操作，請排除問題後重新執行，按任意鍵退出……");
            Serilog.Log.CloseAndFlush();
            Console.ReadKey();
            Environment.Exit(1);
        }
        public static void Exit(Exception ex)
        {
            // exit with logs
            Log.Error($"【鯊鯊補丁】原因：{ex.Message}");
            LogF.Error($"【鯊鯊補丁】堆疊：{ex.StackTrace}");
            Exit();
        }
    }
}