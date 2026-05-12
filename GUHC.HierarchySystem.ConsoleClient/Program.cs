using GUHC.HierarchySystem.Core.DTOs;
using System.Net.Http.Json;

var apiUrl = "https://localhost:7246";
var client = new HttpClient();

Console.WriteLine("Account Hierarchy Viewer");
Console.WriteLine("========================");
Console.WriteLine("Type 'exit' or 'quit' to close the program\n");

while (true)
{
    try
    {
        // Ask user for account ID
        Console.Write("Enter Account ID (or 'exit' to quit): ");
        string input = Console.ReadLine();

        // Check for exit command
        if (string.IsNullOrEmpty(input) ||
            input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\nGoodbye!");
            break;
        }

        // Try to parse the account ID
        if (int.TryParse(input, out var accountId))
        {
            // Fetch and display specific subtree
            await DisplaySubtreeAsync(client, apiUrl, accountId);
            Console.WriteLine(); // Add empty line for better readability
        }
        else
        {
            Console.WriteLine($"\nInvalid input '{input}'. Please enter a valid Account ID or 'exit' to quit.\n");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"\nError: Unable to connect to API at {apiUrl}");
        Console.WriteLine($"Details: {ex.Message}");
        Console.WriteLine("\nMake sure the API is running: dotnet run --project GUHC.HierarchySystem.Api");
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError: {ex.Message}\n");
    }
}

async Task DisplaySubtreeAsync(HttpClient httpClient, string baseUrl, int accountId)
{
    try
    {
        var response = await httpClient.GetFromJsonAsync<AccountTreeResponseDto>(
            $"{baseUrl}/api/accounts/{accountId}/tree");

        if (response == null)
        {
            Console.WriteLine($"\nAccount {accountId} not found.\n");
            return;
        }

        Console.WriteLine($"\nAccount Hierarchy Tree for ID {accountId}:");
        Console.WriteLine("=====================================\n");
        DisplayNode(response, prefix: "");
        Console.WriteLine(); // Add empty line after tree
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"\nError fetching account {accountId}: {ex.Message}\n");
    }
}

void DisplayNode(AccountTreeResponseDto node, string prefix)
{
    // Draw the current node with indentation
    var isRoot = prefix == "";
    var nodePrefix = isRoot ? "" : "├─ ";
    var depthIndicator = isRoot ? "" : $"[Depth: {node.Depth}] ";

    Console.WriteLine($"{prefix}{nodePrefix}{node.Name} {depthIndicator}(ID: {node.Id})");

    // Draw children
    if (node.Children.Count > 0)
    {
        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            var isLast = i == node.Children.Count - 1;
            var childPrefix = prefix + (isRoot ? "" : "│  ");
            var connector = isLast ? "└─ " : "├─ ";

            DisplayNodeRecursive(child, childPrefix, connector);
        }
    }
}

void DisplayNodeRecursive(AccountTreeResponseDto node, string prefix, string connector)
{
    var depthIndicator = $"[Depth: {node.Depth}] ";
    Console.WriteLine($"{prefix}{connector}{node.Name} {depthIndicator}(ID: {node.Id})");

    if (node.Children.Count > 0)
    {
        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            var isLast = i == node.Children.Count - 1;
            var childPrefix = prefix + (connector == "└─ " ? "   " : "│  ");
            var childConnector = isLast ? "└─ " : "├─ ";

            DisplayNodeRecursive(child, childPrefix, childConnector);
        }
    }
}