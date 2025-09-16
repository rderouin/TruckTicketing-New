using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Renci.SshNet;

using SE.BillingService.Contracts.Api.Enums;
using SE.BillingService.Domain.InvoiceDelivery.Context;
using SE.BillingService.Domain.InvoiceDelivery.Encoders;
using SE.Shared.Common.Extensions;

using Trident.Logging;

namespace SE.BillingService.Domain.InvoiceDelivery.Transport;

public class SftpInvoiceDeliveryTransport : IInvoiceDeliveryTransport
{
    private readonly ILog _logger;

    public SftpInvoiceDeliveryTransport(ILog logger)
    {
        _logger = logger;
    }

    public InvoiceDeliveryTransportType TransportType => InvoiceDeliveryTransportType.Sftp;

    public async Task Send(EncodedInvoicePart part, InvoiceDeliveryTransportInstructions instructions)
    {
        // check if the SFTP server is configured
        var uri = instructions.DestinationUri;
        if (uri?.Scheme != "sftp")
        {
            throw new InvalidOperationException("The SFTP transport is not correctly configured.");
        }

        // configure authentication methods
        var authenticationMethods = new List<AuthenticationMethod>();
        var canonicalUsername = default(string);

        if (instructions.ClientId.HasText() &&
            instructions.ClientSecret.HasText())
        {
            authenticationMethods.Add(new PasswordAuthenticationMethod(instructions.ClientId, instructions.ClientSecret));
            canonicalUsername = instructions.ClientId;
        }

        if (instructions.PrivateKey != null)
        {
            throw new NotSupportedException("Private key is not supported.");
        }

        if (!canonicalUsername.HasText())
        {
            throw new InvalidOperationException("SFTP: Authentication method must be specified.");
        }

        // prep inputs
        var fullPath = uri.LocalPath == "/" ? $"/{Guid.NewGuid():N}" : uri.LocalPath;

        // configure the connection
        var connection = new ConnectionInfo(uri.Host, uri.Port > 0 ? uri.Port : 22, canonicalUsername, authenticationMethods.ToArray());
        using var client = new SftpClient(connection);

        // do it
        await UploadFile(client, part.DataStream, fullPath);
    }

    protected virtual async Task UploadFile(SftpClient client, Stream stream, string fullPath)
    {
        try
        {
            client.Connect();
            await Task.Factory.FromAsync(client.BeginUploadFile(stream, fullPath), client.EndUploadFile);
        }
        catch (Exception x)
        {
            _logger.Error(exception: x);
            throw new InvoiceDeliveryException(x.Message, x)
            {
                AdditionalMessage = "Unable to upload a file. Check the SFTP configuration and ensure the remote server is accessible.",
            };
        }
        finally
        {
            client.Disconnect();
        }
    }
}
