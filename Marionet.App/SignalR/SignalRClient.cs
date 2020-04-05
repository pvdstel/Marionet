using Marionet.App.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
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
        private Uri uri;
        private ILogger<SignalRClient<T>> logger;
        private bool retry = false;

        public SignalRClient(Uri uri, ILogger<SignalRClient<T>> logger)
        {
            this.uri = uri;
            this.logger = logger;
            connection = new HubConnectionBuilder()
                .AddMessagePackProtocol()
                .WithUrl(uri, options =>
                    {
                        options.WebSocketConfiguration = o =>
                        {
                            o.ClientCertificates.Add(Certificate.ClientCertificate);
                            o.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                            {
                                if (certificate is X509Certificate2 certificate2)
                                {
                                    return Certificate.IsParent(Certificate.ServerCertificate, certificate2);
                                }
                                else
                                {
                                    throw new ArgumentException("The argument " + nameof(certificate) + "must be of type " + nameof(X509Certificate2) + ".");
                                }
                            };
                        };
                        options.ClientCertificates.Add(Certificate.ClientCertificate);
                        options.HttpMessageHandlerFactory = message =>
                        {
                            if (message is HttpClientHandler clientHandler)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback = (m, certificate, chain, policyErrors) => Certificate.IsParent(Certificate.ServerCertificate, certificate);
                                clientHandler.ClientCertificates.Add(Certificate.ClientCertificate);
                            }
                            return message;
                        };
                    }
                ).Build();
            connection.Closed += async (e) =>
            {
                logger.LogWarning($"Connection to {uri} closed");
                Disconnected?.Invoke(this, new EventArgs());
                if (retry)
                {
                    logger.LogDebug($"Reconnecting to {uri}...");
                    await Connect();
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

        public async Task Connect(CancellationToken cancellationToken = default)
        {
            retry = true;
            int cooldown = 1000;
            ConnectingStarted?.Invoke(this, new EventArgs());
            while (true)
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
                    logger.LogWarning($"Connection to {uri} failed. Waiting {cooldown} ms before retrying");
                    await Task.Delay(cooldown);
                    cooldown = Math.Min(60000, cooldown * 2);
                }
                catch (OperationCanceledException)
                {
                    logger.LogDebug($"Connection to {uri} aborted");
                    break;
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
