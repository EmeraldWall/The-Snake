using Android.Gms.Common.Apis;
using Android.Gms.Extensions;
using Android.Gms.Nearby;
using Android.Gms.Nearby.Connection;
using Android.OS;
using TheSnake.Services;

namespace TheSnake.Platforms.Android;

/// <summary>Runtime permissions Nearby Connections needs, by Android version (Google's set up guide).</summary>
public sealed class NearbyPermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
    {
        get
        {
            var list = new List<(string, bool)>();
            int sdk = (int)Build.VERSION.SdkInt;
            if (sdk >= 31)
            {
                list.Add((global::Android.Manifest.Permission.BluetoothAdvertise, true));
                list.Add((global::Android.Manifest.Permission.BluetoothConnect, true));
                list.Add((global::Android.Manifest.Permission.BluetoothScan, true));
            }

            if (sdk >= 33)
            {
                list.Add((global::Android.Manifest.Permission.NearbyWifiDevices, true));
            }

            if (sdk is >= 29 and <= 31)
            {
                list.Add((global::Android.Manifest.Permission.AccessFineLocation, true));
            }
            else if (sdk <= 28)
            {
                list.Add((global::Android.Manifest.Permission.AccessCoarseLocation, true));
            }

            return [.. list];
        }
    }
}

/// <summary>
/// Nearby Connections with the P2P_STAR strategy: one host in the middle, up to three players around it.
/// Works over Bluetooth and Wi Fi with no internet.
/// </summary>
public sealed class NearbyConnectionsService : INearbyService
{
    private const string ServiceId = "com.vishvplus.thesnakegame.battle";

    private readonly Dictionary<string, string> _pendingNames = [];
    private IConnectionsClient? _client;
    private Lifecycle? _lifecycle;
    private Payloads? _payloads;

    public bool IsSupported => true;

    public event EventHandler<(string EndpointId, string Name)>? HostFound;

    public event EventHandler<string>? HostLost;

    public event EventHandler<(string EndpointId, string Name)>? Connected;

    public event EventHandler<string>? Disconnected;

    public event EventHandler<(string EndpointId, byte[] Data)>? Received;

    private IConnectionsClient Client => _client ??= NearbyClass.GetConnectionsClient(Platform.AppContext);

    private Lifecycle LifecycleCallback => _lifecycle ??= new Lifecycle(this);

    private Payloads PayloadCallback => _payloads ??= new Payloads(this);

    public async Task<bool> EnsurePermissionsAsync()
    {
        var status = await Permissions.CheckStatusAsync<NearbyPermissions>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<NearbyPermissions>();
        }

        return status == PermissionStatus.Granted;
    }

    public async Task<bool> StartHostingAsync(string name)
    {
        try
        {
            var options = new AdvertisingOptions.Builder().SetStrategy(Strategy.P2pStar).Build();
            await Client.StartAdvertising(name, ServiceId, LifecycleCallback, options).AsAsync();
            return true;
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or ApiException)
        {
            return false;
        }
    }

    public async Task<bool> StartBrowsingAsync()
    {
        try
        {
            var options = new DiscoveryOptions.Builder().SetStrategy(Strategy.P2pStar).Build();
            await Client.StartDiscovery(ServiceId, new Discovery(this), options).AsAsync();
            return true;
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or ApiException)
        {
            return false;
        }
    }

    public async Task<bool> JoinAsync(string endpointId, string name)
    {
        try
        {
            await Client.RequestConnection(name, endpointId, LifecycleCallback).AsAsync();
            return true;
        }
        catch (Exception ex) when (ex is Java.Lang.Exception or ApiException)
        {
            return false;
        }
    }

    public void Send(string endpointId, byte[] data)
    {
        try
        {
            Client.SendPayload(endpointId, Payload.FromBytes(data));
        }
        catch (Java.Lang.Exception)
        {
        }
    }

    public void SendToAll(IReadOnlyList<string> endpointIds, byte[] data)
    {
        if (endpointIds.Count == 0)
        {
            return;
        }

        try
        {
            Client.SendPayload([.. endpointIds], Payload.FromBytes(data));
        }
        catch (Java.Lang.Exception)
        {
        }
    }

    public void Disconnect(string endpointId)
    {
        _client?.DisconnectFromEndpoint(endpointId);
        _pendingNames.Remove(endpointId);
    }

    public void StopLooking()
    {
        _client?.StopAdvertising();
        _client?.StopDiscovery();
    }

    public void Stop()
    {
        StopLooking();
        _client?.StopAllEndpoints();
        _pendingNames.Clear();
    }

    private void OnMain(Action action) => MainThread.BeginInvokeOnMainThread(action);

    private sealed class Lifecycle(NearbyConnectionsService owner) : ConnectionLifecycleCallback
    {
        public override void OnConnectionInitiated(string p0, ConnectionInfo p1)
        {
            // Both sides accept automatically. Players see who joined in the lobby before the host starts.
            owner._pendingNames[p0] = p1.EndpointName ?? "Player";
            owner.Client.AcceptConnection(p0, owner.PayloadCallback);
        }

        public override void OnConnectionResult(string p0, ConnectionResolution p1)
        {
            string name = owner._pendingNames.GetValueOrDefault(p0, "Player");
            if (p1.Status?.IsSuccess ?? false)
            {
                owner.OnMain(() => owner.Connected?.Invoke(owner, (p0, name)));
            }
            else
            {
                owner._pendingNames.Remove(p0);
            }
        }

        public override void OnDisconnected(string p0)
        {
            owner._pendingNames.Remove(p0);
            owner.OnMain(() => owner.Disconnected?.Invoke(owner, p0));
        }
    }

    private sealed class Discovery(NearbyConnectionsService owner) : EndpointDiscoveryCallback
    {
        public override void OnEndpointFound(string p0, DiscoveredEndpointInfo p1) =>
            owner.OnMain(() => owner.HostFound?.Invoke(owner, (p0, p1.EndpointName ?? "Host")));

        public override void OnEndpointLost(string p0) => owner.OnMain(() => owner.HostLost?.Invoke(owner, p0));
    }

    private sealed class Payloads(NearbyConnectionsService owner) : PayloadCallback
    {
        public override void OnPayloadReceived(string p0, Payload p1)
        {
            if (p1.AsBytes() is { } bytes)
            {
                owner.OnMain(() => owner.Received?.Invoke(owner, (p0, bytes)));
            }
        }

        public override void OnPayloadTransferUpdate(string p0, PayloadTransferUpdate p1)
        {
        }
    }
}
