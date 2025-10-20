using System.Reflection;

namespace JonjubNet.Metrics.Services
{
    /// <summary>
    /// Servicio para detectar automáticamente información del servicio actual
    /// </summary>
    public class ServiceDiscoveryService
    {
        /// <summary>
        /// Detecta automáticamente el nombre del servicio
        /// </summary>
        public static string DetectServiceName()
        {
            try
            {
                // Intentar obtener el nombre del ensamblado principal
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var assemblyName = entryAssembly.GetName().Name;
                    if (!string.IsNullOrEmpty(assemblyName))
                    {
                        // Limpiar el nombre del ensamblado para obtener el nombre del servicio
                        return CleanServiceName(assemblyName);
                    }
                }

                // Fallback: usar el nombre del proceso actual
                var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                return CleanServiceName(processName);
            }
            catch
            {
                return "UnknownService";
            }
        }

        /// <summary>
        /// Detecta automáticamente la versión del servicio
        /// </summary>
        public static string DetectServiceVersion()
        {
            try
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var version = entryAssembly.GetName().Version;
                    if (version != null)
                    {
                        return version.ToString();
                    }
                }

                return "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        /// <summary>
        /// Detecta automáticamente el entorno de ejecución
        /// </summary>
        public static string DetectEnvironment()
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                                ?? Environment.GetEnvironmentVariable("ENVIRONMENT");
                
                return !string.IsNullOrEmpty(environment) ? environment : "Development";
            }
            catch
            {
                return "Development";
            }
        }

        /// <summary>
        /// Detecta automáticamente la información del host
        /// </summary>
        public static string DetectHostName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "UnknownHost";
            }
        }

        /// <summary>
        /// Detecta automáticamente la información del proceso
        /// </summary>
        public static int DetectProcessId()
        {
            try
            {
                return Environment.ProcessId;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Limpia el nombre del servicio removiendo prefijos comunes
        /// </summary>
        private static string CleanServiceName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "UnknownService";

            // Remover prefijos comunes
            var prefixes = new[] { "JonjubNet.", "Company.", "MyApp.", "Api.", "Service.", "Web." };
            
            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length);
                    break;
                }
            }

            // Remover sufijos comunes
            var suffixes = new[] { ".Api", ".Service", ".Web", ".Application", ".Host" };
            
            foreach (var suffix in suffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    break;
                }
            }

            return string.IsNullOrEmpty(name) ? "UnknownService" : name;
        }
    }
}
