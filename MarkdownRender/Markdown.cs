using HtmlAgilityPack;
using MarkdownRender.Properties;
using System;
using System.IO;

namespace MarkdownRender
{
    public class Markdown
    {
        public static string ConvertToHtml(string md)
        {
            var html = CommonMark.CommonMarkConverter.Convert(md);
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
    }
}
