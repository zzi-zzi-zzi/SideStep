﻿/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */
using System;
using System.Diagnostics;
using Sidestep.Logging;

namespace Sidestep.Helpers
{
    [DebuggerStepThrough]
    internal class PerformanceLogger : IDisposable
    {
        private readonly string _BlockName;
        private readonly Stopwatch _Stopwatch;
        private bool _IsDisposed;
        private bool _ForceLog;

        public PerformanceLogger(string blockName, bool forceLog = false)
        {
            _ForceLog = forceLog;
            _BlockName = blockName;
            _Stopwatch = new Stopwatch();
            _Stopwatch.Start();
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_IsDisposed) return;
            _IsDisposed = true;
            _Stopwatch.Stop();
            if (_Stopwatch.Elapsed.TotalMilliseconds > 5 || _ForceLog)
            {
                if (_Stopwatch.Elapsed.TotalMilliseconds > 1000)
                {
                    Logger.Error("[Performance] Execution of \"{0}\" took {1:00.00000}ms.", _BlockName,
                        _Stopwatch.Elapsed.TotalMilliseconds);
                }
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        ~PerformanceLogger()
        {
            Dispose();
        }
    }
}