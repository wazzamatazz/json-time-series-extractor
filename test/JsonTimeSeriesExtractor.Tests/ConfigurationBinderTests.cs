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

    }

}
