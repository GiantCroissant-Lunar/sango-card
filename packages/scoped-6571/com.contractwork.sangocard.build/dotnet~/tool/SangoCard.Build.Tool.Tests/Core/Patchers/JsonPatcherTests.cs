using Microsoft.Extensions.Logging.Abstractions;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;

namespace SangoCard.Build.Tool.Tests.Core.Patchers;

public class JsonPatcherTests
{
    private readonly JsonPatcher _patcher;

    public JsonPatcherTests()
    {
        _patcher = new JsonPatcher(NullLogger<JsonPatcher>.Instance);
    }

    [Fact]
    public async Task AddProperty_ToRootObject_AddsProperty()
    {
        // Arrange
        var json = @"{
  ""name"": ""test"",
  ""version"": ""1.0.0""
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "AddProperty",
            Search = "",
            Replace = "description:A test package",
            File = "package.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("\"description\"", patchedJson);
            Assert.Contains("A test package", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AddProperty_ToNestedObject_AddsProperty()
    {
        // Arrange
        var json = @"{
  ""config"": {
    ""debug"": true
  }
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "AddProperty",
            Search = "config",
            Replace = "verbose:false",
            File = "config.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("\"verbose\"", patchedJson);
            Assert.Contains("false", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task AddProperty_WithJsonObject_AddsComplexProperty()
    {
        // Arrange
        var json = @"{
  ""name"": ""test""
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "AddProperty",
            Search = "",
            Replace = @"{""dependencies"":{""package"":""1.0.0""}}",
            File = "package.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("\"dependencies\"", patchedJson);
            Assert.Contains("\"package\"", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RemoveProperty_FromRootObject_RemovesProperty()
    {
        // Arrange
        var json = @"{
  ""name"": ""test"",
  ""version"": ""1.0.0"",
  ""deprecated"": true
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "RemoveProperty",
            Search = "deprecated",
            File = "package.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("deprecated", patchedJson);
            Assert.Contains("name", patchedJson);
            Assert.Contains("version", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RemoveProperty_FromNestedObject_RemovesProperty()
    {
        // Arrange
        var json = @"{
  ""config"": {
    ""debug"": true,
    ""verbose"": false,
    ""legacy"": true
  }
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "RemoveProperty",
            Search = "config.legacy",
            File = "config.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("legacy", patchedJson);
            Assert.Contains("debug", patchedJson);
            Assert.Contains("verbose", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceValue_SimpleValue_ReplacesValue()
    {
        // Arrange
        var json = @"{
  ""version"": ""1.0.0"",
  ""name"": ""test""
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "version",
            Replace = "\"2.0.0\"",
            File = "package.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("\"2.0.0\"", patchedJson);
            Assert.DoesNotContain("\"1.0.0\"", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceValue_NestedValue_ReplacesValue()
    {
        // Arrange
        var json = @"{
  ""config"": {
    ""timeout"": 30,
    ""retries"": 3
  }
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "config.timeout",
            Replace = "60",
            File = "config.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("60", patchedJson);
            Assert.DoesNotContain("30", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceValue_WithObject_ReplacesWithComplexValue()
    {
        // Arrange
        var json = @"{
  ""server"": ""localhost""
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "server",
            Replace = @"{""host"":""localhost"",""port"":8080}",
            File = "config.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("\"host\"", patchedJson);
            Assert.Contains("\"port\"", patchedJson);
            Assert.Contains("8080", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task HandleArrays_ReplaceArrayElement_ReplacesElement()
    {
        // Arrange
        var json = @"{
  ""tags"": [""tag1"", ""tag2"", ""tag3""]
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "tags.1",
            Replace = "\"updated-tag\"",
            File = "data.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("updated-tag", patchedJson);
            Assert.Contains("tag1", patchedJson);
            Assert.Contains("tag3", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PreservesFormatting_AfterPatch()
    {
        // Arrange
        var json = @"{
  ""name"": ""test"",
  ""version"": ""1.0.0""
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "version",
            Replace = "\"2.0.0\"",
            File = "package.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            // Check that indentation is preserved (2 spaces)
            Assert.Contains("  \"name\"", patchedJson);
            Assert.Contains("  \"version\"", patchedJson);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ValidatesJson_AfterPatching()
    {
        // Arrange
        var json = @"{
  ""name"": ""test""
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "name",
            Replace = "\"valid-name\"",
            File = "test.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);

            // Verify JSON is still valid
            var patchedJson = await File.ReadAllTextAsync(tempFile);
            var parsed = System.Text.Json.JsonDocument.Parse(patchedJson);
            Assert.NotNull(parsed);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DryRun_DoesNotModifyFile()
    {
        // Arrange
        var json = @"{
  ""name"": ""test"",
  ""version"": ""1.0.0""
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "version",
            Replace = "\"2.0.0\"",
            File = "package.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: true);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);
            Assert.NotNull(result.Preview);

            var fileContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(json, fileContent); // File unchanged
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComplexNestedStructure_ModifiesCorrectly()
    {
        // Arrange
        var json = @"{
  ""project"": {
    ""name"": ""MyProject"",
    ""settings"": {
      ""build"": {
        ""platform"": ""Android"",
        ""version"": ""1.0""
      }
    }
  }
}";

        var patch = new CodePatch
        {
            Type = PatchType.Json,
            Operation = "ReplaceValue",
            Search = "project.settings.build.version",
            Replace = "\"2.0\"",
            File = "project.json"
        };

        var tempFile = Path.GetTempFileName() + ".json";
        await File.WriteAllTextAsync(tempFile, json);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedJson = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("\"2.0\"", patchedJson);
            Assert.Contains("Android", patchedJson); // Other values preserved
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
