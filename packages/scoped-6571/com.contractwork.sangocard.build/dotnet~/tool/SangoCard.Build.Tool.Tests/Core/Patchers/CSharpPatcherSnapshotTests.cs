using Microsoft.Extensions.Logging.Abstractions;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;

namespace SangoCard.Build.Tool.Tests.Core.Patchers;

public class CSharpPatcherSnapshotTests
{
    private readonly CSharpPatcher _patcher;

    public CSharpPatcherSnapshotTests()
    {
        _patcher = new CSharpPatcher(NullLogger<CSharpPatcher>.Instance);
    }

    [Fact]
    public async Task RemoveUsing_SingleUsing_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var list = new List<int>();
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
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified,
                    result.Message
                }
            }).UseMethodName("RemoveUsing_SingleUsing");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RemoveUsing_MultipleUsings_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

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
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("RemoveUsing_MultipleUsings");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceExpression_SimpleIdentifier_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var oldValue = 10;
            var result = OldValue + 5;
            Console.WriteLine(OldValue);
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
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("ReplaceExpression_SimpleIdentifier");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceExpression_MethodInvocation_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""Hello"");
            var x = 1;
            Console.WriteLine(""World"");
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "ReplaceExpression",
            Search = "Console.WriteLine(\"Hello\")",
            Replace = "Debug.Log(\"Hello\")",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("ReplaceExpression_MethodInvocation");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReplaceBlock_MethodBody_SnapshotMatch()
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
            Console.WriteLine(x + y);
        }

        public void OtherMethod()
        {
            var z = 3;
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "ReplaceBlock",
            Search = "var x = 1",
            Replace = "{ var x = 10; var y = 20; Console.WriteLine(x * y); }",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("ReplaceBlock_MethodBody");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RemoveBlock_EntireMethod_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void MethodToRemove()
        {
            var x = 1;
            Console.WriteLine(x);
        }

        public void MethodToKeep()
        {
            var y = 2;
            Console.WriteLine(y);
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "RemoveBlock",
            Search = "MethodToRemove",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("RemoveBlock_EntireMethod");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PreservesComments_WithRoslynOperations_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"using System;
using System.Linq; // For LINQ operations
using System.Collections.Generic;

namespace TestNamespace
{
    /// <summary>
    /// This is a test class.
    /// </summary>
    public class TestClass
    {
        // This is a field comment
        private int _value;

        /// <summary>
        /// Test method with documentation.
        /// </summary>
        public void TestMethod()
        {
            // Inline comment
            var x = 1; // End of line comment
            /* Multi-line
               comment */
            var y = 2;
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
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("PreservesComments_WithRoslynOperations");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ComplexScenario_MultipleOperations_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"using System;
using System.Collections.Generic;
using System.Linq;

namespace SangoCard.Build.Example
{
    public class BuildConfiguration
    {
        private string _oldValue = ""legacy"";

        public void Configure()
        {
            var settings = new Dictionary<string, object>();
            settings[""key""] = _oldValue;
            Console.WriteLine(""Configuring build..."");
        }

        [Obsolete]
        public void LegacyMethod()
        {
            // This method is deprecated
            Console.WriteLine(""Legacy"");
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "RemoveUsing",
            Search = "System.Linq",
            File = "BuildConfiguration.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("ComplexScenario_MultipleOperations");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DryRun_PreviewGeneration_SnapshotMatch()
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

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: true);

            // Assert - File should be unchanged
            var fileContent = await File.ReadAllTextAsync(tempFile);
            await Verify(new
            {
                Original = sourceCode,
                FileAfterDryRun = fileContent,
                Result = new
                {
                    result.Success,
                    result.Modified,
                    result.Message,
                    result.Preview
                }
            }).UseMethodName("DryRun_PreviewGeneration");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task FallbackToTextMode_NoOperation_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void OldMethod() { }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Mode = PatchMode.Replace,
            Search = "OldMethod",
            Replace = "NewMethod",
            File = "Test.cs"
            // No Operation - should use text-based replacement
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("FallbackToTextMode_NoOperation");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PreservesIndentation_NestedBlocks_SnapshotMatch()
    {
        // Arrange
        var sourceCode = @"namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            if (true)
            {
                if (true)
                {
                    var x = 1;
                    Console.WriteLine(x);
                }
            }
        }
    }
}";

        var patch = new CodePatch
        {
            Type = PatchType.CSharp,
            Operation = "ReplaceExpression",
            Search = "x",
            Replace = "result",
            File = "Test.cs"
        };

        var tempFile = Path.GetTempFileName() + ".cs";
        await File.WriteAllTextAsync(tempFile, sourceCode);

        try
        {
            // Act
            var result = await _patcher.ApplyPatchAsync(tempFile, patch, dryRun: false);
            var patchedContent = await File.ReadAllTextAsync(tempFile);

            // Assert
            await Verify(new
            {
                Original = sourceCode,
                Patched = patchedContent,
                Result = new
                {
                    result.Success,
                    result.Modified
                }
            }).UseMethodName("PreservesIndentation_NestedBlocks");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
