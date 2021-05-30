using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jaahas.Json.Tests {

    [TestClass]
    public class JsonTimeSeriesExtractorTests {

        public TestContext TestContext { get; set; } = default!;


        [TestMethod]
        public void ShouldExtractTagValuesFromJsonForAllNonTimestampFields() {
            var deviceSample = new {
                Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                SignalStrength = -75,
                DataFormat = 5,
                Temperature = 19.3,
                Humidity = 37.905,
                Pressure = 1013.35,
                AccelerationX = -0.872,
                AccelerationY = 0.512,
                AccelerationZ = -0.04,
                BatteryVoltage = 3.085,
                TxPower = 4,
                MovementCounter = 5,
                MeasurementSequence = 34425,
                MacAddress = "AB:CD:EF:01:23:45"
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var tagValues = TimeSeriesExtractor.GetSamples(json).ToArray();

            Assert.AreEqual(13, tagValues.Length);
            Assert.IsTrue(tagValues.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
        }


        [TestMethod]
        public void ShouldProcessKeyTemplate() {
            var deviceSample = new {
                Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                SignalStrength = -75,
                DataFormat = 5,
                Temperature = 19.3,
                Humidity = 37.905,
                Pressure = 1013.35,
                AccelerationX = -0.872,
                AccelerationY = 0.512,
                AccelerationZ = -0.04,
                BatteryVoltage = 3.085,
                TxPower = 4,
                MovementCounter = 5,
                MeasurementSequence = 34425,
                MacAddress = "AB:CD:EF:01:23:45"
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var tagValues = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{$prop}"
            }).ToArray();

            Assert.AreEqual(13, tagValues.Length);
            Assert.IsTrue(tagValues.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(tagValues.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress)));
        }


        [TestMethod]
        public void ShouldProcessKeyTemplateWithDefaultReplacements() {
            var deviceSample = new {
                Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                SignalStrength = -75,
                DataFormat = 5,
                Temperature = 19.3,
                Humidity = 37.905,
                Pressure = 1013.35,
                AccelerationX = -0.872,
                AccelerationY = 0.512,
                AccelerationZ = -0.04,
                BatteryVoltage = 3.085,
                TxPower = 4,
                MovementCounter = 5,
                MeasurementSequence = 34425,
                MacAddress = "AB:CD:EF:01:23:45"
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var guid = Guid.NewGuid();
            var tagValues = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{Uuid}/{$prop}",
                TemplateReplacements = new Dictionary<string, string>() {
                    ["Uuid"] = guid.ToString()
                }
            }).ToArray();

            Assert.AreEqual(13, tagValues.Length);
            Assert.IsTrue(tagValues.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(tagValues.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress + "/" + guid)));
        }


        [TestMethod]
        public void ShouldExcludeSpecifiedProperties() {
            var deviceSample = new {
                Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                SignalStrength = -75,
                DataFormat = 5,
                Temperature = 19.3,
                Humidity = 37.905,
                Pressure = 1013.35,
                AccelerationX = -0.872,
                AccelerationY = 0.512,
                AccelerationZ = -0.04,
                BatteryVoltage = 3.085,
                TxPower = 4,
                MovementCounter = 5,
                MeasurementSequence = 34425,
                MacAddress = "AB:CD:EF:01:23:45"
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var tagValues = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{DataFormat}/{$prop}",
                IncludeProperty = prop => { 
                    if (prop.Equals(nameof(deviceSample.DataFormat))) {
                        return false;
                    }
                    if (prop.Equals(nameof(deviceSample.MacAddress))) {
                        return false;
                    }
                    return true;
                }
            }).ToArray();

            Assert.AreEqual(11, tagValues.Length);
            Assert.IsTrue(tagValues.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(tagValues.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress + "/" + deviceSample.DataFormat)));
        }


        [TestMethod]
        public void ShouldIncludeSpecifiedProperties() {
            var deviceSample = new {
                Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                SignalStrength = -75,
                DataFormat = 5,
                Temperature = 19.3,
                Humidity = 37.905,
                Pressure = 1013.35,
                AccelerationX = -0.872,
                AccelerationY = 0.512,
                AccelerationZ = -0.04,
                BatteryVoltage = 3.085,
                TxPower = 4,
                MovementCounter = 5,
                MeasurementSequence = 34425,
                MacAddress = "AB:CD:EF:01:23:45"
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var tagValues = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{DataFormat}/{$prop}",
                IncludeProperty = prop => {
                    if (prop.Equals(nameof(deviceSample.Temperature))) {
                        return true;
                    }
                    if (prop.Equals(nameof(deviceSample.Humidity))) {
                        return true;
                    }
                    if (prop.Equals(nameof(deviceSample.Pressure))) {
                        return true;
                    }
                    return false;
                }
            }).ToArray();

            Assert.AreEqual(3, tagValues.Length);
            Assert.IsTrue(tagValues.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(tagValues.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress + "/" + deviceSample.DataFormat)));
        }


        [TestMethod]
        public void ShouldParseTopLevelArray() {
            var deviceSamples = new[] {
                new { Value = 55.5 },
                new { Value = 417.1 },
                new { Value = -0.0032 },
                new { Value = 14.0 },
            };

            var json = JsonSerializer.Serialize(deviceSamples);

            var tagValues = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/sample/{$prop}"
            }).ToArray();

            Assert.AreEqual(deviceSamples.Length, tagValues.Length);
            Assert.IsTrue(tagValues.All(x => string.Equals(x.Key, TestContext.TestName + "/sample/Value")));

            for (var i = 0; i < deviceSamples.Length; i++) {
                Assert.AreEqual(deviceSamples[i].Value, (double) tagValues[i].Value!, $"Samples at index {i} are different.");
            }
        }


        [TestMethod]
        public void ShouldRecursivelyParseObject() {
            var deviceSample = new {
                Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                Metadata = new {
                    SignalStrength = -75,
                    DataFormat = 5,
                    MeasurementSequence = 34425,
                    MacAddress = "AB:CD:EF:01:23:45"
                },
                Environment = new[] {
                    new {
                        Temperature = 19.3,
                        Humidity = 37.905,
                        Pressure = 1013.35
                    },
                    new {
                        Temperature = 19.3,
                        Humidity = 37.905,
                        Pressure = 1013.35
                    }
                },
                Acceleration = new {
                    X = -0.872,
                    Y = 0.512,
                    Z = -0.04
                },
                Power = new {
                    BatteryVoltage = 3.085,
                    TxPower = 4
                },
                Other = new {
                    MovementCounter = 5
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var tagValues = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{$prop}",
                Recursive = true
            }).ToArray();

            Assert.AreEqual(16, tagValues.Length);
            Assert.IsTrue(tagValues.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
        }


        [TestMethod]
        public void ShouldApplyRecursiveTemplateReplacements() {
            var testObject = new { 
                location = "System A",
                measurements = new {
                    location = "Subsystem 1",
                    temperature = 28.2
                }
            };

            var json = JsonSerializer.Serialize(testObject);

            var tagValues = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = "{location}/{$prop}",
                PathSeparator = "/",
                Recursive = true,
                IncludeProperty = prop => !prop.Equals("location")
            }).ToArray();

            Assert.AreEqual(1, tagValues.Length);
            Assert.AreEqual("System A/Subsystem 1/measurements/temperature", tagValues[0].Key);
        }

    }
}
