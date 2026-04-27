using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace HotelManagementSystem.Tests.Logging
{
    /// <summary>
    /// Configuration-level smoke tests verifying that appsettings.json carries the
    /// expected Serilog shape (sinks, levels, connection-string wiring). Real sink
    /// behaviour is not exercised here — runtime verification belongs in integration tests.
    /// </summary>
    public class SerilogConfigurationTests
    {
        private static readonly string ApiAppSettingsPath =
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "HotelManagementSystem.API", "appsettings.json"));

        private static IConfigurationRoot LoadConfig() =>
            new ConfigurationBuilder()
                .AddJsonFile(ApiAppSettingsPath, optional: false)
                .Build();

        [Fact]
        public void AppSettings_HasSerilogSection()
        {
            var config = LoadConfig();
            var section = config.GetSection("Serilog");
            Assert.True(section.Exists(), "Serilog section missing from appsettings.json");
        }

        [Fact]
        public void Serilog_DeclaresFileAndMSSqlSinks()
        {
            var config = LoadConfig();
            var using_ = config.GetSection("Serilog:Using").Get<string[]>() ?? Array.Empty<string>();
            Assert.Contains("Serilog.Sinks.File", using_);
            Assert.Contains("Serilog.Sinks.MSSqlServer", using_);
        }

        [Fact]
        public void Serilog_HasFileSinkWritingToLogsFolder()
        {
            var config = LoadConfig();
            var writeTo = config.GetSection("Serilog:WriteTo").GetChildren().ToList();
            var fileSink = writeTo.FirstOrDefault(c => c["Name"] == "File");
            Assert.NotNull(fileSink);
            var path = fileSink!["Args:path"];
            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.Contains("Logs", path!);
            Assert.Equal("Day", fileSink["Args:rollingInterval"]);
        }

        [Fact]
        public void Serilog_HasMSSqlServerSinkWithAutoCreateTable()
        {
            var config = LoadConfig();
            var writeTo = config.GetSection("Serilog:WriteTo").GetChildren().ToList();
            var sqlSink = writeTo.FirstOrDefault(c => c["Name"] == "MSSqlServer");
            Assert.NotNull(sqlSink);
            // autoCreateSqlTable can be nested under sinkOptionsSection
            var autoCreate = sqlSink!["Args:sinkOptionsSection:autoCreateSqlTable"]
                          ?? sqlSink["Args:sinkOptions:autoCreateSqlTable"]
                          ?? sqlSink["Args:autoCreateSqlTable"];
            Assert.NotNull(autoCreate);
            Assert.Equal("true", autoCreate!.ToLowerInvariant());
        }

        [Fact]
        public void Serilog_MSSqlSink_UsesDefaultConnectionString()
        {
            var config = LoadConfig();
            var writeTo = config.GetSection("Serilog:WriteTo").GetChildren().ToList();
            var sqlSink = writeTo.First(c => c["Name"] == "MSSqlServer");
            var conn = sqlSink["Args:connectionString"];
            Assert.False(string.IsNullOrWhiteSpace(conn));
            // Either the literal name "DefaultConnection" or a full connection string is acceptable;
            // we expect the configuration-name pattern so logs follow the same DB as EF Core.
            Assert.Contains("DefaultConnection", conn!);
        }

        [Fact]
        public void Serilog_HasMinimumLevelConfigured()
        {
            var config = LoadConfig();
            var level = config["Serilog:MinimumLevel:Default"];
            Assert.False(string.IsNullOrWhiteSpace(level));
        }

        [Fact]
        public void Serilog_OverridesNoisyMicrosoftNamespaces()
        {
            var config = LoadConfig();
            var overrides = config.GetSection("Serilog:MinimumLevel:Override").GetChildren().ToList();
            Assert.Contains(overrides, c => c.Key.StartsWith("Microsoft"));
        }
    }
}
