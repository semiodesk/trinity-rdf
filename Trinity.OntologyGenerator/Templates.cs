namespace Semiodesk.Trinity.OntologyGenerator
{
    public class Templates
    {
        public static string FileTemplate =
@"// Attention: This file is generated. Any modifications will eventually be overwritten.

using System;
using System.Collections.Generic;
using System.Text;
using Semiodesk.Trinity;

namespace {1}
{{
    {0}
}}";

        public static string OntologyTemplate = 
@"
///<summary>
///{3}
///{4}
///</summary>
public class {0} : Ontology
{{
    public static readonly Uri Namespace = new Uri(""{1}"");
    public static Uri GetNamespace() {{ return Namespace; }}
    
    public static readonly string Prefix = ""{0}"";
    public static string GetPrefix() {{ return Prefix; }} {2}
}}";

        public static string ResourceTemplate =
            @"

    ///<summary>
    ///{3}
    ///</summary>
    public static readonly {0} {1} = new {0}(new Uri(""{2}""));
";

        public static string StringOntologyTemplate = 
@"
///<summary>
///{3}
///{4}
///</summary>
public static class {0}
{{
    public static readonly Uri Namespace = new Uri(""{1}"");
    public static Uri GetNamespace() {{ return Namespace; }}
    
    public static readonly string Prefix = ""{0}"";
    public static string GetPrefix() {{ return Prefix; }} {2}
}}
";

        public static string StringTemplate = @"

    ///<summary>
    ///{3}
    ///</summary>
    public const string {1} = ""{2}"";";
    }
}