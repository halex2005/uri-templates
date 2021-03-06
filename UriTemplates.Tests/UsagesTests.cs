﻿using System;

namespace Resta.UriTemplates.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    public class UsagesTests
    {
        [Test]
        public void PathSegmentTest()
        {
            var template = new UriTemplate("http://example.com/{path}");

            var actual = template.Resolve(new Dictionary<string, object>
            {
                { "path", "foo" }
            });

            Assert.AreEqual("http://example.com/foo", actual);
        }

        [Test]
        public void GuidValueIsNotSupported_UseStringInstead()
        {
            var template = new UriTemplate("http://example.com/{guid}/{int}");

            var ex = Assert.Throws<UriTemplateException>(() => template.Resolve(new Dictionary<string, object> {{ "guid", Guid.NewGuid() }}));

            Assert.That(ex.Message, Does.Contain("Invalid value type of variable \"System.Guid\". Expected: string or IEnumerable<string> or IDictionary<string, string>."));
        }

        [Test]
        public void IntValueIsNotSupported_UseStringInstead()
        {
            var template = new UriTemplate("http://example.com/{guid}/{int}");

            var ex = Assert.Throws<UriTemplateException>(() => template.Resolve(new Dictionary<string, object> {{ "int", 42 }}));

            Assert.That(ex.Message, Does.Contain("Invalid value type of variable \"System.Int32\". Expected: string or IEnumerable<string> or IDictionary<string, string>."));
        }

        [Test]
        public void MultiplePathSegmentTest()
        {
            var template = new UriTemplate("http://example.com/{path1}/{path2}");

            var actual = template.Resolve(new Dictionary<string, object>
            {
                { "path1", "foo" },
                { "path2", "bar" }
            });

            Assert.AreEqual("http://example.com/foo/bar", actual);
        }

        [Test]
        public void QueryParamsWithoutValuesTest()
        {
            var template = new UriTemplate("http://example.com/foo{?q1,q2}");

            var actual = template.Resolve(new Dictionary<string, object>());

            Assert.AreEqual("http://example.com/foo", actual);
        }

        [Test]
        public void QueryParamsWithOneValueTest()
        {
            var template = new UriTemplate("http://example.com/foo{?q1,q2}");

            var actual = template.Resolve(new Dictionary<string, object>()
            {
                { "q1", "abc" }
            });

            Assert.AreEqual("http://example.com/foo?q1=abc", actual);
        }

        [Test]
        public void QueryParamsTest()
        {
            var template = new UriTemplate("http://example.com/foo{?q1,q2}");

            var actual = template.Resolve(new Dictionary<string, object>
            {
                { "q1", "abc" },
                { "q2", "10" }
            });

            Assert.AreEqual("http://example.com/foo?q1=abc&q2=10", actual);
        }

        [Test]
        public void QueryParamsWithMultipleValuesTest()
        {
            var template = new UriTemplate("http://example.com/foo{?q1,q2}");

            var actual = template.Resolve(new Dictionary<string, object>
            {
                { "q1", new string[] { "abc", "def", "ghi" } },
                { "q2", "10" }
            });

            Assert.AreEqual("http://example.com/foo?q1=abc,def,ghi&q2=10", actual);
        }

        [Test]
        public void QueryParamsExpandedTest()
        {
            var template = new UriTemplate("http://example.com/foo{?q*}");

            var actual = template.Resolve(new Dictionary<string, object>
            {
                { "q", new Dictionary<string, string> { { "q1", "abc" }, { "q2", "def" } } }
            });

            Assert.AreEqual("http://example.com/foo?q1=abc&q2=def", actual);
        }

        [Test]
        public void ExampleTest()
        {
            var template = new UriTemplate("http://example.org/{area}/last-news{?type,count}");

            var uri = template.Resolve(new Dictionary<string, object>
            {
                { "area", "world" },
                { "type", "actual" },
                { "count", "10" }
            });

            Assert.AreEqual("http://example.org/world/last-news?type=actual&count=10", uri);
        }

        [Test]
        public void AnotherExampleTest()
        {
            var template = new UriTemplate("http://example.org/{area}/last-news{?type}");

            var uri = template.GetResolver()
                .Bind("area", "world")
                .Bind("type", new string[] { "it", "music", "art" })
                .Resolve();

            Assert.AreEqual("http://example.org/world/last-news?type=it,music,art", uri);
        }

        [Test]
        public void ComplexTest()
        {
            var template = new UriTemplate("http://example.com{/paths*}{?q1,q2}{#f*}");

            var actual = template.Resolve(new Dictionary<string, object>
            {
                { "paths", new string[] { "foo", "bar" } },
                { "q1", "abc" },
                { "f", new Dictionary<string, string> { { "key1", "val1" }, { "key2", null } } }
            });

            Assert.AreEqual("http://example.com/foo/bar?q1=abc#key1=val1,key2=", actual);
        }

        [Test]
        public void InvalidUriTemplateTest()
        {
            var template = "http://example.com/{path#!!}";

            try
            {
                new UriTemplate(template);
            }
            catch (UriTemplateParseException ex)
            {
                Assert.AreEqual(ex.Position, 24); // invalid variable name
                Assert.AreEqual(ex.Template, template);
                return;
            }

            Assert.Fail("Test must die");
        }

        [Test]
        public void ResolveUri()
        {
            var template = new UriTemplate("http://example.com{/paths*}{?q1,q2}{#f*}");

            var actual = template.ResolveUri(new Dictionary<string, object>
            {
                { "paths", new string[] { "foo", "bar" } },
                { "q1", "abc" },
                { "f", new Dictionary<string, string> { { "key1", "val1" }, { "key2", null } } }
            });

            Assert.AreEqual(new Uri("http://example.com/foo/bar?q1=abc#key1=val1,key2="), actual);
        }

        [Test]
        public void ResolveUriRelative()
        {
            var template = new UriTemplate("{/paths*}{?q1,q2}{#f*}");

            var actual = template.ResolveUri(UriKind.RelativeOrAbsolute, new Dictionary<string, object>
            {
                { "paths", new string[] { "foo", "bar" } },
                { "q1", "abc" },
                { "f", new Dictionary<string, string> { { "key1", "val1" }, { "key2", null } } }
            });

            Assert.AreEqual(new Uri("/foo/bar?q1=abc#key1=val1,key2=", UriKind.Relative), actual);
        }
    }
}