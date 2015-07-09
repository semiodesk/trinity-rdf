using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace Semiodesk.Trinity.OntologyDeployment.Task
{
    public class DeployOntology : ITask
    {
        #region ITask Members

        public IBuildEngine BuildEngine
        {
            get;
            set;
        }

        [Required]
        public string ProjectPath
        {
            get;
            set;
        }

        public bool Execute()
        {
            try
            {

                var logger = new TaskLogger(BuildEngine);
                FileInfo projectFile = new FileInfo(ProjectPath);
                FileInfo configFile = new FileInfo(Path.Combine(projectFile.DirectoryName, "app.config"));
                if (!configFile.Exists)
                    configFile = new FileInfo(Path.Combine(projectFile.DirectoryName, "web.config"));

                Program p = new Program(logger);
                if (p.LoadConfigFile(configFile.FullName))
                {
                    if (p.Initialized)
                    {
                        int res = p.Run();
                        return res == 0;
                    }
                    else
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {

            }
            return false;


        }

        public ITaskHost HostObject
        {
            get;
            set;
        }

        #endregion
    }
}
