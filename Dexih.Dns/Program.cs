using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dexih.Dns
{
    public class Program
    {
        public enum EExitCode
        {
            Success = 0,
            InvalidSetting = 1,
            InvalidLogin = 2,
            Terminated = 3,
            UnknownError = 10,
            Upgrade = 20
        }

        public static int Main(string[] args)
        {
            var mutex = new Mutex(true, "dexih.dns", out var createdNew);
    
            if (!createdNew)
            {
                Console.WriteLine("Only one instance of the dns agent is allowed.");
                return (int) EExitCode.Terminated;
            }
            
            var returnValue = StartAsync(args).Result;
            
            mutex.ReleaseMutex();
            
            return returnValue;
        }

        public static async Task<int> StartAsync(string[] args)
        {
            Welcome();
            WriteVersion();

            // create a temporary logger (until the log level settings have been loaded.
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger("main");

            var configDirectory = Environment.GetEnvironmentVariable("DEXIH_CONFIG_DIRECTORY");
            if (string.IsNullOrEmpty(configDirectory))
            {
                configDirectory = Directory.GetCurrentDirectory();
            }

            var settingsFile = Path.Combine(configDirectory, "appsettings.json");
            if (args.Length >= 2 && args[0] == "-appsettings")
            {
                settingsFile = args[1];
            }

            //check config file first for any settings.
            if (File.Exists(settingsFile))
            {
                logger.LogInformation($"Reading settings from the file {settingsFile}.");
            }
            else
            {
                logger.LogInformation($"Could not find the settings file {settingsFile}.");
            }
            
            var hostBuilder = new HostBuilder()
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(configDirectory);
                    configHost.AddJsonFile(settingsFile, optional: true);
                    configHost.AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    // add user secrets when development mode
                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        configApp.AddUserSecrets<Program>();
                    }

                    configApp.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddHostedService<DnsService>();
                })
                .UseConsoleLifetime();

            var host = hostBuilder.Build();
            
            try
            {
                await host.RunAsync();
            }
            catch (OperationCanceledException)
            {
                
            }
            
            host.Dispose();
            
            return (int)EExitCode.Success;;
        }
        
        private static void Welcome()
        {
            Console.WriteLine(@"
 _______   _______ ___   ___  __   __    __  
|       \ |   ____|\  \ /  / |  | |  |  |  | 
|  .--.  ||  |__    \  V  /  |  | |  |__|  | 
|  |  |  ||   __|    >   <   |  | |   __   | 
|  '--'  ||  |____  /  .  \  |  | |  |  |  | 
|_______/ |_______|/__/ \__\ |__| |__|  |__| 

Welcome to Dexih - The Data Experts Integration Hub
");
            
            // introduction message, with file version
            var runtimeVersion = Assembly.GetEntryAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            Console.WriteLine($"Remote Agent - Version {runtimeVersion}");
            
        }
        
        private static void WriteVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            var localVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            File.WriteAllText(assembly.GetName().Name + ".version", localVersion);
        }

    }
}
