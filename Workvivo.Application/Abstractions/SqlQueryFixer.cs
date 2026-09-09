namespace Workvivo.Application.Abstractions;
public class SqlQueryFixer
{
    // Generic function to fix ambiguous column names and add N' for string literals
    public static string FixSqlQuery(string query)
    {
        // Fix ambiguous column names (if necessary)
        query = FixAmbiguousColumnNames(query);

        // Add N' prefix for string literals
        query = FixStringLiterals(query);

        return query;
    }

    // Function to fix ambiguous column names by adding table aliases (generic)
    private static string FixAmbiguousColumnNames(string query)
    {
        // Extract table aliases from FROM and JOIN clauses
        Dictionary<string, string> tableAliases = ExtractTableAliases(query);

        // Extract all columns from SELECT and GROUP BY clauses
        List<string> ambiguousColumns = ExtractColumns(query);

        // Fix ambiguous columns by adding the appropriate table alias
        foreach (var column in ambiguousColumns)
        {
            string pattern = $@"(?<![\w\.]){column}(?![\w])"; // Match column without table alias
            foreach (var tableAlias in tableAliases)
            {
                if (query.Contains($"{tableAlias.Key}.{column}")) // If the column already has an alias, skip it
                    continue;

                // Replace ambiguous column with the appropriate alias
                string replacement = $"{tableAlias.Value}.{column}";
                query = Regex.Replace(query, pattern, replacement);
            }
        }

        return query;
    }

    // Function to add N' prefix for all string literals in the query
    private static string FixStringLiterals(string query)
    {
        // Regular expression to match all string literals in the query
        string pattern = @"(?<!N)\'([^']*)\'";

        // Replace single-quoted strings with N-prefixed strings
        return Regex.Replace(query, pattern, "N'$1'");
    }

    // Function to extract table aliases from the SQL query
    private static Dictionary<string, string> ExtractTableAliases(string query)
    {
        var tableAliases = new Dictionary<string, string>();

        // Pattern to match table aliases in FROM and JOIN clauses (e.g., "tableName AS alias" or "tableName alias")
        string pattern = @"\bFROM\s+(\w+)\s+(\w+)|\bJOIN\s+(\w+)\s+(\w+)";
        var matches = Regex.Matches(query, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            if (match.Groups[2].Success)
            {
                tableAliases[match.Groups[1].Value] = match.Groups[2].Value; // tableName AS alias
            }
            if (match.Groups[4].Success)
            {
                tableAliases[match.Groups[3].Value] = match.Groups[4].Value; // JOIN tableName alias
            }
        }

        return tableAliases;
    }

    // Function to extract ambiguous column names from SELECT and GROUP BY clauses
    private static List<string> ExtractColumns(string query)
    {
        var columns = new List<string>();

        // Pattern to match columns in SELECT and GROUP BY clauses
        string selectPattern = @"SELECT\s+([^FROM]+)\s+FROM";
        string groupByPattern = @"GROUP\s+BY\s+([^;]+)";

        var selectMatch = Regex.Match(query, selectPattern, RegexOptions.IgnoreCase);
        var groupByMatch = Regex.Match(query, groupByPattern, RegexOptions.IgnoreCase);

        // Extract columns from SELECT clause
        if (selectMatch.Success)
        {
            string selectColumns = selectMatch.Groups[1].Value;
            columns.AddRange(ExtractColumnNames(selectColumns));
        }

        // Extract columns from GROUP BY clause
        if (groupByMatch.Success)
        {
            string groupByColumns = groupByMatch.Groups[1].Value;
            columns.AddRange(ExtractColumnNames(groupByColumns));
        }

        return columns;
    }

    // Helper function to extract individual column names
    private static List<string> ExtractColumnNames(string columnsPart)
    {
        var columns = new List<string>();
        var columnMatches = Regex.Matches(columnsPart, @"\b\w+\b", RegexOptions.IgnoreCase);

        foreach (Match match in columnMatches)
        {
            columns.Add(match.Value);
        }

        return columns;
    }
}
