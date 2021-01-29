using System.IO;
using System.Reflection;

namespace MarkdownRender
{
    public class Utilities
    {
        public static string AssemblyFolder()
            => Path.GetDirectoryName(ExecutingAssembly());

        public static string ExecutingAssembly()
            => Assembly.GetExecutingAssembly().GetName().CodeBase.Substring(8).Replace('/', '\\');
    }
}