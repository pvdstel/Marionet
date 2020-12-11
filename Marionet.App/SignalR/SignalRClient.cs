using Marionet.App.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Marionet.App.SignalR
{
    public abstract class SignalRClient<T>
        where T : new()
    {

        private const int ConnectionFailureCooldownMs = 30000;

        private readonly string name;
        private readonly Func<IAsyncEnumerable<Uri>> getUris;
        private readonly ConfigurationService configurationService;
        private readonly ILogger<SignalRClient<T>> logger;
        private bool retry = false;

        private CancellationToken connectCancellationToken = default;
        private HubConnection? currentConnection;

        public SignalRClient(
            string name,
            Func<IAsyncEnumerable<Uri>> getUris,
            ConfigurationService configurationService,
            ILogger<SignalRClient<T>> logger)
        {
            this.name = name;
            this.getUris = getUris ?? throw new ArgumentNullException(nameof(getUris));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public T? Hub { get; private set; }

        public event EventHandler? ConnectingStarted;
        public event EventHandler? Connected;
        public event EventHandler? Disconnected;

        public async Task Connect(CancellationToken cancellationToken)
        {
            retry = true;
            connectCancellationToken = cancellationToken;
            ConnectingStarted?.Invoke(this, new EventArgs());
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var uris = getUris();
                    await foreach (var uri in uris)
                    {
                        var (connection, hub) = GetHubConnection(uri);
                        currentConnection = connection;
                        Hub = hub;

                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var cancelRegistration = cancellationToken.Register(async () =>
                            {
                                if (connection.State == HubConnectionState.Connected)
                                {
                                    await Disconnect();
                                }
                            });

                            async Task onConnectionClosed(Exception e)
                            {
                                logger.LogWarning($"Connection to {name}@{uri} closed");
                                await cancelRegistration.DisposeAsync();
                                await connection.DisposeAsync();

                                Disconnected?.Invoke(this, new EventArgs());
                                if (retry && !connectCancellationToken.IsCancellationRequested)
                                {
                                    logger.LogDebug($"Reconnecting to {name}...");
                                    await Connect(connectCancellationToken);
                                }
                                else
                                {
                                    logger.LogDebug($"Not reconnecting to {name}");
                                }
                            }
                            connection.Closed += onConnectionClosed;


                            logger.LogInformation($"Connecting to {name}@{uri}...");
                            await connection.StartAsync(cancellationToken);
                            logger.LogInformation($"Connected to {name}@{uri}");
                            Connected?.Invoke(this, new EventArgs());
                            return;
                        }
                        catch (HttpRequestException)
                        {
                            logger.LogWarning($"Connection to {name}@{uri} failed");
                        }
                    }
                    logger.LogWarning($"All connection attempts to {name} failed. Waiting {ConnectionFailureCooldownMs} ms before retrying");
                    await Task.Delay(ConnectionFailureCooldownMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug($"Connection attempt to {name} aborted");
                }
            }
        }

        public async Task Disconnect()
        {
            retry = false;
            if (currentConnection != null)
            {
                await currentConnection.StopAsync();
            }
        }

        private (HubConnection, T) GetHubConnection(Uri uri)
        {
            var connection = new HubConnectionBuilder()
                   //.AddMessagePackProtocol()
                   .WithUrl(uri, options =>
                   {
                       options.WebSocketConfiguration = o =>
                       {
                           o.ClientCertificates.Add(configurationService.CertificateManagement.ClientCertificate);
                           o.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                           {
                               if (certificate is X509Certificate2 certificate2)
                               {
                                   return configurationService.CertificateManagement.IsValidClientCertificate(certificate2);
                               }
                               else
                               {
                                   throw new ArgumentException("The argument " + nameof(certificate) + "must be of type " + nameof(X509Certificate2) + ".");
                               }
                           };
                       };
                       options.ClientCertificates.Add(configurationService.CertificateManagement.ClientCertificate);
                       options.HttpMessageHandlerFactory = message =>
                       {
                           if (message is HttpClientHandler clientHandler)
                           {
                               clientHandler.ServerCertificateCustomValidationCallback = (m, certificate, chain, policyErrors) => certificate != null && configurationService.CertificateManagement.IsValidServerCertificate(certificate);
                               clientHandler.ClientCertificates.Add(configurationService.CertificateManagement.ClientCertificate);
                           }
                           return message;
                       };
                   }
                   ).Build();
            var hub = CreateHubInterface(connection);
            RegisterAll(connection);

            return (connection, hub);
        }

        private static T CreateHubInterface(HubConnection connection)
        {
            T result = new T();
            HubDelegateFactory factory = new HubDelegateFactory(connection);

            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                field.SetValue(result, factory.CreateDelegate(field.FieldType, field.Name));
            }

            return result;
        }


        private void RegisterAll(HubConnection connection)
        {
            foreach (MethodInfo method in GetType().GetMethods())
            {
                if (method.GetCustomAttribute<HubCallableAttribute>(true) != null)
                {
                    Type[] parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                    IDisposable subscription = connection.On(method.Name, parameterTypes, (parameters) =>
                    {
                        return method.Invoke(this, parameters) as Task;
                    });
                }
            }
        }
    }
}
