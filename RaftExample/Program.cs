// See https://aka.ms/new-console-template for more information
using RaftElection;

Console.WriteLine("Hello, World!");

var nodes = new Election[5];
for (int i = 0; i < nodes.Length; i++)
{
  nodes[i] = new Election();
}

foreach (var node in nodes)
{
  var thread = new Thread(new ThreadStart(node.CheckState));
  thread.Start();
}