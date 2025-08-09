using System;
using System.Linq;
using System.Text.Json;

using Json.Pointer;

using Xunit;

namespace Jaahas.Json.Tests {

    public class JsonTimeSeriesExtractorTests {


        [Fact]
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

            Assert.Equal(13, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [Fact]
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

            Assert.Equal(13, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [Fact]
        public void ShouldUseCustomKeyTemplate() {
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
                Template = TestContext.Current.TestCase.TestMethodName + "/{MacAddress}/{$prop}",
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
            }).ToArray();

            Assert.Equal(13, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.True(samples.All(x => x.Key.StartsWith(TestContext.Current.TestCase.TestMethodName + "/" + deviceSample.MacAddress)));
        }


        [Fact]
        public void ShouldUseCustomKeyTemplateWithDefaultReplacements() {
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
                Template = TestContext.Current.TestCase.TestMethodName + "/{MacAddress}/{Uuid}/{$prop}",
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

            Assert.Equal(13, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.True(samples.All(x => x.Key.StartsWith(TestContext.Current.TestCase.TestMethodName + "/" + deviceSample.MacAddress + "/" + guid)));
        }


        [Fact]
        public void ShouldUsePropertyPathInCustomTemplate() {
            var data = new { 
                A = new { 
                    B = new { 
                        C = new { 
                            Name = "Instrument-1",
                            Value = 99.997
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(data);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                    PointersToInclude = new JsonPointerMatch[] { "/A/B/C/Value" },
                }),
                Template = "{$prop-path}/{Name}"
            }).ToArray();

            Assert.Equal(1, samples.Length);
            Assert.Equal("A/B/C/Instrument-1", samples[0].Key);
            Assert.Equal(data.A.B.C.Value, samples[0].Value);
            Assert.Equal(TimestampSource.CurrentTime, samples[0].TimestampSource);
        }


        [Fact]
        public void ShouldUsePropertyPathWithoutArrayIndexesInCustomTemplate() {
            var data = new {
                A = new {
                    B = new {
                        C = new[] {
                            new {
                                Name = "Instrument-1",
                                Value = 99.997
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(data);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() {
                    PointersToInclude = new JsonPointerMatch[] { "/A/B/C/0/Value" },
                }),
                Template = "{$prop-path}/{Name}",
                IncludeArrayIndexesInSampleKeys = false
            }).ToArray();

            Assert.Equal(1, samples.Length);
            Assert.Equal("A/B/C/Instrument-1", samples[0].Key);
            Assert.Equal(data.A.B.C[0].Value, samples[0].Value);
            Assert.Equal(TimestampSource.CurrentTime, samples[0].TimestampSource);
        }


        [Fact]
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
                Template = TestContext.Current.TestCase.TestMethodName + "/{MacAddress}/{DataFormat}/{$prop}",
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                    PointersToExclude = new JsonPointerMatch[] {
                        $"/{nameof(deviceSample.DataFormat)}",
                        $"/{nameof(deviceSample.MacAddress)}"
                    }
                })
            }).ToArray();

            Assert.Equal(11, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.True(samples.All(x => x.Key.StartsWith(TestContext.Current.TestCase.TestMethodName + "/" + deviceSample.MacAddress + "/" + deviceSample.DataFormat)));
        }


