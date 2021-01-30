using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using MarkdownRender.Properties;

namespace MarkdownRender
{
    public class Markdown
    {
        /// <summary>
        /// Convert the Markdown to html with the Pandoc.
        /// </summary>
        /// <param name="md">The content of the Markdown.</param>
        /// <param name="pathPandoc">The path of the Pandoc.exe</param>
        /// <param name="args">The arguments for the Pandoc. If empty, the default args will be used.</param>
        /// <returns></returns>
        public static string ConvertToHtml(string md, string pathPandoc = null, string args = null)
        {
            var argsPandoc = string.IsNullOrEmpty(args)
                ? "-s --highlight-style haddock -f markdown_github-emoji+tex_math_dollars -t html5 --email-obfuscation=none --mathjax"
                : args;
            var html = Pandoc(md, pathPandoc, args);
            var template = GetTemplateHtml();
            var rendered = RenderWithTemplate(template, html);
            return rendered;
        }

        private static string GetTemplateHtml()
        {
            var manager = Resources.ResourceManager;
            var tempHtml = manager.GetObject("user_template");
            if (tempHtml is string)
                return tempHtml.ToString();
            else
                throw new FileNotFoundException("unable to find the template.");
        }

        private static string RenderWithTemplate(string template, string md)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(template);
            var div = doc.GetElementbyId("content");
            if (div == null)
                throw new NullReferenceException(
                    "unable to find the div element with the id 'content' in the template.");
            else
                div.InnerHtml = ScrubHtml(md);

            return doc.DocumentNode.WriteTo();
        }

        private static string GetIdName(int number) => $"mde-{number}";

        private static string ScrubHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            Action<HtmlNodeCollection, Action<HtmlNode>> each = (nodes, action) =>
            {
                if (nodes == null) return;
                foreach (var node in nodes) action.Invoke(node);
            };

            var idx = 1;
            Func<string> getName = () => GetIdName(idx++);

            // Inject anchors at all block level elements for scroll synchronization
            var nc = doc.DocumentNode.SelectNodes("//p|//h1|//h2|//h3|//h4|//h5|//h6|//ul|//ol|//li|//hr|//pre|//blockquote");
            each(nc, node => { if (node.Name != "blockquote" || node.ParentNode.Name != "li") node.Attributes.Add("id", getName()); });

            // Remove potentially harmful elements
            nc = doc.DocumentNode.SelectNodes("//script|//link|//iframe|//frameset|//frame|//applet|//object|//embed");
            each(nc, node => node.ParentNode.RemoveChild(node, false));

            // Remove hrefs to java/j/vbscript URLs
            nc = doc.DocumentNode.SelectNodes("//a[starts-with(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'javascript')]|//a[starts-with(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'jscript')]|//a[starts-with(translate(@href, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'vbscript')]");
            each(nc, node => node.SetAttributeValue("href", "#"));

            // Remove img with refs to java/j/vbscript URLs
            nc = doc.DocumentNode.SelectNodes("//img[starts-with(translate(@src, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'javascript')]|//img[starts-with(translate(@src, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'jscript')]|//img[starts-with(translate(@src, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'vbscript')]");
            each(nc, node => node.SetAttributeValue("src", "#"));

            // Remove on<Event> handlers from all tags
            nc = doc.DocumentNode.SelectNodes("//*[@onclick or @onmouseover or @onfocus or @onblur or @onmouseout or @ondoubleclick or @onload or @onunload]");
            each(nc, node =>
            {
                node.Attributes.Remove("onFocus");
                node.Attributes.Remove("onBlur");
                node.Attributes.Remove("onClick");
                node.Attributes.Remove("onMouseOver");
                node.Attributes.Remove("onMouseOut");
                node.Attributes.Remove("onDoubleClick");
                node.Attributes.Remove("onLoad");
                node.Attributes.Remove("onUnload");
            });

            // remove any style attributes that contain the word expression (IE evaluates this as script)
            nc = doc.DocumentNode.SelectNodes("//*[contains(translate(@style, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'expression')]");
            each(nc, node => node.Attributes.Remove("style"));

            return doc.DocumentNode.WriteTo();
        }

        private static string Pandoc(string text, string pathPandoc, string args)
        {
            var whereIsPandoc = string.IsNullOrEmpty(pathPandoc) ? "pandoc.exe" : pathPandoc;
            var pandoc = ProcessStartInfo("pandoc.exe", args, text != null);

            return ResultFromExecuteProcess(text, pandoc);
        }

        private static string ResultFromExecuteProcess(string text, ProcessStartInfo startInfo)
        {
            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    return text;
                }
                if (text != null)
                {
                    var utf8 = new StreamWriter(process.StandardInput.BaseStream, Encoding.UTF8);
                    utf8.Write(text);
                    utf8.Close();
                }
                var result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode == 0) return result;

                var msg = process.StandardError.ReadToEnd();
                result = string.IsNullOrEmpty(msg) ? "empty error response" : msg;
                return result;
            }
        }

        private static ProcessStartInfo ProcessStartInfo(string fileName, string arguments, bool redirectInput)
        {
            if (File.Exists(fileName) == false)
                throw new FileNotFoundException("unable to find pandoc.exe");

            return new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Utilities.AssemblyFolder()
            };
        }
    }
}
