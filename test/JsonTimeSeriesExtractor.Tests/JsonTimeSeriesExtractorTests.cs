using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Json.Pointer;

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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp))
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp))
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
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
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
                IncludeProperty = prop => {
                    if (prop.ToString().Equals("/" + nameof(deviceSample.DataFormat))) {
                        return false;
                    }
                    if (prop.ToString().Equals("/" + nameof(deviceSample.MacAddress))) {
                        return false;
                    }
                    return true;
                }
            }).ToArray();

            Assert.AreEqual(11, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
                IncludeProperty = prop => {
                    if (prop.ToString().Equals("/" + nameof(deviceSample.Temperature))) {
                        return true;
                    }
                    if (prop.ToString().Equals("/" + nameof(deviceSample.Humidity))) {
                        return true;
                    }
                    if (prop.ToString().Equals("/" + nameof(deviceSample.Pressure))) {
                        return true;
                    }
                    return false;
                }
            }).ToArray();

            Assert.AreEqual(3, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
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
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
                Recursive = true
            }).ToArray();

            Assert.AreEqual(16, samples.Length);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
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
                IncludeProperty = prop => !prop.Segments.Last().Value.Equals("location")
            }).ToArray();

            Assert.AreEqual(1, samples.Length);
            Assert.AreEqual("System A/Subsystem 1/measurements/temperature", samples[0].Key);
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
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
                IncludeProperty = prop => !prop.Segments.Last().Value.Equals("location")
            }).ToArray();

            Assert.AreEqual(1, samples.Length);
            Assert.AreEqual("System A/Subsystem 1/temperature", samples[0].Key);
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
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

            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
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
            Assert.AreEqual(TimestampSource.FallbackProvider, samples[0].TimestampSource);
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
            Assert.AreEqual(TimestampSource.CurrentTime, samples[0].TimestampSource);
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp))
            }).ToArray();

            Assert.AreEqual(13, samples.Length);

            var expectedTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(ms);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(expectedTimestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [TestMethod]
        public void ShoulAllowCustomTimestampParsing() {
            long secs = 1686559277;

            var deviceSample = new {
                Timestamp = secs,
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
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
                TimestampParser = element => new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(element.GetInt64())
            }).ToArray();

            Assert.AreEqual(13, samples.Length);

            var expectedTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(secs);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(expectedTimestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [TestMethod]
        public void ShouldAllowCustomStartPosition() {
            long ms = 1646312969367;

            var deviceSample = new {
                data = new {
                    time = ms,
                    device1 = new {
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
                    }
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                StartAt = JsonPointer.Parse("/data"),
                Recursive = true
            }).ToArray();

            Assert.AreEqual(13, samples.Length);
            Assert.IsTrue(samples.All(x => x.Key.StartsWith("device1/")));

            var expectedTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(ms);
            Assert.IsTrue(samples.All(x => x.Timestamp.UtcDateTime.Equals(expectedTimestamp.UtcDateTime)));
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [TestMethod]
        public void ShouldAllowNestedTimestampsInRecursiveMode() {
            var now = DateTimeOffset.UtcNow;

            var deviceSample = new { 
                data = new[] {
                    new {
                        time = now.AddHours(-2),
                        temperature = 19.3
                    },
                    new {
                        time = now.AddHours(-1),
                        temperature = 20.6
                    }
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                AllowNestedTimestamps = true
            }).ToArray();

            Assert.AreEqual(2, samples.Length);
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.AreEqual(deviceSample.data[0].time, samples[0].Timestamp);
            Assert.AreEqual(deviceSample.data[0].temperature, samples[0].Value);
            Assert.AreEqual(deviceSample.data[1].time, samples[1].Timestamp);
            Assert.AreEqual(deviceSample.data[1].temperature, samples[1].Value);
        }


        [TestMethod]
        public void ShouldNotAllowNestedTimestampsInRecursiveMode() {
            var now = DateTimeOffset.UtcNow;

            var deviceSample = new {
                time = now,
                data = new[] {
                    new {
                        time = now.AddHours(-2),
                        temperature = 19.3
                    },
                    new {
                        time = now.AddHours(-1),
                        temperature = 20.6
                    }
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                AllowNestedTimestamps = false
            }).ToArray();

            // 4 samples because the nested time properties are not treated as timestamps
            Assert.AreEqual(4, samples.Length);
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.IsTrue(samples.All(x => x.Timestamp.Equals(deviceSample.time)));

            Assert.AreEqual(JsonSerializer.Serialize(deviceSample.data[0].time).Trim('"'), samples[0].Value);
            Assert.AreEqual(deviceSample.data[0].temperature, samples[1].Value);
            Assert.AreEqual(JsonSerializer.Serialize(deviceSample.data[1].time).Trim('"'), samples[2].Value);
            Assert.AreEqual(deviceSample.data[1].temperature, samples[3].Value);
        }


        [TestMethod]
        public void ShouldInheritTimestampFromAncestorLevelInRecursiveMode() {
            var now = DateTimeOffset.UtcNow;

            var deviceSample = new {
                data = new { 
                    time = now,
                    samples = new[] {
                        new {
                            temperature = 19.3
                        },
                        new {
                            temperature = 20.6
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                AllowNestedTimestamps = true
            }).ToArray();

            Assert.AreEqual(2, samples.Length);
            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.AreEqual(deviceSample.data.time, samples[0].Timestamp);
            Assert.AreEqual(deviceSample.data.time, samples[1].Timestamp);
            Assert.AreEqual(deviceSample.data.samples[0].temperature, samples[0].Value);
            Assert.AreEqual(deviceSample.data.samples[1].temperature, samples[1].Value);
        }


        [TestMethod]
        public void ShouldIncludeArrayIndexesInSampleKeys() {
            var now = DateTimeOffset.UtcNow;

            var deviceSample = new {
                data = new[] {
                    new {
                        time = now.AddHours(-2),
                        temperature = 19.3
                    },
                    new {
                        time = now.AddHours(-1),
                        temperature = 20.6
                    }
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                AllowNestedTimestamps = true,
                IncludeArrayIndexesInSampleKeys = true
            }).ToArray(); 

            Assert.AreEqual(2, samples.Length);

            Assert.AreEqual("data/0/temperature", samples[0].Key);
            Assert.AreEqual("data/1/temperature", samples[1].Key);

            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.AreEqual(deviceSample.data[0].time, samples[0].Timestamp);
            Assert.AreEqual(deviceSample.data[1].time, samples[1].Timestamp);
            Assert.AreEqual(deviceSample.data[0].temperature, samples[0].Value);
            Assert.AreEqual(deviceSample.data[1].temperature, samples[1].Value);
        }


        [TestMethod]
        public void ShouldNotIncludeArrayIndexesInSampleKeys() {
            var now = DateTimeOffset.UtcNow;

            var deviceSample = new {
                data = new[] {
                    new {
                        time = now.AddHours(-2),
                        temperature = 19.3
                    },
                    new {
                        time = now.AddHours(-1),
                        temperature = 20.6
                    }
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                AllowNestedTimestamps = true,
                IncludeArrayIndexesInSampleKeys = false
            }).ToArray();

            Assert.AreEqual(2, samples.Length);

            Assert.AreEqual("data/temperature", samples[0].Key);
            Assert.AreEqual("data/temperature", samples[1].Key);

            Assert.IsTrue(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.AreEqual(deviceSample.data[0].time, samples[0].Timestamp);
            Assert.AreEqual(deviceSample.data[1].time, samples[1].Timestamp);
            Assert.AreEqual(deviceSample.data[0].temperature, samples[0].Value);
            Assert.AreEqual(deviceSample.data[1].temperature, samples[1].Value);
        }

    }
}
