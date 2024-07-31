using System;
using System.Collections;
using System.IO;
using System.Xml.Linq;

if (args.Length < 1)
{
    Console.WriteLine("Please provide at least one file path.");
    return;
}

Queue<string> pathsToExplore = new Queue<string>();
foreach (string arg in args)
{
    pathsToExplore.Enqueue(arg);
}

Dictionary<string, LinkedList<string>> dependencyGraph = new Dictionary<string, LinkedList<string>>();

string filePath;
string currentWorkingDir = Directory.GetCurrentDirectory();
while (pathsToExplore.TryDequeue(out filePath))
{
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"The file {filePath} does not exist.");
        continue;
    }

    string relativeFilePath = Path.GetRelativePath(currentWorkingDir, filePath);
    using (StreamReader reader = new StreamReader(filePath))
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            const string includeToken = "template:";
            if (line.Contains(includeToken))
            {
                string includedRelativePath = line.Substring(line.IndexOf(includeToken) + includeToken.Length).Split("#")[0].Trim();
                string? includedAbsolutePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filePath), includedRelativePath));
                string includedPathRelativeToCwd = File.Exists(includedAbsolutePath) ? Path.GetRelativePath(currentWorkingDir, includedAbsolutePath) : includedRelativePath;

                // If we haven't discovered the included path, parse it next
                if (!dependencyGraph.ContainsKey(includedPathRelativeToCwd))
                {
                    pathsToExplore.Enqueue(includedAbsolutePath);
                }

                // If we haven't created a list for the current path
                if (!dependencyGraph.ContainsKey(relativeFilePath))
                {
                    dependencyGraph[relativeFilePath] = new LinkedList<string>();
                }

                dependencyGraph[relativeFilePath].AddLast(includedPathRelativeToCwd);
            }
        }
    }
}

XNamespace ns = "http://schemas.microsoft.com/vs/2009/dgml";
XElement root = new XElement(ns + "DirectedGraph");
XElement nodes = new XElement(ns + "Nodes");
XElement links = new XElement(ns + "Links");

foreach (var node in dependencyGraph)
{
    nodes.Add(new XElement(ns + "Node", new XAttribute("Id", node.Key), new XAttribute("Label", node.Key)));
    foreach (var child in node.Value)
    {
        links.Add(new XElement(ns + "Link", new XAttribute("Source", node.Key), new XAttribute("Target", child)));
    }
}

root.Add(nodes);
root.Add(links);
XDocument doc = new XDocument(root);
doc.Save("graph.dgml");

Console.WriteLine("DGML file generated successfully.");
