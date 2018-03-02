using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Configuration;

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
                FileInfo configFile = null;
                IEnumerable<string> allFiles;
                #if NET_3_5
                allFiles = Directory.GetFiles(projectFile.DirectoryName);
                #else
                allFiles = Directory.EnumerateFiles(projectFile.DirectoryName);
                #endif

                foreach (string file in allFiles)
                {
                    string filename = file.ToLower();
                    if (filename.EndsWith("app.config") || filename.EndsWith("web.config")) 
                    {
                        string contents = File.ReadAllText(file);
                        if (contents.Contains("TrinitySettings namespace=\"Semiodesk.Trinity.Test\")"));
                        {
                            configFile = new FileInfo(file);
                            break;
                        }
                    }

                    if( filename.EndsWith("ontologies.config"))
                    {
                        configFile = new FileInfo(file);
                    }       
                }


                Program p = new Program(logger);
                p.SetConfig(configFile.FullName);
                if (string.IsNullOrEmpty(IntermediatePath))
                {
                    IntermediatePath = projectFile.Directory.FullName;
                }
                else
                {
                    IntermediatePath = Path.Combine(projectFile.DirectoryName, IntermediatePath);
                }
                string targetFile = Path.Combine(IntermediatePath, "Ontologies.g.cs");
                p.SetTarget(targetFile);
                var res = p.Run();

                OutputFiles = new TaskItem[] { new TaskItem(targetFile) };

                return 0 == res;
            }
            catch (Exception)
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
