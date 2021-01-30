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
            //// load the markdown file.
            //var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //var fileName = Path.Combine(path, "readme.md");
            //var md = File.ReadAllText(fileName);

            // customized md
            //var md = "<p style=\"color: red\"><b>无法找到Readme文件</b></p>" +
            //         $"<p>{Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)?.Replace("\\", "&#92;")}</p>";

            // image path fixing
            var md = "![mainFrame](/Resources/mainFrame.png)";

            // convert the markdown to the html.
            var html = Markdown.ConvertToHtml(md);
        }
    }
}