using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jaahas.Json.Tests {

    [TestClass]
    public class ConfigurationBinderTests {

        [TestMethod]
        public void ShouldBindValidJsonPointer() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["TimeSeriesExtractor:StartAt"] = "/foo/bar"
                });

            var config = builder.Build();

            var options = new TimeSeriesExtractorOptions();
            config.Bind("TimeSeriesExtractor", options);

            Assert.IsNotNull(options.StartAt);
            Assert.AreEqual("/foo/bar", options.StartAt.ToString());
        }


        [TestMethod]
        public void ShouldNotBindInvalidJsonPointer() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["TimeSeriesExtractor:StartAt"] = "invalid"
                });

            var config = builder.Build();

            var options = new TimeSeriesExtractorOptions();
            Assert.ThrowsException<InvalidOperationException>(() => config.Bind("TimeSeriesExtractor", options));
        }


        [TestMethod]
        public void ShouldBindValidJsonPointerLiteralMatch() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["JsonPointerMatch:Match"] = "/foo/bar"
                });

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.IsNotNull(options.Match);
            Assert.AreEqual("/foo/bar", options.Match.ToString());
            Assert.IsFalse(options.Match.IsWildcardMatchRule);
        }


        [TestMethod]
        public void ShouldBindValidJsonPointerMqttMatch() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["JsonPointerMatch:Match"] = "/foo/bar/+/baz/#"
                });

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.IsNotNull(options.Match);
            Assert.AreEqual("/foo/bar/+/baz/#", options.Match.ToString());
            Assert.IsTrue(options.Match.IsWildcardMatchRule);
            Assert.IsFalse(options.Match.IsPatternWildcardMatchRule);
            Assert.IsTrue(options.Match.IsMqttWildcardMatchRule);
        }


        [TestMethod]
        public void ShouldBindValidJsonPointerPatternMatch() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["JsonPointerMatch:Match"] = "*/bar"
                });

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.IsNotNull(options.Match);
            Assert.AreEqual("*/bar", options.Match.ToString());
            Assert.IsTrue(options.Match.IsWildcardMatchRule);
            Assert.IsTrue(options.Match.IsPatternWildcardMatchRule);
            Assert.IsFalse(options.Match.IsMqttWildcardMatchRule);
        }


        private class JsonPointerMatchOptions {
            
            public JsonPointerMatch? Match { get; set; }

        }

    }

}
