using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XrayUI.Services;

internal static class NetworkInterfaceSelector
{
    public static IReadOnlyList<string> GetEligiblePhysicalInterfaceNames()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(IsEligiblePhysicalInterface)
                .OrderByDescending(HasIPv4Gateway)
                .ThenByDescending(ni => ni.Speed)
                .ThenBy(ni => ni.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ni => ni.Name)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static bool IsExcludedInterface(NetworkInterface networkInterface)
    {
        var name = networkInterface.Name;
        var description = networkInterface.Description;

        return networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback
            or NetworkInterfaceType.Tunnel
            || ContainsAny(name, "tailscale", "tunnel", "wintun", "wireguard", "vpn")
            || ContainsAny(description, "tailscale", "tunnel", "wintun", "wireguard", "vpn");
    }

    public static bool IsEligiblePhysicalInterface(NetworkInterface networkInterface)
    {
        if (networkInterface.OperationalStatus != OperationalStatus.Up
            || IsExcludedInterface(networkInterface))
        {
            return false;
        }

        var ipProperties = networkInterface.GetIPProperties();
        return ipProperties.GatewayAddresses.Any(gateway =>
            !gateway.Address.Equals(IPAddress.Any)
            && !gateway.Address.Equals(IPAddress.IPv6Any)
            && !IPAddress.IsLoopback(gateway.Address)
            && (!gateway.Address.IsIPv6LinkLocal || HasGlobalIPv6Address(ipProperties)));
    }

    private static bool HasGlobalIPv6Address(IPInterfaceProperties ipProperties) =>
        ipProperties.UnicastAddresses.Any(address =>
            address.Address.AddressFamily == AddressFamily.InterNetworkV6
            && !address.Address.IsIPv6LinkLocal
            && !IPAddress.IsLoopback(address.Address));

    internal static bool HasIPv4Gateway(NetworkInterface networkInterface) =>
        networkInterface.GetIPProperties().GatewayAddresses.Any(gateway =>
            gateway.Address.AddressFamily == AddressFamily.InterNetwork
            && !gateway.Address.Equals(IPAddress.Any));

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}