        [Fact]
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
                Template = TestContext.Current.TestCase.TestMethodName + "/{MacAddress}/{DataFormat}/{$prop}",
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() {
                    PointersToInclude = new JsonPointerMatch[] {
                        $"/{nameof(deviceSample.Temperature)}",
                        $"/{nameof(deviceSample.Humidity)}",
                        $"/{nameof(deviceSample.Pressure)}"
                    }
                })
            }).ToArray();

            Assert.Equal(3, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.True(samples.All(x => x.Key.StartsWith(TestContext.Current.TestCase.TestMethodName + "/" + deviceSample.MacAddress + "/" + deviceSample.DataFormat)));
        }


        [Fact]
        public void ShouldIncludePropertiesUsingMqttMultiLevelMatch() {
            var deviceSample = new { 
                Data = new {
                    Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                    SignalStrength = -75,
                    DataFormat = 5,
                    Temperature = 19.3,
                    Humidity = 37.905,
                    Pressure = 1013.35,
                    Acceleration = new {
                        X = -0.872,
                        Y = 0.512,
                        Z = -0.04
                    },
                    BatteryVoltage = 3.085,
                    TxPower = 4,
                    MovementCounter = 5,
                    MeasurementSequence = 34425,
                    MacAddress = "AB:CD:EF:01:23:45"
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                TimestampProperty = JsonPointer.Parse($"/{nameof(deviceSample.Data)}/{nameof(deviceSample.Data.Timestamp)}"),
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                    AllowWildcardExpressions = true,
                    PointersToInclude = new JsonPointerMatch[] {
                        $"/{nameof(deviceSample.Data)}/{nameof(deviceSample.Data.Acceleration)}/#",
                    }
                })
            }).ToArray();

            Assert.Equal(3, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Data.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.True(samples.All(x => x.Key.StartsWith("Data/Acceleration/")));
        }


        [Fact]
        public void ShouldIncludePropertiesUsingMqttSingleLevelMatch() {
            var deviceSample = new {
                Data = new {
                    Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                    SignalStrength = -75,
                    DataFormat = 5,
                    Temperature = 19.3,
                    Humidity = 37.905,
                    Pressure = 1013.35,
                    Acceleration = new {
                        X = -0.872,
                        Y = 0.512,
                        Z = -0.04
                    },
                    BatteryVoltage = 3.085,
                    TxPower = 4,
                    MovementCounter = 5,
                    MeasurementSequence = 34425,
                    MacAddress = "AB:CD:EF:01:23:45"
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                TimestampProperty = JsonPointer.Parse($"/{nameof(deviceSample.Data)}/{nameof(deviceSample.Data.Timestamp)}"),
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                    AllowWildcardExpressions = true,
                    PointersToInclude = new JsonPointerMatch[] {
                        $"/+/+/X",
                    }
                })
            }).ToArray();

            Assert.Equal(1, samples.Length);
            var sample = samples[0];

            Assert.Equal(deviceSample.Data.Timestamp.UtcDateTime, sample.Timestamp.UtcDateTime);
            Assert.Equal(TimestampSource.Document, sample.TimestampSource);
            Assert.Equal("Data/Acceleration/X", sample.Key);
        }


        [Fact]
        public void ShouldIncludePropertiesUsingMultiCharacterPatternMatch() {
            var deviceSample = new {
                Data = new {
                    Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                    SignalStrength = -75,
                    DataFormat = 5,
                    Temperature = 19.3,
                    Humidity = 37.905,
                    Pressure = 1013.35,
                    Acceleration = new {
                        X = -0.872,
                        Y = 0.512,
                        Z = -0.04
                    },
                    BatteryVoltage = 3.085,
                    TxPower = 4,
                    MovementCounter = 5,
                    MeasurementSequence = 34425,
                    MacAddress = "AB:CD:EF:01:23:45"
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                TimestampProperty = JsonPointer.Parse($"/{nameof(deviceSample.Data)}/{nameof(deviceSample.Data.Timestamp)}"),
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                    AllowWildcardExpressions = true,
                    PointersToInclude = new JsonPointerMatch[] {
                        "*/X",
                    }
                })
            }).ToArray();

            Assert.Equal(1, samples.Length);
            var sample = samples[0];

            Assert.Equal(deviceSample.Data.Timestamp.UtcDateTime, sample.Timestamp.UtcDateTime);
            Assert.Equal(TimestampSource.Document, sample.TimestampSource);
            Assert.Equal("Data/Acceleration/X", sample.Key);
        }


        [Fact]
        public void ShouldIncludePropertiesUsingSingleCharacterPatternMatch() {
            var deviceSample = new {
                Data = new {
                    Timestamp = DateTimeOffset.Parse("2021-05-28T17:41:09.7031076+03:00"),
                    SignalStrength = -75,
                    DataFormat = 5,
                    Temperature = 19.3,
                    Humidity = 37.905,
                    Pressure = 1013.35,
                    Acceleration = new {
                        X = -0.872,
                        Y = 0.512,
                        Z = -0.04
                    },
                    BatteryVoltage = 3.085,
                    TxPower = 4,
                    MovementCounter = 5,
                    MeasurementSequence = 34425,
                    MacAddress = "AB:CD:EF:01:23:45"
                }
            };

            var json = JsonSerializer.Serialize(deviceSample);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                TimestampProperty = JsonPointer.Parse($"/{nameof(deviceSample.Data)}/{nameof(deviceSample.Data.Timestamp)}"),
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                    AllowWildcardExpressions = true,
                    PointersToInclude = new JsonPointerMatch[] {
                        $"/{nameof(deviceSample.Data)}/{nameof(deviceSample.Data.Acceleration)}/?",
                    }
                })
            }).ToArray();

            Assert.Equal(3, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Data.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.True(samples.All(x => x.Key.StartsWith("Data/Acceleration/")));
        }


        [Fact]
        public void ShouldParseTopLevelArray() {
            var deviceSamples = new[] {
                new { Value = 55.5 },
                new { Value = 417.1 },
                new { Value = -0.0032 },
                new { Value = 14.0 },
            };

            var json = JsonSerializer.Serialize(deviceSamples);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.Current.TestCase.TestMethodName + "/sample/{$prop}"
            }).ToArray();

            Assert.Equal(deviceSamples.Length, samples.Length);
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
            Assert.True(samples.All(x => string.Equals(x.Key, TestContext.Current.TestCase.TestMethodName + "/sample/Value")));

            for (var i = 0; i < deviceSamples.Length; i++) {
                Assert.Equal(deviceSamples[i].Value, (double) samples[i].Value!);
            }
        }


        [Fact]
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
                Template = TestContext.Current.TestCase.TestMethodName + "/{$prop}",
                TimestampProperty = JsonPointer.Parse("/" + nameof(deviceSample.Timestamp)),
                Recursive = true
            }).ToArray();

            Assert.Equal(16, samples.Length);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(deviceSample.Timestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [Fact]
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
                CanProcessElement = (_, prop, _) => !prop.Last().Equals("location")
            }).ToArray();

            Assert.Equal(1, samples.Length);
            Assert.Equal("System A/Subsystem 1/measurements/temperature", samples[0].Key);
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
        }


        [Fact]
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
                CanProcessElement = (_, prop, _) => !prop.Last().Equals("location")
            }).ToArray();

            Assert.Equal(1, samples.Length);
            Assert.Equal("System A/Subsystem 1/temperature", samples[0].Key);
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
        }


        [Fact]
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

            Assert.Equal(2, samples.Length);

            Assert.Equal("location", samples[0].Key);
            Assert.Equal(testObject.location, samples[0].Value);
            
            Assert.Equal("measurements", samples[1].Key);
            Assert.Equal(@"{""location"":""Subsystem 1"",""temperature"":14}", samples[1].Value);

            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.CurrentTime));
        }


        [Fact]
        public void ShouldObeyRecursionDepthLimitWhenUsingAnInclusionDelegate() {
            var testObject = new {
                parent = new {
                    child = new {
                        value = 100d
                    }
                }
            };

            var json = JsonSerializer.Serialize(testObject);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Recursive = true,
                MaxDepth = 3,
                CanProcessElement = TimeSeriesExtractor.CreateJsonPointerMatchDelegate(new JsonPointerMatchDelegateOptions() { 
                    AllowWildcardExpressions = true,
                    PointersToInclude = new JsonPointerMatch[] { "/+/+/value" }
                })
            }).ToArray();

            Assert.Equal(1, samples.Length);
            Assert.Equal("parent/child/value", samples[0].Key);
            Assert.Equal(testObject.parent.child.value, samples[0].Value);
            Assert.Equal(TimestampSource.CurrentTime, samples[0].TimestampSource);
        }


        [Fact]
        public void ShouldUseFallbackTimestamp() {
            var testObject = new { 
                value = 99
            };

            var json = JsonSerializer.Serialize(testObject);

            var fallbackTimestamp = DateTimeOffset.Parse("1999-12-31");

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.Current.TestCase.TestMethodName + "/{$prop}",
                GetDefaultTimestamp = () => fallbackTimestamp
            }).ToArray();

            Assert.Equal(1, samples.Length);
            Assert.Equal(fallbackTimestamp, samples[0].Timestamp);
            Assert.Equal(TimestampSource.FallbackProvider, samples[0].TimestampSource);
        }


        [Fact]
        public void ShouldAllowUnresolvedTemplateReplacements() {
            var testObject = new {
                value = 99
            };

            string json = JsonSerializer.Serialize(testObject);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() { 
                Template = TestContext.Current.TestCase.TestMethodName + "/{deviceId}/{$prop}",
                AllowUnresolvedTemplateReplacements = true
            }).ToArray();

            Assert.Equal(1, samples.Length);
            Assert.Equal(TestContext.Current.TestCase.TestMethodName + "/{deviceId}/value", samples[0].Key);
            Assert.Equal(TimestampSource.CurrentTime, samples[0].TimestampSource);
        }


        [Fact]
        public void ShouldNotAllowUnresolvedTemplateReplacements() {
            var testObject = new {
                value = 99
            };

            string json = JsonSerializer.Serialize(testObject);

            var samples = TimeSeriesExtractor.GetSamples(json, new TimeSeriesExtractorOptions() {
                Template = TestContext.Current.TestCase.TestMethodName + "/{deviceId}/{$prop}",
                AllowUnresolvedTemplateReplacements = false
            }).ToArray();

            Assert.Equal(0, samples.Length);
        }


        [Fact]
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

            Assert.Equal(13, samples.Length);

            var expectedTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(ms);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(expectedTimestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [Fact]
        public void ShouldAllowCustomTimestampParsing() {
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

            Assert.Equal(13, samples.Length);

            var expectedTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(secs);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(expectedTimestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [Fact]
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

            Assert.Equal(13, samples.Length);
            Assert.True(samples.All(x => x.Key.StartsWith("device1/")));

            var expectedTimestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(ms);
            Assert.True(samples.All(x => x.Timestamp.UtcDateTime.Equals(expectedTimestamp.UtcDateTime)));
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
        }


        [Fact]
        public void ShouldAllowNestedTimestampsInRecursiveMode() {
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
                AllowNestedTimestamps = true
            }).ToArray();

            Assert.Equal(2, samples.Length);
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.Equal(deviceSample.data[0].time, samples[0].Timestamp);
            Assert.Equal(deviceSample.data[0].temperature, samples[0].Value);
            Assert.Equal(deviceSample.data[1].time, samples[1].Timestamp);
            Assert.Equal(deviceSample.data[1].temperature, samples[1].Value);
        }
        

        [Fact]
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
            Assert.Equal(4, samples.Length);
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.True(samples.All(x => x.Timestamp.Equals(deviceSample.time)));

            Assert.Equal(JsonSerializer.Serialize(deviceSample.data[0].time).Trim('"'), samples[0].Value);
            Assert.Equal(deviceSample.data[0].temperature, samples[1].Value);
            Assert.Equal(JsonSerializer.Serialize(deviceSample.data[1].time).Trim('"'), samples[2].Value);
            Assert.Equal(deviceSample.data[1].temperature, samples[3].Value);
        }


        [Fact]
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

            Assert.Equal(2, samples.Length);
            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.Equal(deviceSample.data.time, samples[0].Timestamp);
            Assert.Equal(deviceSample.data.time, samples[1].Timestamp);
            Assert.Equal(deviceSample.data.samples[0].temperature, samples[0].Value);
            Assert.Equal(deviceSample.data.samples[1].temperature, samples[1].Value);
        }


        [Fact]
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

            Assert.Equal(2, samples.Length);

            Assert.Equal("data/0/temperature", samples[0].Key);
            Assert.Equal("data/1/temperature", samples[1].Key);

            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.Equal(deviceSample.data[0].time, samples[0].Timestamp);
            Assert.Equal(deviceSample.data[1].time, samples[1].Timestamp);
            Assert.Equal(deviceSample.data[0].temperature, samples[0].Value);
            Assert.Equal(deviceSample.data[1].temperature, samples[1].Value);
        }


        [Fact]
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

            Assert.Equal(2, samples.Length);

            Assert.Equal("data/temperature", samples[0].Key);
            Assert.Equal("data/temperature", samples[1].Key);

            Assert.True(samples.All(x => x.TimestampSource == TimestampSource.Document));
            Assert.Equal(deviceSample.data[0].time, samples[0].Timestamp);
            Assert.Equal(deviceSample.data[1].time, samples[1].Timestamp);
            Assert.Equal(deviceSample.data[0].temperature, samples[0].Value);
            Assert.Equal(deviceSample.data[1].temperature, samples[1].Value);
        }

    }
}
