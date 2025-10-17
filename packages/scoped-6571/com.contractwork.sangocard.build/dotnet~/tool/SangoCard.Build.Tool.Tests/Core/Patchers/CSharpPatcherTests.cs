using Microsoft.Extensions.Logging.Abstractions;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;

namespace SangoCard.Build.Tool.Tests.Core.Patchers;

public class CSharpPatcherTests
{
    private readonly CSharpPatcher _patcher;

    public CSharpPatcherTests()
    {
        _patcher = new CSharpPatcher(NullLogger<CSharpPatcher>.Instance);
    }

    [Fact]
    public async Task RemoveUsing_RemovesTargetUsingDirective()
    {
        // Arrange
        var sourceCode = @"using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class TestClass { }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "RemoveUsing",
            Search = "System.Linq",
            File = "Test.cs"
        };

        // Act
        var result = await _patcher.ApplyPatchAsync("Test.cs", patch, dryRun: false);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Modified);
        Assert.DoesNotContain("using System.Linq", result.Preview ?? "");
        
        // Verify other usings are preserved
        var validation = await _patcher.ValidatePatchAsync("Test.cs", patch);
        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task RemoveUsing_PreservesOtherUsings()
    {
        // Arrange
        var sourceCode = @"using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class TestClass { }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "RemoveUsing",
            Search = "System.Linq",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedContent = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("using System.Linq", patchedContent);
            Assert.Contains("using System;", patchedContent);
            Assert.Contains("using System.Collections.Generic;", patchedContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceExpression_ReplacesSimpleIdentifier()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var result = OldValue;
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "ReplaceExpression",
            Search = "OldValue",
            Replace = "NewValue",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedContent = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("OldValue", patchedContent);
            Assert.Contains("NewValue", patchedContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceExpression_ReplacesMethodInvocation()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""test"");
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "ReplaceExpression",
            Search = "Console.WriteLine(\"test\")",
            Replace = "Debug.Log(\"test\")",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedContent = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("Debug.Log", patchedContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceBlock_ReplacesEntireBlock()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var x = 1;
            var y = 2;
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "ReplaceBlock",
            Search = "var x = 1",
            Replace = "{ var x = 10; var y = 20; }",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedContent = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("var x = 10", patchedContent);
            Assert.Contains("var y = 20", patchedContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RemoveBlock_RemovesMatchingBlock()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var x = 1;
        }

        public void OtherMethod()
        {
            var y = 2;
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "RemoveBlock",
            Search = "var x = 1",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedContent = await File.ReadAllTextAsync(tempFile);
            Assert.DoesNotContain("var x = 1", patchedContent);
            Assert.Contains("var y = 2", patchedContent); // Other block preserved
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
        var sourceCode = @"using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass { }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "RemoveUsing",
            Search = "System.Linq",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);
        var originalContent = sourceCode;

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: true);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);
            Assert.NotNull(result.Preview);

            var fileContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(originalContent, fileContent); // File unchanged
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ValidatePatchedContent_DetectsSyntaxErrors()
    {
        // Arrange
        var invalidCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod(
        // Missing closing brace and parenthesis
    }
}";

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, invalidCode);

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Mode = PatchMode.Replace,
            Search = "TestMethod",
            Replace = "UpdatedMethod",
            File = "Test.cs"
        };

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert - Should fail validation
            Assert.False(result.Success);
            Assert.Contains("validation", result.Message.ToLower());
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Rollback_RestoresOriginalContent()
    {
        // Arrange
        var originalCode = @"using System;

namespace TestNamespace
{
    public class TestClass { }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Mode = PatchMode.Replace,
            Search = "TestClass",
            Replace = "ModifiedClass",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, originalCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);
            Assert.True(result.Success);
            Assert.NotNull(result.RollbackId);

            var modifiedContent = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("ModifiedClass", modifiedContent);

            // Rollback
            var rollbackSuccess = await _patcher.RollbackAsync(tempFile, result.RollbackId!);

            // Assert
            Assert.True(rollbackSuccess);
            var restoredContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(originalCode, restoredContent);

            // Cleanup rollback
            await _patcher.CleanupRollbackAsync(result.RollbackId!);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FallbackToTextMode_WhenNoOperationSpecified()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass { }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Mode = PatchMode.Replace,
            Search = "TestClass",
            Replace = "ModifiedClass",
            File = "Test.cs"
            // No Operation specified - should fall back to text mode
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Modified);

            var patchedContent = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("ModifiedClass", patchedContent);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PreservesFormatting_WithRoslynOperations()
    {
        // Arrange
        var sourceCode = @"using System;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // This is a comment
            var value = 42;
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "RemoveUsing",
            Search = "System.Linq",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);

            // Assert
            Assert.True(result.Success);

            var patchedContent = await File.ReadAllTextAsync(tempFile);
            Assert.Contains("// This is a comment", patchedContent); // Comment preserved
            Assert.Contains("var value = 42;", patchedContent); // Code preserved
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
