using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace Semiodesk.Trinity.OntologyDeployment.Task
{
    class DeployOntology : ITask
    {
        #region ITask Members

        public IBuildEngine BuildEngine
        {
            get;
            set;
        }

        public bool Execute()
        {
            
            //BuildEngine.ProjectFileOfTaskNode
             //FileInfo assembly = System.Reflection.Assembly.GetExecutingAssembly().Location

            return true;
        }

        public ITaskHost HostObject
        {
            get;
            set;
        }

        #endregion
    }
}
