﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Select;

namespace Test.Parser
{
    /// <summary>
    /// Test suite for attribute parser.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class ParserTest
    {
        public ParserTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void parsesSimpleDocument()
        {

            string html = "<html><head><title>First!</title></head><body><p>First post! <img src=\"foo.png\" /></p></body></html>";
            Document doc = NSoup.NSoupClient.Parse(html);
            // need a better way to verify these:
            Element p = doc.Body.Child(0);
            Assert.AreEqual("p", p.TagName);
            Element img = p.Child(0);
            Assert.AreEqual("foo.png", img.Attr("src"));
            Assert.AreEqual("img", img.TagName);
        }

        [TestMethod]
        public void parsesRoughAttributes()
        {
            string html = "<html><head><title>First!</title></head><body><p class=\"foo > bar\">First post! <img src=\"foo.png\" /></p></body></html>";
            Document doc = NSoup.NSoupClient.Parse(html);

            // need a better way to verify these:
            Element p = doc.Body.Child(0);
            Assert.AreEqual("p", p.TagName);
            Assert.AreEqual("foo > bar", p.Attr("class"));
        }

        [TestMethod]
        public void parsesComments()
        {
            string html = "<html><head></head><body><img src=foo><!-- <table><tr><td></table> --><p>Hello</p></body></html>";
            Document doc = NSoup.NSoupClient.Parse(html);

            Element body = doc.Body;
            Comment comment = (Comment)body.ChildNodes[1]; // comment should not be sub of img, as it's an empty tag
            Assert.AreEqual(" <table><tr><td></table> ", comment.GetData());
            Element p = body.Child(1);
            TextNode text = (TextNode)p.ChildNodes[0];
            Assert.AreEqual("Hello", text.GetWholeText());
        }

        [TestMethod]
        public void parsesUnterminatedComments()
        {
            string html = "<p>Hello<!-- <tr><td>";
            Document doc = NSoup.NSoupClient.Parse(html);
            Element p = doc.GetElementsByTag("p")[0];
            Assert.AreEqual("Hello", p.Text());
            TextNode text = (TextNode)p.ChildNodes[0];
            Assert.AreEqual("Hello", text.GetWholeText());
            Comment comment = (Comment)p.ChildNodes[1];
            Assert.AreEqual(" <tr><td>", comment.GetData());
        }

        [TestMethod]
        public void parsesUnterminatedTag()
        {
            string h1 = "<p";
            Document doc = NSoup.NSoupClient.Parse(h1);
            Assert.AreEqual(1, doc.GetElementsByTag("p").Count);

            string h2 = "<div id=1<p id='2'";
            doc = NSoup.NSoupClient.Parse(h2);
            Element d = doc.GetElementById("1");
            Assert.AreEqual(1, d.Children.Count);
            Element p = doc.GetElementById("2");
            Assert.IsNotNull(p);
        }

        [TestMethod]
        public void parsesUnterminatedAttribute()
        {
            string h1 = "<p id=\"foo";
            Document doc = NSoup.NSoupClient.Parse(h1);
            Element p = doc.GetElementById("foo");
            Assert.IsNotNull(p);
            Assert.AreEqual("p", p.TagName);
        }

        [TestMethod]
        public void parsesUnterminatedTextarea()
        {
            Document doc = NSoup.NSoupClient.Parse("<body><p><textarea>one<p>two");
            Element t = doc.Select("textarea").First;
            Assert.AreEqual("one<p>two", t.Text());
        }

        [TestMethod]
        public void parsesUnterminatedOption()
        {
            Document doc = NSoup.NSoupClient.Parse("<body><p><select><option>One<option>Two</p><p>Three</p>");
            Elements options = doc.Select("option");
            Assert.AreEqual(2, options.Count);
            Assert.AreEqual("One", options.First.Text());
            Assert.AreEqual("Two", options.Last.Text());
        }

        [TestMethod]
        public void testSpaceAfterTag()
        {
            Document doc = NSoup.NSoupClient.Parse("<div > <a name=\"top\"></a ><p id=1 >Hello</p></div>");
            Assert.AreEqual("<div> <a name=\"top\"></a><p id=\"1\">Hello</p></div>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void createsDocumentStructure()
        {
            string html = "<meta name=keywords /><link rel=stylesheet /><title>jsoup</title><p>Hello world</p>";
            Document doc = NSoup.NSoupClient.Parse(html);
            Element head = doc.Head;
            Element body = doc.Body;

            Assert.AreEqual(1, doc.Children.Count); // root node: contains html node
            Assert.AreEqual(2, doc.Child(0).Children.Count); // html node: head and body
            Assert.AreEqual(3, head.Children.Count);
            Assert.AreEqual(1, body.Children.Count);

            Assert.AreEqual("keywords", head.GetElementsByTag("meta")[0].Attr("name"));
            Assert.AreEqual(0, body.GetElementsByTag("meta").Count);
            Assert.AreEqual("jsoup", doc.Title);
            Assert.AreEqual("Hello world", body.Text());
            Assert.AreEqual("Hello world", body.Children[0].Text());
        }

        [TestMethod]
        public void createsStructureFromBodySnippet()
        {
            // the bar baz stuff naturally goes into the body, but the 'foo' goes into root, and the normalisation routine
            // needs to move into the start of the body
            string html = "foo <b>bar</b> baz";
            Document doc = NSoup.NSoupClient.Parse(html);
            Assert.AreEqual("foo bar baz", doc.Text());

        }

        [TestMethod]
        public void handlesEscapedData()
        {
            string html = "<div title='Surf &amp; Turf'>Reef &amp; Beef</div>";
            Document doc = NSoup.NSoupClient.Parse(html);
            Element div = doc.GetElementsByTag("div")[0];

            Assert.AreEqual("Surf & Turf", div.Attr("title"));
            Assert.AreEqual("Reef & Beef", div.Text());
        }

        [TestMethod]
        public void handlesDataOnlyTags()
        {
            string t = "<style>font-family: bold</style>";
            List<Element> tels = NSoup.NSoupClient.Parse(t).GetElementsByTag("style").ToList();
            Assert.AreEqual("font-family: bold", tels[0].Data);
            Assert.AreEqual("", tels[0].Text());

            string s = "<p>Hello</p><script>Nope</script><p>There</p>";
            Document doc = NSoup.NSoupClient.Parse(s);
            Assert.AreEqual("Hello There", doc.Text());
            Assert.AreEqual("Nope", doc.Data);
        }

        [TestMethod]
        public void handlesTextAfterData()
        {
            string h = "<html><body>pre <script>inner</script> aft</body></html>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("<html><head></head><body>pre <script>inner</script> aft</body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void handlesTextArea()
        {
            Document doc = NSoup.NSoupClient.Parse("<textarea>Hello</textarea>");
            Elements els = doc.Select("textarea");
            Assert.AreEqual("Hello", els.Text);
            Assert.AreEqual("Hello", els.Val());
        }

        [TestMethod]
        public void createsImplicitLists()
        {
            string h = "<li>Point one<li>Point two";
            Document doc = NSoup.NSoupClient.Parse(h);
            Elements ol = doc.Select("ul"); // should have created a default ul.
            Assert.AreEqual(1, ol.Count);
            Assert.AreEqual(2, ol[0].Children.Count);

            // no fiddling with non-implicit lists
            string h2 = "<ol><li><p>Point the first<li><p>Point the second";
            Document doc2 = NSoup.NSoupClient.Parse(h2);

            Assert.AreEqual(0, doc2.Select("ul").Count);
            Assert.AreEqual(1, doc2.Select("ol").Count);
            Assert.AreEqual(2, doc2.Select("ol li").Count);
            Assert.AreEqual(2, doc2.Select("ol li p").Count);
            Assert.AreEqual(1, doc2.Select("ol li")[0].Children.Count); // one p in first li
        }

        [TestMethod]
        public void createsImplicitTable()
        {
            string h = "<td>Hello<td><p>There<p>now";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("<table><tbody><tr><td>Hello</td><td><p>There</p><p>now</p></td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
            // <tbody> is introduced if no implicitly creating table, but allows tr to be directly under table
        }

        [TestMethod]
        public void handlesNestedImplicitTable()
        {
            Document doc = NSoup.NSoupClient.Parse("<table><td>1</td></tr> <td>2</td></tr> <td> <table><td>3</td> <td>4</td></table> <tr><td>5</table>");
            Assert.AreEqual("<table><tr><td>1</td></tr> <tr><td>2</td></tr> <tr><td> <table><tr><td>3</td> <td>4</td></tr></table> </td></tr><tr><td>5</td></tr></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesWhatWgExpensesTableExample()
        {
            // http://www.whatwg.org/specs/web-apps/current-work/multipage/tabular-data.html#examples-0
            Document doc = NSoup.NSoupClient.Parse("<table> <colgroup> <col> <colgroup> <col> <col> <col> <thead> <tr> <th> <th>2008 <th>2007 <th>2006 <tbody> <tr> <th scope=rowgroup> Research and development <td> $ 1,109 <td> $ 782 <td> $ 712 <tr> <th scope=row> Percentage of net sales <td> 3.4% <td> 3.3% <td> 3.7% <tbody> <tr> <th scope=rowgroup> Selling, general, and administrative <td> $ 3,761 <td> $ 2,963 <td> $ 2,433 <tr> <th scope=row> Percentage of net sales <td> 11.6% <td> 12.3% <td> 12.6% </table>");
            Assert.AreEqual("<table> <colgroup> <col /> </colgroup><colgroup> <col /> <col /> <col /> </colgroup><thead> <tr> <th> </th><th>2008 </th><th>2007 </th><th>2006 </th></tr></thead><tbody> <tr> <th scope=\"rowgroup\">Research and development </th><td>$ 1,109 </td><td>$ 782 </td><td>$ 712 </td></tr><tr> <th scope=\"row\">Percentage of net sales </th><td>3.4% </td><td>3.3% </td><td>3.7% </td></tr></tbody><tbody> <tr> <th scope=\"rowgroup\">Selling, general, and administrative </th><td>$ 3,761 </td><td>$ 2,963 </td><td>$ 2,433 </td></tr><tr> <th scope=\"row\">Percentage of net sales </th><td>11.6% </td><td>12.3% </td><td>12.6% </td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesTbodyTable()
        {
            Document doc = NSoup.NSoupClient.Parse("<html><head></head><body><table><tbody><tr><td>aaa</td><td>bbb</td></tr></tbody></table></body></html>");
            Assert.AreEqual("<table><tbody><tr><td>aaa</td><td>bbb</td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesImplicitCaptionClose()
        {
            Document doc = NSoup.NSoupClient.Parse("<table><caption>A caption<td>One<td>Two");
            Assert.AreEqual("<table><caption>A caption</caption><tr><td>One</td><td>Two</td></tr></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesBaseTags()
        {
            string h = "<a href=1>#</a><base href='/2/'><a href='3'>#</a><base href='http://bar'><a href=4>#</a>";
            Document doc = NSoup.NSoupClient.Parse(h, "http://foo/");
            //Assert.AreEqual("http://bar", doc.BaseUri); // gets updated as base changes, so doc.createElement has latest.
            Assert.AreEqual("http://bar/", doc.BaseUri); // Slight limitation in .NET, System.Uri class adds slash after string.
            

            Elements anchors = doc.GetElementsByTag("a");
            Assert.AreEqual(3, anchors.Count);

            Assert.AreEqual("http://foo/", anchors[0].BaseUri);
            Assert.AreEqual("http://foo/2/", anchors[1].BaseUri);
            //Assert.AreEqual("http://bar", anchors[2].BaseUri);
            Assert.AreEqual("http://bar/", anchors[2].BaseUri); // Again, same limitation as above.

            Assert.AreEqual("http://foo/1", anchors[0].AbsUrl("href"));
            Assert.AreEqual("http://foo/2/3", anchors[1].AbsUrl("href"));
            Assert.AreEqual("http://bar/4", anchors[2].AbsUrl("href"));
        }

        [TestMethod]
        public void handlesCdata()
        {
            string h = "<div id=1><![CData[<html>\n<foo><&amp;]]></div>"; // "cdata" insensitive. the &amp; in there should remain literal
            Document doc = NSoup.NSoupClient.Parse(h);
            Element div = doc.GetElementById("1");
            Assert.AreEqual("<html> <foo><&amp;", div.Text());
            Assert.AreEqual(0, div.Children.Count);
            Assert.AreEqual(1, div.ChildNodes.Count); // no elements, one text node
        }

        [TestMethod]
        public void handlesInvalidStartTags()
        {
            string h = "<div>Hello < There <&amp;></div>"; // parse to <div {#text=Hello < There <&>}>
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("Hello < There <&>", doc.Select("div").First.Text());
        }

        [TestMethod]
        public void handlesUnknownTags()
        {
            string h = "<div><foo title=bar>Hello<foo title=qux>there</foo></div>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Elements foos = doc.Select("foo");
            Assert.AreEqual(2, foos.Count);
            Assert.AreEqual("bar", foos.First.Attr("title"));
            Assert.AreEqual("qux", foos.Last.Attr("title"));
            Assert.AreEqual("there", foos.Last.Text());
        }

        [TestMethod]
        public void handlesUnknownInlineTags()
        {
            string h = "<p><cust>Test</cust></p><p><cust><cust>Test</cust></cust></p>";
            Document doc = NSoup.NSoupClient.ParseBodyFragment(h);
            string output = doc.Body.Html();
            Assert.AreEqual(h, TextUtil.StripNewLines(output));
        }

        [TestMethod]
        public void handlesUnknownNamespaceTags()
        {
            string h = "<foo:bar id=1/><abc:def id=2>Foo<p>Hello</abc:def><foo:bar>There</foo:bar>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("<foo:bar id=\"1\" /><abc:def id=\"2\">Foo<p>Hello</p></abc:def><foo:bar>There</foo:bar>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesEmptyBlocks()
        {
            string h = "<div id=1/><div id=2><img /></div>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Element div1 = doc.GetElementById("1");
            Assert.IsTrue(div1.Children.IsEmpty);
        }

        [TestMethod]
        public void handlesMultiClosingBody()
        {
            string h = "<body><p>Hello</body><p>there</p></body></body></html><p>now";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual(3, doc.Select("p").Count);
            Assert.AreEqual(3, doc.Body.Children.Count);
        }

        [TestMethod]
        public void handlesUnclosedDefinitionLists()
        {
            string h = "<dt>Foo<dd>Bar<dt>Qux<dd>Zug";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual(4, doc.Body.GetElementsByTag("dl").First.Children.Count);
            Elements dts = doc.Select("dt");
            Assert.AreEqual(2, dts.Count);
            Assert.AreEqual("Zug", dts[1].NextElementSibling.Text());
        }

        [TestMethod]
        public void handlesBlocksInDefinitions()
        {
            // per the spec, dt and dd are inline, but in practise are block
            string h = "<dl><dt><div id=1>Term</div></dt><dd><div id=2>Def</div></dd></dl>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("dt", doc.Select("#1").First.Parent.TagName);
            Assert.AreEqual("dd", doc.Select("#2").First.Parent.TagName);
            Assert.AreEqual("<dl><dt><div id=\"1\">Term</div></dt><dd><div id=\"2\">Def</div></dd></dl>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesFrames()
        {
            string h = "<html><head><script></script><noscript></noscript></head><frameset><frame src=foo></frame><frame src=foo></frameset></html>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("<html><head><script></script><noscript></noscript></head><frameset><frame src=\"foo\" /><frame src=\"foo\" /></frameset><body></body></html>",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void handlesJavadocFont()
        {
            string h = "<TD BGCOLOR=\"#EEEEFF\" CLASS=\"NavBarCell1\">    <A HREF=\"deprecated-list.html\"><FONT CLASS=\"NavBarFont1\"><B>Deprecated</B></FONT></A>&nbsp;</TD>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Element a = doc.Select("a").First;
            Assert.AreEqual("Deprecated", a.Text());
            Assert.AreEqual("font", a.Child(0).TagName);
            Assert.AreEqual("b", a.Child(0).Child(0).TagName);
        }

        [TestMethod]
        public void handlesBaseWithoutHref()
        {
            string h = "<head><base target='_blank'></head><body><a href=/foo>Test</a></body>";
            Document doc = NSoup.NSoupClient.Parse(h, "http://example.com/");
            Element a = doc.Select("a").First;
            Assert.AreEqual("/foo", a.Attr("href"));
            Assert.AreEqual("http://example.com/foo", a.Attr("abs:href"));
        }

        [TestMethod]
        public void normalisesDocument()
        {
            string h = "<!doctype html>One<html>Two<head>Three<link></head>Four<body>Five </body>Six </html>Seven ";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("<!doctype html><html><head><link /></head><body>One Two Four Three Five Six Seven </body></html>",
                    TextUtil.StripNewLines(doc.Html())); // is spaced OK if not newline & space stripped
        }

        [TestMethod]
        public void normalisesEmptyDocument()
        {
            Document doc = NSoup.NSoupClient.Parse("");
            Assert.AreEqual("<html><head></head><body></body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void normalisesHeadlessBody()
        {
            Document doc = NSoup.NSoupClient.Parse("<html><body><span class=\"foo\">bar</span>");
            Assert.AreEqual("<html><head></head><body><span class=\"foo\">bar</span></body></html>",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void findsCharsetInMalformedMeta()
        {
            string h = "<meta http-equiv=Content-Type content=text/html; charset=gb2312>";
            // example cited for reason of html5's <meta charset> element
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("gb2312", doc.Select("meta").Attr("charset"));
        }

        [TestMethod]
        public void testHgroup()
        {
            Document doc = NSoup.NSoupClient.Parse("<h1>Hello <h2>There <hgroup><h1>Another<h2>headline</hgroup> <hgroup><h1>More</h1><p>stuff</p></hgroup>");
            Assert.AreEqual("<h1>Hello </h1><h2>There </h2><hgroup><h1>Another</h1><h2>headline</h2></hgroup> <hgroup><h1>More</h1></hgroup><p>stuff</p>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testRelaxedTags()
        {
            Document doc = NSoup.NSoupClient.Parse("<abc_def id=1>Hello</abc_def> <abc-def>There</abc-def>");
            Assert.AreEqual("<abc_def id=\"1\">Hello</abc_def> <abc-def>There</abc-def>", TextUtil.StripNewLines(doc.Body.Html()));
        }
    }
}
