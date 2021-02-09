using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TezosNotifyBot.Model;
using TezosNotifyBot.Tezos;

namespace TezosNotifyBot.Nodes
{
    public class NodeManager
    {
        private readonly HttpClient _http;
        private readonly ILogger<Node> _logger;
        
        /// <summary>
        /// All nodes described in Settings/Nodes.json
        /// </summary>
        public Node[] Nodes { get; } = new Node[0];

        /// <summary>
        /// Currently active node. Changing using SwitchTo methods
        /// </summary>
        public Node Active { get; private set; }
        
        public NodeClient Client { get; private set; }

        public NodeManager(Node[] nodes, HttpClient http, ILogger<Node> logger)
        {
            _http = http;
            _logger = logger;
            
            Nodes = nodes;
            Active = Nodes[0];
        }

        public void SwitchTo(int index)
        {
            var changeTo = Nodes.ElementAtOrDefault(index);
            if (changeTo is null)
                throw new ArgumentException($"Node in {index} index doesn't exists");

            Active = changeTo;
            Client = new NodeClient(Active.Url, _http, _logger);
        }

        public string GetStatus(Node node)
        {
            var client = new NodeClient(node.Url, _http, _logger);
            try
            {
                var bh = client.GetBlockHeader("head");
                return $"{bh.level} ({bh.timestamp}) 🆗";
            }
            catch(Exception e)
            {
                return e.Message;
            }
        }
    }
}