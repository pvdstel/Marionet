using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Marionet.App.SignalR
{
    public class HubDelegateFactory
    {
        private static readonly MethodInfo InvokedMethod = typeof(HubDelegateFactory).GetMethod(nameof(InvokeOnHub), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo InvokedMethodWithResult = typeof(HubDelegateFactory).GetMethod(nameof(InvokeOnHubWithResult), BindingFlags.Static | BindingFlags.NonPublic)!;

        public HubDelegateFactory(HubConnection hubConnection)
        {
            HubConnection = hubConnection;
        }

        public HubConnection HubConnection { get; }

        public object CreateDelegate(Type delegateType, string name)
        {
            if (delegateType == null)
            {
                throw new ArgumentNullException(nameof(delegateType));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!delegateType.IsSubclassOf(typeof(Delegate)))
            {
                throw new ArgumentException("The field does not have a delegate type.");
            }

            Type[] genericTypes = delegateType.GetGenericArguments();
            Type returnType = genericTypes.Last();

            if (genericTypes.Length == 0
                || (!returnType.Equals(typeof(Task))
                && (!returnType.IsGenericType || !returnType.GetGenericTypeDefinition().Equals(typeof(Task<>)))))
            {
                throw new ArgumentException("The return value of the hub function must be of type Task.");
            }

            ParameterExpression[] parameterTypes = genericTypes.SkipLast(1).Select(t => Expression.Parameter(t)).ToArray();
            Expression arg = Expression.NewArrayInit(typeof(object), parameterTypes.Select(x => Expression.Convert(x, typeof(object))));
            IEnumerable<Expression> arguments = new[] { Expression.Constant(HubConnection), Expression.Constant(name), arg };

            bool hasReturnType = returnType.GenericTypeArguments.Length > 0;
            MethodInfo method = hasReturnType ? InvokedMethodWithResult.MakeGenericMethod(returnType.GenericTypeArguments[0]) : InvokedMethod;
            Expression body = Expression.Call(method, arguments);
            LambdaExpression delegateExpression = Expression.Lambda(delegateType, body, parameterTypes);
            return delegateExpression.Compile();
        }

        private static async Task InvokeOnHub(HubConnection connection, string name, object[] arguments)
            => await connection.InvokeCoreAsync(name, arguments);

        private static async Task<T> InvokeOnHubWithResult<T>(HubConnection connection, string name, object[] arguments)
            => await connection.InvokeCoreAsync<T>(name, arguments);
    }
}
