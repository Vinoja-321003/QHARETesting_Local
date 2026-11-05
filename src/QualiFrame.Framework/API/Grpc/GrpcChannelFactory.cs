using System;
using Grpc.Net.Client;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.API.Grpc
{
    /// <summary>
    /// Opt-in gRPC channel factory. No generated stubs ship in this repository;
    /// callers pass their own protobuf clients over the returned channel.
    /// </summary>
    public static class GrpcChannelFactory
    {
        public static bool IsConfigured(string? address = null)
        {
            return !string.IsNullOrWhiteSpace(address ?? TestConfiguration.Instance.GrpcAddress);
        }

        public static GrpcChannel CreateChannel(string? address = null)
        {
            var target = address ?? TestConfiguration.Instance.GrpcAddress;
            if (string.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("gRPC address is not configured. Set APITesting:Grpc:Address.");
            }

            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"Invalid gRPC address: {target}");
            }

            Logger.Information("Creating gRPC channel for {Address}", target);
            return GrpcChannel.ForAddress(target);
        }
    }
}
