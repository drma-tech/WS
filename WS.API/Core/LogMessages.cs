using Microsoft.Extensions.Logging;

namespace WS.API.Core
{
    internal static partial class LogMessages
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "{method} - Id {Id}, RequestCharge {RequestCharge}")]
        public static partial void RequestCharge(this ILogger logger, string method, string id, double requestCharge);

        [LoggerMessage(Level = LogLevel.Warning, Message = "params:{Custom_Params}, version:{Custom_AppVersion}, ip:{Custom_Ip}")]
        public static partial void Error(this ILogger logger, Exception? exception, string? custom_Params, string? custom_AppVersion, string? custom_Ip);

        [LoggerMessage(Level = LogLevel.Warning, Message = "message:{Custom_Message}, params:{Custom_Params}, version:{Custom_AppVersion}, ip:{Custom_Ip}")]
        public static partial void Warning(this ILogger logger, string? custom_Message, string? custom_Params, string? custom_AppVersion, string? custom_Ip);
    }
}