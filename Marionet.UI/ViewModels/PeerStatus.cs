namespace Marionet.UI.ViewModels
{
    public class PeerStatus
    {
        public PeerStatus(string name, bool isClient, bool isServer)
        {
            Name = name;
            IsClient = isClient;
            IsServer = isServer;
        }

        public string Name { get; }

        public bool IsClient { get; }
    
        public bool IsServer { get; }
    }
}
