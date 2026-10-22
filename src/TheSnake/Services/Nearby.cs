namespace TheSnake.Services;

/// <summary>Peer to peer links to phones in the same room (Google Play services Nearby Connections).</summary>
public interface INearbyService
{
    bool IsSupported { get; }

    /// <summary>A phone hosting a battle was found while browsing.</summary>
    event EventHandler<(string EndpointId, string Name)>? HostFound;

    event EventHandler<string>? HostLost;

    /// <summary>A link is ready (on the host: a player joined; on a player: connected to the host).</summary>
    event EventHandler<(string EndpointId, string Name)>? Connected;

    event EventHandler<string>? Disconnected;

    event EventHandler<(string EndpointId, byte[] Data)>? Received;

    /// <summary>Asks for the Bluetooth, Wi Fi and nearby device permissions. Returns true when all are granted.</summary>
    Task<bool> EnsurePermissionsAsync();

    Task<bool> StartHostingAsync(string name);

    Task<bool> StartBrowsingAsync();

    Task<bool> JoinAsync(string endpointId, string name);

    void Send(string endpointId, byte[] data);

    void SendToAll(IReadOnlyList<string> endpointIds, byte[] data);

    /// <summary>Closes one link.</summary>
    void Disconnect(string endpointId);

    /// <summary>Stops advertising and discovery but keeps links open.</summary>
    void StopLooking();

    /// <summary>Closes everything.</summary>
    void Stop();
}

public sealed class NoNearbyService : INearbyService
{
    public bool IsSupported => false;

    public event EventHandler<(string EndpointId, string Name)>? HostFound { add { } remove { } }

    public event EventHandler<string>? HostLost { add { } remove { } }

    public event EventHandler<(string EndpointId, string Name)>? Connected { add { } remove { } }

    public event EventHandler<string>? Disconnected { add { } remove { } }

    public event EventHandler<(string EndpointId, byte[] Data)>? Received { add { } remove { } }

    public Task<bool> EnsurePermissionsAsync() => Task.FromResult(false);

    public Task<bool> StartHostingAsync(string name) => Task.FromResult(false);

    public Task<bool> StartBrowsingAsync() => Task.FromResult(false);

    public Task<bool> JoinAsync(string endpointId, string name) => Task.FromResult(false);

    public void Send(string endpointId, byte[] data)
    {
    }

    public void SendToAll(IReadOnlyList<string> endpointIds, byte[] data)
    {
    }

    public void Disconnect(string endpointId)
    {
    }

    public void StopLooking()
    {
    }

    public void Stop()
    {
    }
}
