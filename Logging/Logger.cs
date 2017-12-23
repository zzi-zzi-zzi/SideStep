using System;
using System.Globalization;
using System.Reflection;
using Clio.Utilities;
using Sidestep.Helpers;
using rLogging = ff14bot.Helpers.Logging;

namespace Sidestep.Logging
{
    internal static class Logger
    {
       
        private static string Prefix => $"[SideStep] ";

        
        [StringFormatMethod("format")]
        internal static void Error(string format, params object[] args)
        {
            rLogging.Write(LogColors.Error, Prefix + string.Format(CultureInfo.InvariantCulture, format, args));
        }

        [StringFormatMethod("format")]
        internal static void Info(string format, params object[] args)
        {
            rLogging.Write(LogColors.Info, Prefix + string.Format(CultureInfo.InvariantCulture, format, args));
        }
        
        [StringFormatMethod("format")]
        internal static void Verbose(string format, params object[] args)
        {
            rLogging.WriteVerbose(LogColors.Verbose, Prefix + string.Format(CultureInfo.InvariantCulture, format, args));
        }
        
        [StringFormatMethod("format")]
        internal static void Warn(string format, params object[] args)
        {
            rLogging.Write(LogColors.Warn, Prefix + string.Format(CultureInfo.InvariantCulture, format, args));
        }
    }
}