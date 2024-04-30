using System;
using System.Net.Sockets;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Export;
using System.Collections.Generic;
using System.IO;
using static System.Collections.Specialized.BitVector32;

public class BrowseEndpoints
{  // TURN THIS INTO WINDOWS FORM APP
    public static void Main(string[] args)
    {
     
        var config = new ApplicationConfiguration()
        {
            ApplicationType = ApplicationType.Client,
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            TraceConfiguration = new TraceConfiguration()
        };
        try
        {
            Console.Write("Url:");
            var URL = Console.ReadLine();

            Console.Write("Port:");
            var PORT = Console.ReadLine();
            ITransportChannel channel = DiscoveryChannel.Create(new Uri($"opc.tcp://{URL}:{PORT}/discovery"), EndpointConfiguration.Create(), new ServiceMessageContext());
            var discoveryClient = new DiscoveryClient(channel);
            var endpointDescriptions = discoveryClient.GetEndpoints(null);
            foreach (var endpointDescription in endpointDescriptions)
            {
                Console.WriteLine("Server URI: {0}", endpointDescription.EndpointUrl);
                Console.WriteLine("Security Mode: {0}", endpointDescription.SecurityMode);
                Console.WriteLine("Security Policy URI: {0}", endpointDescription.SecurityPolicyUri);
                Console.WriteLine("Transport Profile URI: {0}", endpointDescription.TransportProfileUri);
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointDescription.EndpointUrl, false, 15000);
                Session session = Session.Create(configuration: config, new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(config)), false, "", 60000, null, null).GetAwaiter().GetResult();
                BrowseAll(session, ObjectIds.ObjectsFolder);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during endpoint discovery: {0}", ex.Message);
        }
    }
    public static void BrowseAll(Session session, NodeId nodeId, int depth = 0)
    {
        ReferenceDescriptionCollection references;
        byte[] continuationPoint = null;


            do
            {
                session.Browse(
                    null,
                    null,
                    nodeId,
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    nodeClassMask: (uint)(NodeClass.Variable | NodeClass.Object | NodeClass.Method),
                    out continuationPoint,
                    out references);
                foreach (var reference in references)
                {
                References referencesList = new References { referenceDescriptions = references };
                referencesList.references = referencesList;
                    StreamWriter writer = new StreamWriter("NodeList.txt", true);
                    writer.WriteLine($"{new string('\t', depth)}{"->"}{reference.BrowseName.Name}{" NodeId= "}{reference.NodeId}");
                    Console.WriteLine($"{new string('\t', depth)}{"->"}{reference.BrowseName.Name}{" NodeId= "}{reference.NodeId}");
                    Thread.Sleep(100);
                    if (reference.NodeClass.HasFlag(NodeClass.Object) || reference.NodeClass.HasFlag(NodeClass.Variable) || reference.NodeClass.HasFlag(NodeClass.Method))
                        {
                        writer.Dispose();
                    BrowseAll(session, (NodeId)reference.NodeId, depth + 1);
                        }
                    }
               
        } while (continuationPoint != null && continuationPoint.Length > 0);
    }

    public class References
    {
        public ReferenceDescriptionCollection? referenceDescriptions { get; set; }
        public References? references { get; set; }
    }
}
