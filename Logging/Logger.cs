/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */
using System;
using System.Globalization;
using System.Reflection;
using Clio.Utilities;
using ff14bot.Helpers;
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
            rLogging.WriteToFileSync(LogLevel.Verbose, Prefix + string.Format(CultureInfo.InvariantCulture, format, args));
        }
        
        [StringFormatMethod("format")]
        internal static void Warn(string format, params object[] args)
        {
            rLogging.Write(LogColors.Warn, Prefix + string.Format(CultureInfo.InvariantCulture, format, args));
        }
    }
}