using FoundryAgentLocal.Worker;

namespace FoundryAgentLocal.Tests;

public class ResearchPromptBuilderTests
{
    [Theory]
    [InlineData("climate policy impacts", "climate policy impacts")]
    [InlineData("  quantum computing trends  ", "quantum computing trends")]
    [InlineData("AI in healthcare", "AI in healthcare")]
    public void Build_NormalInput_ContainsTopic(string folderName, string expectedFragment)
    {
        var result = ResearchPromptBuilder.Build(folderName);

        Assert.Contains(expectedFragment, result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Build_EmptyOrWhitespace_ReturnsEmpty(string? folderName)
    {
        var result = ResearchPromptBuilder.Build(folderName!);

        Assert.Empty(result);
    }

    [Fact]
    public void Build_SpecialCharacters_IncludesFullName()
    {
        var folderName = "U.S. drug pricing & reform";

        var result = ResearchPromptBuilder.Build(folderName);

        Assert.Contains(folderName.Trim(), result);
    }

    [Fact]
    public void SystemInstructions_IsNotEmpty()
    {
        Assert.NotEmpty(ResearchPromptBuilder.SystemInstructions);
    }
}
