using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace Semiodesk.Trinity.OntologyDeployment
{
    /// <summary>
    /// Logs for a MSBuild Task
    /// </summary>
    public class TaskLogger : ILogger
    {
        #region Members
        IBuildEngine _buildEngine;
        #endregion

        #region Constructor
        public TaskLogger(IBuildEngine buildEngine)
        {
            _buildEngine = buildEngine;
        }
        #endregion

        #region Methods
        public void LogMessage(string format, params object[] args)
        {
            _buildEngine.LogMessageEvent(new BuildMessageEventArgs(string.Format(format, args), "Semiodesk", "OntologyGenerator", MessageImportance.Normal));
        }

        public void LogWarning(string message, ElementInformation info)
        {
            _buildEngine.LogWarningEvent(new BuildWarningEventArgs("Semiodesk.Trinity", "", info.Source, info.LineNumber, 0, info.LineNumber, 0, message, "Semiodesk", "OntologyGenerator", DateTime.Now));
        }

        public void LogWarning(string format, params object[] args)
        {
            _buildEngine.LogWarningEvent(new BuildWarningEventArgs("Semiodesk.Trinity", "", "", 0, 0, 0, 0, string.Format(format, args), "Semiodesk", "OntologyGenerator", DateTime.Now));
        }

        public void LogError(string format, params object[] args)
        {
            _buildEngine.LogErrorEvent(new BuildErrorEventArgs("Semiodesk.Trinity", "", "", 0, 0, 0, 0, string.Format(format, args), "Semiodesk", "OntologyGenerator", DateTime.Now));
        }
        #endregion
    }
}
