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
        public void ShouldExtractSamplesFromJsonForAllNonTimestampFields() {
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                TimestampProperty = "/" + nameof(deviceSample.Timestamp)
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
        }


        [TestMethod]
        public void ShouldUseDefaultKeyTemplate() {
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() { 
                Template = null!,
                TimestampProperty = "/" + nameof(deviceSample.Timestamp)
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{$prop}",
                TimestampProperty = "/" + nameof(deviceSample.Timestamp),
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress)));
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
            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{Uuid}/{$prop}",
                TimestampProperty = "/" + nameof(deviceSample.Timestamp),
                GetTemplateReplacement = text => { 
                    switch (text.ToUpperInvariant()) {
                        case "UUID":
                            return guid.ToString();
                        default:
                            return null;
                    }
                }
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress + "/" + guid)));
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{DataFormat}/{$prop}",
                TimestampProperty = "/" + nameof(deviceSample.Timestamp),
                IncludeProperty = prop => { 
                    if (prop.Equals("/" + nameof(deviceSample.DataFormat))) {
                        return false;
                    }
                    if (prop.Equals("/" + nameof(deviceSample.MacAddress))) {
                        return false;
                    }
                    return true;
                }
            }).ToArray();

            Assert.AreEqual(11, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress + "/" + deviceSample.DataFormat)));
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{MacAddress}/{DataFormat}/{$prop}",
                TimestampProperty = "/" + nameof(deviceSample.Timestamp),
                IncludeProperty = prop => {
                    if (prop.Equals("/" + nameof(deviceSample.Temperature))) {
                        return true;
                    }
                    if (prop.Equals("/" + nameof(deviceSample.Humidity))) {
                        return true;
                    }
                    if (prop.Equals("/" + nameof(deviceSample.Pressure))) {
                        return true;
                    }
                    return false;
                }
            }).ToArray();

            Assert.AreEqual(3, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.Key.StartsWith(TestContext.TestName + "/" + deviceSample.MacAddress + "/" + deviceSample.DataFormat)));
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/sample/{$prop}"
            }).ToArray();

            Assert.AreEqual(deviceSamples.Length, samples.Length);
            Assert.IsTrue(samples.All(x => string.Equals(x.Key, TestContext.TestName + "/sample/Value")));

            for (var i = 0; i < deviceSamples.Length; i++) {
                Assert.AreEqual(deviceSamples[i].Value, (double) samples[i].Value!, $"Samples at index {i} are different.");
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{$prop}",
                TimestampProperty = "/" + nameof(deviceSample.Timestamp),
                Recursive = true
            }).ToArray();

            Assert.AreEqual(16, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = "{location}/{$prop}",
                PathSeparator = "/",
                Recursive = true,
                IncludeProperty = prop => !prop.EndsWith("/location")
            }).ToArray();

            Assert.AreEqual(1, samples.Length);
            Assert.AreEqual("System A/Subsystem 1/measurements/temperature", samples[0].Key);
        }


        [TestMethod]
        public void ShouldApplyRecursiveTemplateReplacementsWithLocalPropertyName() {
            var testObject = new {
                location = "System A",
                measurements = new {
                    location = "Subsystem 1",
                    temperature = 28.2
                }
            };

            var json = JsonSerializer.Serialize(testObject);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = "{location}/{$prop-local}",
                PathSeparator = "/",
                Recursive = true,
                IncludeProperty = prop => !prop.EndsWith("/location")
            }).ToArray();

            Assert.AreEqual(1, samples.Length);
            Assert.AreEqual("System A/Subsystem 1/temperature", samples[0].Key);
        }


        [TestMethod]
        public void ShouldObeyRecursionDepthLimit() {
            var testObject = new {
                location = "System A",
                measurements = new {
                    location = "Subsystem 1",
                    temperature = 14
                }
            };

            var json = JsonSerializer.Serialize(testObject);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                MaxDepth = 1
            }).ToArray();

            Assert.AreEqual(2, samples.Length);

            Assert.AreEqual("location", samples[0].Key);
            Assert.AreEqual(testObject.location, samples[0].Value);
            
            Assert.AreEqual("measurements", samples[1].Key);
            Assert.AreEqual(@"{""location"":""Subsystem 1"",""temperature"":14}", samples[1].Value);
        }


        [TestMethod]
        public void ShouldUseFallbackTimestamp() {
            var testObject = new { 
                value = 99
            };

            var json = JsonSerializer.Serialize(testObject);

            var fallbackTimestamp = DateTimeOffset.Parse("1999-12-31");

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{$prop}",
                GetDefaultTimestamp = () => fallbackTimestamp
            }).ToArray();

            Assert.AreEqual(1, samples.Length);
            Assert.AreEqual(fallbackTimestamp, samples[0].Timestamp);
        }


        [TestMethod]
        public void ShouldAllowUnresolvedTemplateReplacements() {
            var testObject = new {
                value = 99
            };

            string json = JsonSerializer.Serialize(testObject);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() { 
                Template = TestContext.TestName + "/{deviceId}/{$prop}",
                AllowUnresolvedTemplateReplacements = true
            }).ToArray();

            Assert.AreEqual(1, samples.Length);
            Assert.AreEqual(TestContext.TestName + "/{deviceId}/value", samples[0].Key);
        }


        [TestMethod]
        public void ShouldNotAllowUnresolvedTemplateReplacements() {
            var testObject = new {
                value = 99
            };

            string json = JsonSerializer.Serialize(testObject);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.TestName + "/{deviceId}/{$prop}",
                AllowUnresolvedTemplateReplacements = false
            }).ToArray();

            Assert.AreEqual(0, samples.Length);
        }


        [TestMethod]
        public void ShouldAllowNumericalTimestamp() {
            long ms = 1646312969367;

            var deviceSample = new {
                Timestamp = ms,
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

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                TimestampProperty = "/" + nameof(deviceSample.Timestamp)
            }).ToArray();

            Assert.AreEqual(13, samples.Length);

            var expectedTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(ms);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(expectedTimestamp.UtcDateTime)));
        }

    }
}
