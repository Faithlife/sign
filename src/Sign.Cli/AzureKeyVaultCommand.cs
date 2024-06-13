// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;
using Sign.Core;
using Sign.SignatureProviders.KeyVault;

namespace Sign.Cli
{
    internal sealed class AzureKeyVaultCommand : Command
    {
        private readonly CodeCommand _codeCommand;

        internal Option<string> CertificateOption { get; } = new(["-kvc", "--azure-key-vault-certificate"], AzureKeyVaultResources.CertificateOptionDescription);
        internal Option<string?> ClientIdOption { get; } = new(["-kvi", "--azure-key-vault-client-id"], AzureKeyVaultResources.ClientIdOptionDescription);
        internal Option<string?> ClientSecretOption { get; } = new(["-kvs", "--azure-key-vault-client-secret"], AzureKeyVaultResources.ClientSecretOptionDescription);
        internal Option<bool> ManagedIdentityOption { get; } = new(["-kvm", "--azure-key-vault-managed-identity"], getDefaultValue: () => false, AzureKeyVaultResources.ManagedIdentityOptionDescription);
        internal Option<string?> TenantIdOption { get; } = new(["-kvt", "--azure-key-vault-tenant-id"], AzureKeyVaultResources.TenantIdOptionDescription);
        internal Option<Uri> UrlOption { get; } = new(["-kvu", "--azure-key-vault-url"], AzureKeyVaultResources.UrlOptionDescription);

        internal Argument<string?> FileArgument { get; } = new("file(s)", Resources.FilesArgumentDescription);

        internal AzureKeyVaultCommand(CodeCommand codeCommand, IServiceProviderFactory serviceProviderFactory)
            : base("azure-key-vault", AzureKeyVaultResources.CommandDescription)
        {
            ArgumentNullException.ThrowIfNull(codeCommand, nameof(codeCommand));
            ArgumentNullException.ThrowIfNull(serviceProviderFactory, nameof(serviceProviderFactory));

            _codeCommand = codeCommand;

            CertificateOption.IsRequired = true;
            UrlOption.IsRequired = true;

            ManagedIdentityOption.SetDefaultValue(false);

            AddOption(UrlOption);
            AddOption(TenantIdOption);
            AddOption(ClientIdOption);
            AddOption(ClientSecretOption);
            AddOption(CertificateOption);
            AddOption(ManagedIdentityOption);

            AddArgument(FileArgument);

            this.SetHandler(async (InvocationContext context) =>
            {
                string? fileArgument = context.ParseResult.GetValueForArgument(FileArgument);

                if (string.IsNullOrEmpty(fileArgument))
                {
                    context.Console.Error.WriteLine(Resources.MissingFileValue);
                    context.ExitCode = ExitCode.InvalidOptions;
                    return;
                }

                // this check exists as a courtesy to users who may have been signing .clickonce files via the old workaround.
                // at some point we should remove this check, probably once we hit v1.0
                if (fileArgument.EndsWith(".clickonce", StringComparison.OrdinalIgnoreCase))
                {
                    context.Console.Error.WriteLine(AzureKeyVaultResources.ClickOnceExtensionNotSupported);
                    context.ExitCode = ExitCode.InvalidOptions;
                    return;
                }

                // Some of the options are required and that is why we can safely use
                // the null-forgiving operator (!) to simplify the code.
                Uri url = context.ParseResult.GetValueForOption(UrlOption)!;
                string? tenantId = context.ParseResult.GetValueForOption(TenantIdOption);
                string? clientId = context.ParseResult.GetValueForOption(ClientIdOption);
                string? secret = context.ParseResult.GetValueForOption(ClientSecretOption);
                string certificateId = context.ParseResult.GetValueForOption(CertificateOption)!;
                bool useManagedIdentity = context.ParseResult.GetValueForOption(ManagedIdentityOption);

                TokenCredential? credential = null;

                if (useManagedIdentity)
                {
                    credential = new DefaultAzureCredential();
                }
                else
                {
                    if (string.IsNullOrEmpty(tenantId) ||
                        string.IsNullOrEmpty(clientId) ||
                        string.IsNullOrEmpty(secret))
                    {
                        context.Console.Error.WriteFormattedLine(
                            AzureKeyVaultResources.InvalidClientSecretCredential,
                            TenantIdOption,
                            ClientIdOption,
                            ClientSecretOption);
                        context.ExitCode = ExitCode.NoInputsFound;
                        return;
                    }

                    credential = new ClientSecretCredential(tenantId, clientId, secret);
                }

                KeyVaultServiceProvider keyVaultServiceProvider = new(credential, url, certificateId);
                await _codeCommand.HandleAsync(context, serviceProviderFactory, keyVaultServiceProvider, fileArgument);
            });
        }
    }
}
