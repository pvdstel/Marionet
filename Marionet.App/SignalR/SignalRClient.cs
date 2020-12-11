using Marionet.App.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly HubConnection connection;
        private readonly Uri uri;
        private readonly ILogger<SignalRClient<T>> logger;
        private bool retry = false;
        private CancellationToken connectCancellationToken = default;

        public SignalRClient(
            Uri uri,
            ConfigurationService configurationService,
            ILogger<SignalRClient<T>> logger)
        {
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
            if (configurationService == null) throw new ArgumentNullException(nameof(configurationService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            connection = new HubConnectionBuilder()
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
            connection.Closed += async (e) =>
            {
                logger.LogWarning($"Connection to {uri} closed");
                Disconnected?.Invoke(this, new EventArgs());
                if (retry && !connectCancellationToken.IsCancellationRequested)
                {
                    logger.LogDebug($"Reconnecting to {uri}...");
                    await Connect(connectCancellationToken);
                }
                else
                {
                    logger.LogDebug($"Not reconnecting to {uri}");
                }
            };
            Hub = CreateHubInterface(connection);
            RegisterAll();
        }

        public T Hub { get; }

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
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        cancellationToken.Register(CancelConnection);
                        logger.LogInformation($"Connecting to {uri}...");
                        await connection.StartAsync(cancellationToken);
                        logger.LogInformation($"Connected to {uri}");
                        Connected?.Invoke(this, new EventArgs());
                        return;
                    }
                    catch (HttpRequestException)
                    {
                        logger.LogWarning($"Connection to {uri} failed. Waiting {10000} ms before retrying");
                        await Task.Delay(10000, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug($"Connection to {uri} aborted");
                }
            }
        }

        public async Task Disconnect()
        {
            retry = false;
            await connection.StopAsync();
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

        private async void CancelConnection()
        {
            if (connection.State == HubConnectionState.Connected)
            {
                await Disconnect();
            }
        }


        private void RegisterAll()
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
