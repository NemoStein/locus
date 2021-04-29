using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sourbit.Locus
{
    public class SquareGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float[] Costs { get; private set; }

        public SquareGrid(int width, int height, float[] costs)
        {
            if (costs.Length != width * height)
            {
                throw new ArgumentException("Size of \"costs\" doesn't match \"width\" and \"height\".");
            }

            Width = width;
            Height = height;
            Costs = costs;
        }

        public List<int> Find(Vector2Int origin, Vector2Int target, Corners corners)
        {
            // Get origin & target Nodes
            var originIndex = PositionToIndex(origin.x, origin.y);
            var targetIndex = PositionToIndex(target.x, target.y);

            // Start Open list
            var open = new List<int>();

            var size = Costs.Length;
            var closed = new bool[size];
            var heuristics = new int[size];
            var costs = new double[size];
            var estimateCosts = new double[size];
            var paths = new int[size];

            open.Add(originIndex);
            costs[originIndex] = 0;
            estimateCosts[originIndex] = 0;
            paths[originIndex] = -1;

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
                    var nodeCost = estimateCosts[nodeIndex];

                    if (cost > nodeCost)
                    {
                        node = nodeIndex;
                        cost = nodeCost;
                        openIndex = i;
                    }
                }

                // Is it the target? Terminate search
                if (node == targetIndex)
                {
                    foundPath = true;
                    break;
                }

                // Remove node from open list
                open.RemoveAt(openIndex);

                // Close node
                closed[node] = true;

                // -> For each neighbor
                (int nodeX, int nodeY) = IndexToPosition(node);
                for (var x = nodeX - 1; x <= nodeX + 1; ++x)
                {
                    // Is x out-of-bounds?
                    if (x < 0 || x >= Width)
                    {
                        continue;
                    }

                    for (var y = nodeY - 1; y <= nodeY + 1; ++y)
                    {
                        // Is y out-of-bounds?
                        if (y < 0 || y >= Height)
                        {
                            continue;
                        }

                        // Ignore itself
                        if (x == nodeX && y == nodeY)
                        {
                            continue;
                        }

                        // If diagonals is not allowed -> Skip diagonal neighbor
                        if (corners == Corners.NONE && x != nodeX && y != nodeY)
                        {
                            continue;
                        }

                        var neighbor = PositionToIndex(x, y);

                        // If neighbor is closed -> Skip
                        if (closed[neighbor])
                        {
                            continue;
                        }

                        // If neighbor is a block -> Skip
                        if (Costs[neighbor] == 0)
                        {
                            continue;
                        }

                        // Check corners
                        if (corners < Corners.PHASE)
                        {
                            var blockL = IsBlock(nodeX - 1, nodeY);
                            var blockR = IsBlock(nodeX + 1, nodeY);
                            var blockU = IsBlock(nodeX, nodeY - 1);
                            var blockD = IsBlock(nodeX, nodeY + 1);
                            var none = corners < Corners.CUT;

                            if ((
                              (x < nodeX && blockL && (none || (y < nodeY && blockU) || (y > nodeY && blockD))) ||
                              (x > nodeX && blockR && (none || (y < nodeY && blockU) || (y > nodeY && blockD))) ||
                              (y < nodeY && blockU && (none || (x < nodeX && blockL) || (x > nodeX && blockR))) ||
                              (y > nodeY && blockD && (none || (x < nodeX && blockL) || (x > nodeX && blockR)))
                            ))
                            {
                                continue;
                            }
                        }

                        // Calculate G (travel cost)
                        var travelCost = Costs[neighbor] * (x != nodeX && y != nodeY ? 1.4 : 1) + costs[node];

                        // If neighbor is in open list and new G is lower -> Update path
                        var isOpen = false;
                        foreach (int openNode in open)
                        {
                            if (openNode == neighbor)
                            {
                                if (costs[neighbor] > travelCost)
                                {
                                    costs[neighbor] = travelCost;
                                    paths[neighbor] = node;
                                    estimateCosts[neighbor] = heuristics[neighbor] + travelCost;
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

                            // Calculate neighbor H (Heuristic), G and F
                            var heuristic = Mathf.Abs(x - target.x) + Mathf.Abs(y - target.y);
                            heuristics[neighbor] = heuristic;
                            costs[neighbor] = travelCost;
                            estimateCosts[neighbor] = heuristic + travelCost;
                        }
                    }
                }
            }

            // Backtrack
            var path = new List<int>();
            if (foundPath)
            {
                var node = targetIndex;
                while (paths[node] != -1)
                {
                    path.Add(node);
                    node = paths[node];
                }
                path.Add(node);
                path.Reverse();
            }

            return path;
        }

        bool IsBlock(int x, int y)
        {
            return x < 0 || x >= Width || y < 0 || y >= Height || Costs[PositionToIndex(x, y)] == 0;
        }

        int PositionToIndex(int x, int y)
        {
            return x + y * Width;
        }

        (int, int) IndexToPosition(int i)
        {
            return (i % Width, i / Width);
        }
    }

    public enum Corners
    {
        /// <summary>Allow straight paths only (no diagonals)</summary>
        NONE,
        /// <summary>Allow diagonal paths, but must walk around corners</summary>
        WALK,
        /// <summary>Allow cutting corners</summary>
        CUT,
        /// <summary>Allow phasing between meeting corners</summary>
        PHASE,
    }
}
