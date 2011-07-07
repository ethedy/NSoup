﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup;

namespace Test.Integration
{
    /// <summary>
    /// Tests the URL connection. Not enabled by default, so tests don't require network connection.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    [Ignore] // ignored by default so tests don't require network access. comment out to enable.
    public class UrlConnectTest
    {
        public UrlConnectTest()
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

        private static string echoURL = "http://infohound.net/tools/q.pl";

        [TestMethod]
        public void fetchURl()
        {
            string url = "http://www.google.com"; // no trailing / to force redir
            Document doc = NSoup.NSoupClient.Parse(new Uri(url), 10 * 1000);
            Assert.IsTrue(doc.Title.Contains("Google"));
        }

        [TestMethod]
        public void fetchBaidu()
        {
            IResponse res = NSoup.NSoupClient.Connect("http://www.baidu.com/").Timeout(10 * 1000).Execute();
            Document doc = res.Parse();

            Assert.AreEqual("GB2312", doc.Settings.Encoding.WebName.ToUpperInvariant());
            Assert.AreEqual("GB2312", res.Charset().ToUpperInvariant());
            Assert.IsTrue(res.HasCookie("BAIDUID"));
            Assert.AreEqual("text/html;charset=gb2312", res.ContentType());
        }

        [TestMethod]
        public void exceptOnUnknownContentType()
        {
            string url = "http://jsoup.org/rez/osi_logo.png"; // not text/* but image/png, should throw
            bool threw = false;
            try
            {
                Document doc = NSoupClient.Parse(new Uri(url), 3000);
            }
            catch (System.IO.IOException)
            {
                threw = true;
            }
            Assert.IsTrue(threw);
        }

        [TestMethod]
        public void ignoresContentTypeIfSoConfigured()
        {
            Document doc = NSoupClient.Connect("http://jsoup.org/rez/osi_logo.png").IgnoreContentType(true).Get();
            Assert.AreEqual("", doc.Title); // this will cause an ugly parse tree
        }

        [TestMethod]
        public void doesPost()
        {
            Document doc = NSoupClient.Connect(echoURL)
                .Data("uname", "Jsoup", "uname", "Jonathan", "ח™¾", "ו÷¦ה¸€ה¸‹")
                .Cookie("auth", "token")
                .Post();

            Assert.AreEqual("POST", ihVal("REQUEST_METHOD", doc));
            //Assert.AreEqual("gzip", ihVal("HTTP_ACCEPT_ENCODING", doc)); // current proxy removes gzip on post
            Assert.AreEqual("auth=token", ihVal("HTTP_COOKIE", doc));
            Assert.AreEqual("ו÷¦ה¸€ה¸‹", ihVal("ח™¾", doc));
            Assert.AreEqual("Jsoup, Jonathan", ihVal("uname", doc));
        }

        [TestMethod]
        public void doesGet()
        {
            IConnection con = NSoupClient.Connect(echoURL + "?what=the")
                .UserAgent("Mozilla")
                .Referrer("http://example.com")
                .Data("what", "about & me?");

            Document doc = con.Get();
            //Assert.AreEqual("what=the&what=about+%26+me%3F", ihVal("QUERY_STRING", doc));
            Assert.AreEqual("what=the&what=about+%26+me%3f", ihVal("QUERY_STRING", doc)); // Again, a change due to specific behavior, by HttpUtility.UrlEncode(). Difference is acceptable.
            Assert.AreEqual("the, about & me?", ihVal("what", doc));
            Assert.AreEqual("Mozilla", ihVal("HTTP_USER_AGENT", doc));
            Assert.AreEqual("http://example.com", ihVal("HTTP_REFERER", doc));
        }

        private static string ihVal(string key, Document doc)
        {
            return doc.Select("th:contains(" + key + ") + td").First.Text();
        }

        [TestMethod]
        public void followsTempRedirect()
        {
            IConnection con = NSoupClient.Connect("http://infohound.net/tools/302.pl"); // http://jsoup.org
            Document doc = con.Get();
            Assert.IsTrue(doc.Title.Contains("jsoup"));
        }

        [TestMethod]
        public void followsRedirectToHttps()
        {
            IConnection con = NSoupClient.Connect("http://infohound.net/tools/302-secure.pl"); // https://www.google.com
            con.Data("id", "5");
            Document doc = con.Get();
            Assert.IsTrue(doc.Title.Contains("Google"));
        }

        [TestMethod]
        public void followsRelativeRedirect()
        {
            IConnection con = NSoupClient.Connect("http://infohound.net/tools/302-rel.pl"); // to ./ - /tools/
            Document doc = con.Post();
            Assert.IsTrue(doc.Title.Contains("HTML Tidy Online"));
        }

        [TestMethod]
        public void throwsExceptionOnError()
        {
            IConnection con = NSoupClient.Connect("http://infohound.net/tools/404");
            bool threw = false;
            try
            {
                Document doc = con.Get();
            }
            catch (System.IO.IOException)
            {
                threw = true;
            }
            Assert.IsTrue(threw);
        }

        [TestMethod]
        public void ignoresExceptionIfSoConfigured()
        {
            IConnection con = NSoupClient.Connect("http://infohound.net/tools/404").IgnoreHttpErrors(true);
            IResponse res = con.Execute();
            Document doc = res.Parse();
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, res.StatusCode());
            Assert.AreEqual("Not Found", doc.Select("h1").First.Text());
        }

        [TestMethod]
        public void doesntRedirectIfSoConfigured()
        {
            IConnection con = NSoupClient.Connect("http://infohound.net/tools/302.pl").FollowRedirects(false);
            IResponse res = con.Execute();
            Assert.IsTrue(res.StatusCode() == (System.Net.HttpStatusCode)302);
        }

        [TestMethod]
        public void redirectsResponseCookieToNextResponse()
        {
            IConnection con = NSoupClient.Connect("http://infohound.net/tools/302-cookie.pl");
            IResponse res = con.Execute();
            Assert.AreEqual("asdfg123", res.Cookie("token")); // confirms that cookies set on 1st hit are presented in final result
            Document doc = res.Parse();
            Assert.AreEqual("token=asdfg123", ihVal("HTTP_COOKIE", doc)); // confirms that redirected hit saw cookie
        }

        [TestMethod]
        public void maximumRedirects()
        {
            bool threw = false;
            try
            {
                Document doc = NSoupClient.Connect("http://infohound.net/tools/loop.pl").Get();
            }
            catch (System.IO.IOException e)
            {
                Assert.IsTrue(e.Message.Contains("Too many redirects"));
                threw = true;
            }
            Assert.IsTrue(threw);
        }
    }
}
