using ConsistentHashing;

var service = new ConsistentHashingService();
service.AddNode(new Node ("Server1"));

var node = service.GetNode("testKey");
Console.WriteLine(node.Name);
service.AddNode(new Node ("Server2"));
service.AddNode(new Node ("Server3"));
service.AddNode(new Node ("Server4"));
service.AddNode(new Node ("Server5"));
service.AddNode(new Node ("Server6"));
service.AddNode(new Node ("Server7"));
service.AddNode(new Node ("Server8"));
service.AddNode(new Node ("Server9"));
service.AddNode(new Node ("Server10"));
service.AddNode(new Node ("Server11"));

node = service.GetNode("testKey");

Console.WriteLine(node.Name);