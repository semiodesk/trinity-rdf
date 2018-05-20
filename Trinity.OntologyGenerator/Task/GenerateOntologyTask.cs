using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Semiodesk.Trinity.OntologyGenerator
{
    /// <summary>
    /// MSBuild task which parses the ontologies in the project and generates static vocabulary 
    /// classes which provide access to the terms in code using the ontology namespace prefix.
    /// </summary>
    /// <see href="http://bartdesmet.net/blogs/bart/archive/2008/02/15/the-custom-msbuild-task-cookbook.aspx"/>
    /// <see href="http://stackoverflow.com/questions/2961753/how-to-hide-files-generated-by-custom-tool-in-visual-studio"/>
    public class GenerateOntologyTask : ITask
    {
        #region ITask Members

        public IBuildEngine BuildEngine { get; set; }

        [Required]
        public string IntermediatePath { get; set; }

        [Required]
        public string ProjectPath { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        public ITaskHost HostObject { get; set; }

        public bool Execute()
        {
            TaskLogger logger = new TaskLogger(BuildEngine);

            try
            {
                FileInfo projectFile = new FileInfo(ProjectPath);
                FileInfo configFile = null;

#if NET35
                IEnumerable<string> allFiles = Directory.GetFiles(projectFile.DirectoryName);
#else
                IEnumerable<string> allFiles = Directory.EnumerateFiles(projectFile.DirectoryName);
 #endif

                foreach (string file in allFiles)
                {
                    string filename = file.ToLowerInvariant();

                    if (filename.EndsWith("app.config") || filename.EndsWith("web.config")) 
                    {
                        string contents = File.ReadAllText(file);
                        if (contents.Contains("TrinitySettings namespace=\"Semiodesk.Trinity.Test\")"))
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

                if (configFile != null)
                {
                    Program program = new Program(logger);
                    program.LoadConfigFile();

                    // TODO: Make ontologies folder configurable in Trinity settings.
                    if (string.IsNullOrEmpty(IntermediatePath))
                    {
                        IntermediatePath = projectFile.Directory.FullName;
                    }
                    else
                    {
                        IntermediatePath = Path.Combine(projectFile.DirectoryName, IntermediatePath);
                    }

                    string targetFile = Path.Combine(IntermediatePath, "Ontologies.g.cs");
                    program.SetTarget(targetFile);

                    int status = program.Run();

                    OutputFiles = new TaskItem[] { new TaskItem(targetFile) };

                    return status == 0;
                }
                else
                {
                    logger.LogMessage("No app.config or web.config file found in project directory: {0}.", projectFile.DirectoryName);

                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);

                return false;
            }
        }

        #endregion
    }
}
