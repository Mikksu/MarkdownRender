using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;

namespace MarkdownRender.Tests
{
    [TestClass()]
    public class MarkdownTests
    {
        [TestMethod()]
        public void ConvertToHtmlTest()
        {
            // load the markdown file.
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fileName = Path.Combine(path, "readme.md");
            var md = File.ReadAllText(fileName);

            // convert the markdown to the html.
            var html = Markdown.ConvertToHtml(md);
        }
    }
}