namespace FoundryAgentLocal.Worker;

public static class ResearchPromptBuilder
{
    public static string Build(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return string.Empty;

        return $"Research the following topic and produce a concise, structured plain-text report: {folderName.Trim()}";
    }

    public static string SystemInstructions =>
        "You are a research assistant. Gather key facts, recent developments, and implications. " +
        "Write a structured plain-text report with clear sections. Be concise and factual.";
}
