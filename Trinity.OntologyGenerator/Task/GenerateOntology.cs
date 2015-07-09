using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Semiodesk.Trinity.OntologyGenerator
{
    /// <summary>
    /// 
    /// </summary>
    // http://bartdesmet.net/blogs/bart/archive/2008/02/15/the-custom-msbuild-task-cookbook.aspx
    // http://stackoverflow.com/questions/2961753/how-to-hide-files-generated-by-custom-tool-in-visual-studio
    public class GenerateOntology : ITask
    {
        #region ITask Members

        public IBuildEngine BuildEngine
        {
            get;
            set;
        }

        [Required]
        public string IntermediatePath
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

        [Output]
        public ITaskItem[] OutputFiles
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
                FileInfo configFile = new FileInfo(Path.Combine(projectFile.DirectoryName, "App.config"));
                if (!configFile.Exists)
                    configFile = new FileInfo(Path.Combine(projectFile.DirectoryName, "Web.config"));

                Program p = new Program(logger);
                p.LoadConfigFile(configFile.FullName);
                if (string.IsNullOrEmpty(IntermediatePath))
                {
                    IntermediatePath = projectFile.Directory.FullName;
                }
                else
                {
                    IntermediatePath = Path.Combine(projectFile.DirectoryName, IntermediatePath);
                }
                string targetFile = Path.Combine(IntermediatePath, "Ontologies.g.cs");
                p.SetGenerate(targetFile);
                var res = p.Run();

                OutputFiles = new TaskItem[] { new TaskItem(targetFile) };

                return 0 == res;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public ITaskHost HostObject
        {
            get;
            set;
        }

        #endregion
    }
}
