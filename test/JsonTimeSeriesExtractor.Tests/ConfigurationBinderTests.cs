using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Jaahas.Json.Tests {

    public class ConfigurationBinderTests {

        [Fact]
        public void ShouldBindValidJsonPointer() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["TimeSeriesExtractor:StartAt"] = "/foo/bar"
                });

            var config = builder.Build();

            var options = new TimeSeriesExtractorOptions();
            config.Bind("TimeSeriesExtractor", options);

            Assert.NotNull(options.StartAt);
            Assert.Equal("/foo/bar", options.StartAt.ToString());
        }


        [Fact]
        public void ShouldNotBindInvalidJsonPointer() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["TimeSeriesExtractor:StartAt"] = "invalid"
                });

            var config = builder.Build();

            var options = new TimeSeriesExtractorOptions();
            Assert.Throws<InvalidOperationException>(() => config.Bind("TimeSeriesExtractor", options));
        }


        [Fact]
        public void ShouldNotBindNullValue() {
            var builder = new ConfigurationBuilder();

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.Null(options.Match);
        }


        [Fact]
        public void ShouldNotBindEmptyValue() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["JsonPointerMatch:Match"] = ""
                });

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.Null(options.Match);
        }


        [Fact]
        public void ShouldBindValidJsonPointerLiteralMatch() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["JsonPointerMatch:Match"] = "/foo/bar"
                });

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.NotNull(options.Match);
            Assert.Equal("/foo/bar", options.Match.ToString());
            Assert.False(options.Match.Value.IsWildcardMatchRule);
        }


        [Fact]
        public void ShouldBindValidJsonPointerMqttMatch() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["JsonPointerMatch:Match"] = "/foo/bar/+/baz/#"
                });

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.NotNull(options.Match);
            Assert.Equal("/foo/bar/+/baz/#", options.Match.ToString());
            Assert.True(options.Match.Value.IsWildcardMatchRule);
            Assert.False(options.Match.Value.IsPatternWildcardMatchRule);
            Assert.True(options.Match.Value.IsMqttWildcardMatchRule);
        }


        [Fact]
        public void ShouldBindValidJsonPointerPatternMatch() {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    ["JsonPointerMatch:Match"] = "*/bar"
                });

            var config = builder.Build();

            var options = new JsonPointerMatchOptions();
            config.Bind("JsonPointerMatch", options);

            Assert.NotNull(options.Match);
            Assert.Equal("*/bar", options.Match.ToString());
            Assert.True(options.Match.Value.IsWildcardMatchRule);
            Assert.True(options.Match.Value.IsPatternWildcardMatchRule);
            Assert.False(options.Match.Value.IsMqttWildcardMatchRule);
        }


        private class JsonPointerMatchOptions {
            
            public JsonPointerMatch? Match { get; set; }

        }

    }

}
