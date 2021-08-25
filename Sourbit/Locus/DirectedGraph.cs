using System;
using System.Collections.Generic;

namespace Sourbit.Locus
{
    public class DirectedGraph
    {
        public Dictionary<int, Dictionary<int, float>> Connections { get; private set; }

        int nodes;
        readonly Dictionary<int, bool> disabled;

        public DirectedGraph()
        {
            nodes = 0;
            disabled = new Dictionary<int, bool>();

            Connections = new Dictionary<int, Dictionary<int, float>>();
        }

        public int Add()
        {
            var node = this.nodes++;
            Connections[node] = new Dictionary<int, float>();

            return node;
        }

        public void Remove(int node)
        {
            Connections.Remove(node);

            foreach (var connections in Connections.Values)
            {
                connections.Remove(node);
            }
        }

        public void Connect(int a, int b, float weight)
        {
            Connections[a][b] = weight;
        }

        public void Disconnect(int a, int b)
        {
            Connections[a].Remove(b);
        }

        public int Split(int a, int b)
        {
            var cost = Connections[a][b];
            // if (cost == null)
            // {
            //     throw new ArgumentException($"Nodes {a} and {b} aren't connected");
            // }

            var node = Add();
            Disconnect(a, b);
            Connect(a, node, cost / 2);
            Connect(node, b, cost / 2);

            return node;
        }

        public void Enable(int node)
        {
            disabled[node] = false;
        }

        public void Disable(int node)
        {
            disabled[node] = true;
        }

        public List<int> Find(int origin, int target)
        {
            // Start Open list
            var open = new List<int>();
            var closed = new Dictionary<int, bool>();
            var costs = new Dictionary<int, double>();
            var paths = new Dictionary<int, int>();

            open.Add(origin);
            costs[origin] = 0;

            // Core Loop
            // -> While open list is not empty
            var foundPath = false;
            while (open.Count > 0)
            {
                // Find smallest F (total cost) in open list
                var node = 0;
                var cost = double.MaxValue;
                var openIndex = 0;
                for (var i = 0; i < open.Count; ++i)
                {
                    var nodeIndex = open[i];
                    var nodeCost = costs[nodeIndex];

                    if (cost > nodeCost)
                    {
                        node = nodeIndex;
                        cost = nodeCost;
                        openIndex = i;
                    }
                }

                // Is it the target? Terminate search
                if (node == target)
                {
                    foundPath = true;
                    break;
                }

                // Remove node from open list
                open.RemoveAt(openIndex);

                // Close node
                closed[node] = true;

                // -> For each neighbor
                foreach (var kvp in Connections[node])
                {
                    var neighbor = kvp.Key;
                    var weight = kvp.Value;

                    // If neighbor is closed -> Skip
                    if (closed.ContainsKey(neighbor) && closed[neighbor])
                    {
                        continue;
                    }

                    // If neighbor is a block -> Skip
                    if (disabled.ContainsKey(neighbor) && disabled[neighbor])
                    {
                        continue;
                    }

                    // Calculate G (travel cost)
                    var travelCost = weight + costs[node];

                    // If neighbor is in open list and new G is lower -> Update path
                    var isOpen = false;
                    foreach (var openNode in open)
                    {
                        if (openNode == neighbor)
                        {
                            if (costs[neighbor] > travelCost)
                            {
                                costs[neighbor] = travelCost;
                                paths[neighbor] = node;
                            }
                            isOpen = true;
                            break;
                        }
                    }

                    // If neighbor is not in open list
                    if (!isOpen)
                    {
                        // Add neighbor to open list
                        open.Add(neighbor);
                        paths[neighbor] = node;

                        // Calculate neighbor G
                        costs[neighbor] = travelCost;
                    }
                }
            }

            // Backtrack
            var path = new List<int>();
            if (foundPath)
            {
                var node = target;
                while (paths.ContainsKey(node))
                {
                    path.Add(node);
                    node = paths[node];
                }
                path.Add(node);
                path.Reverse();
            }

            return path;
        }

        public int[] CreateGrid(int width, int height, float[] costs)
        {
            if (costs.Length != width * height)
            {
                throw new ArgumentException("Size of \"costs\" doesn't match \"width\" and \"height\".");
            }

            var nodes = new int[width * height];

            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var index = x + y * width;
                    var node = Add();
                    nodes[index] = node;

                    var neighbors = new List<int>();
                    if (x - 1 >= 0) neighbors.Add((x - 1) + y * width);
                    if (y - 1 >= 0) neighbors.Add(x + (y - 1) * width);

                    foreach (var neighbor in neighbors)
                    {
                        Connect(node, nodes[neighbor], costs[neighbor]);
                        Connect(nodes[neighbor], node, costs[index]);
                    }
                }
            }

            for (var i = 0; i < nodes.Length; ++i)
            {
                if (costs[i] == 0)
                {
                    Disable(nodes[i]);
                }
            }

            return nodes;
        }
    }
}
