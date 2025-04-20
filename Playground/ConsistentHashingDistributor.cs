using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;

namespace ConsistentHashing;

public interface INode
{
    public string Name { get; }
}

public class Node(string Name) : INode
{
    public string Name { get; private set; } = Name;
}

public interface IConsistentHashingService
{
    void AddNode(INode node);
    INode GetNode(string key);
    void RemoveNode(INode node);
}

public class ConsistentHashingService(int virtualNodesCount = 2, HashAlgorithm? hashAlgorithm = null) : IConsistentHashingService
{
    private readonly HashSet<INode> nodes = [];
    private readonly SortedDictionary<ulong, INode> nodesRing = [];
    private readonly ConcurrentDictionary<ulong, INode> keysRing = new();

    private readonly int virtualNodesCount = virtualNodesCount;
    private readonly HashAlgorithm hashAlgorithm = hashAlgorithm ?? MD5.Create();

    public void AddNode(INode node)
    {
        if (nodes.Add(node))
        {
            for (int i = 0; i < virtualNodesCount; i++)
            {
                var hash = Hash($"{node.Name}-{i}");
                nodesRing[hash] = node;
                RebalanceKeysUponNodeInsertion(node, hash);
            }
        }
    }

    public void RemoveNode(INode node)
    {
        if (!nodes.Contains(node))
            return;

        var keysToRedistribute = keysRing.Where(x => x.Value == node).Select(x => x.Key);

        // Remove the node from the ring
        for (int i = 0; i < virtualNodesCount; i++)
        {
            var hash = Hash($"{node}-{i}");
            nodesRing.Remove(hash);
        }

        foreach (var hash in keysToRedistribute)
        {
            var newNode = GetNode(hash);
            if (!newNode!.Equals(default(INode)))
            {
                keysRing[hash] = newNode;
            }
        }
    }

    public INode? GetNode(string key)
    {
        if (nodesRing.Count == 0)
            return default;

        var hash = Hash(key);
        if (keysRing.TryGetValue(hash, out var node))
            return node;

        node = GetNode(hash);
        // Store the mapping of the key to the node
        keysRing[hash] = node!;
        return node;
    }

    private INode? GetNode(ulong hash)
    {
        if (nodesRing.Count == 0)
            return default;

        var nodePair = nodesRing.FirstOrDefault(kvp => kvp.Key >= hash);

        if (nodePair.Value is default(INode))
            nodePair = nodesRing.First();

        return nodePair.Value;
    }

    private void RebalanceKeysUponNodeInsertion(INode newNode, ulong nodeHash)
    {
        var keysToRebalance = new List<string>();
        var prevNode = nodesRing.LastOrDefault(kvp => kvp.Key < nodeHash);
        var wrapAround = false;
        if (prevNode.Value is default(INode))
        {
            prevNode = nodesRing.Last();
            wrapAround = true;
        }

        var prevNodeHash = prevNode.Key;
        
        if (wrapAround)
        {
            foreach (var nodeKeysPair in keysRing.Where(x => x.Key <= nodeHash || x.Key > prevNodeHash))
                keysRing[nodeKeysPair.Key] = newNode;
        }
        else
        {
            foreach (var nodeKeysPair in keysRing.Where(x => x.Key > prevNodeHash && x.Key <= nodeHash))
                keysRing[nodeKeysPair.Key] = newNode;
        }
    }

    private ulong Hash(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hashBytes = hashAlgorithm.ComputeHash(bytes);
        return BitConverter.ToUInt64(hashBytes, 0);
    }
}