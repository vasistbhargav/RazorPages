// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class ExecutorFactory
    {
        public static Func<Page, Task<IActionResult>> Create(MethodInfo method)
        {
            return new Executor()
            {
                Method = method,
            }.Execute;
        }

        private class Executor
        {
            public Type Type { get; set; }

            public MethodInfo Method { get; set; }

            public async Task<IActionResult> Execute(Page page)
            {
                var handler = HandlerMethod.Create(Method);

                var arguments = new object[handler.Parameters.Length];
                for (var i = 0; i < handler.Parameters.Length; i++)
                {
                    var parameter = handler.Parameters[i];
                    arguments[i] = await page.Binder.BindModelAsync(page.PageContext, parameter.Type, parameter.DefaultValue, parameter.Name);
                }

                var result = await handler.Execute(page, arguments);
                return result;
            }
        }

        private class HandlerParameter
        {
            public string Name { get; set; }

            public Type Type { get; set; }

            public object DefaultValue { get; set; }
        }

        private abstract class HandlerMethod
        {
            public static HandlerMethod Create(MethodInfo method)
            {
                var methodParameters = method.GetParameters();
                var parameters = new HandlerParameter[methodParameters.Length];

                for (var i = 0; i < methodParameters.Length; i++)
                {
                    parameters[i] = new HandlerParameter()
                    {
                        DefaultValue = methodParameters[i].HasDefaultValue ? methodParameters[i].DefaultValue : null,
                        Name = methodParameters[i].Name,
                        Type = methodParameters[i].ParameterType,
                    };
                }

                if (method.ReturnType == typeof(Task))
                {
                    return new NonGenericTaskHandlerMethod(parameters, method);
                }
                else if (method.ReturnType == typeof(void))
                {
                    return new VoidHandlerMethod(parameters, method);
                }
                else if (
                    method.ReturnType.IsConstructedGenericType &&
                    method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>) &&
                    typeof(IActionResult).IsAssignableFrom(method.ReturnType.GetTypeInfo().GetGenericArguments()[0]))
                {
                    return new GenericTaskHandlerMethod(parameters, method);
                }
                else if (typeof(IActionResult).IsAssignableFrom(method.ReturnType))
                {
                    return new ActionResultHandlerMethod(parameters, method);
                }
                else
                {
                    throw new InvalidOperationException("unsupported handler method return type");
                }
            }

            protected static Expression[] Unpack(Expression arguments, HandlerParameter[] parameters)
            {
                var unpackExpressions = new Expression[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    unpackExpressions[0] = Expression.Convert(Expression.ArrayIndex(arguments, Expression.Constant(i)), parameters[i].Type);
                }

                return unpackExpressions;
            }

            protected HandlerMethod(HandlerParameter[] parameters)
            {
                Parameters = parameters;
            }

            public HandlerParameter[] Parameters { get; }

            public abstract Task<IActionResult> Execute(Page page, object[] arguments);
        }

        private class NonGenericTaskHandlerMethod : HandlerMethod
        {
            private readonly Func<Page, object[], Task> _thunk;

            public NonGenericTaskHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var page = Expression.Parameter(typeof(Page), "page");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Func<Page, object[], Task>>(
                    Expression.Call(
                        Expression.Convert(page, method.DeclaringType),
                        method,
                        Unpack(arguments, parameters)),
                    page,
                    arguments).Compile();
            }

            public override async Task<IActionResult> Execute(Page page, object[] arguments)
            {
                await _thunk(page, arguments);
                return null;
            }
        }

        private class GenericTaskHandlerMethod : HandlerMethod
        {
            private static readonly MethodInfo ConvertMethod = typeof(GenericTaskHandlerMethod).GetMethod(
                nameof(Convert), 
                BindingFlags.NonPublic | BindingFlags.Static);

            private readonly Func<Page, object[], Task<object>> _thunk;

            public GenericTaskHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var page = Expression.Parameter(typeof(Page), "page");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Func<Page, object[], Task<object>>>(
                    Expression.Call(
                        ConvertMethod.MakeGenericMethod(method.ReturnType.GenericTypeArguments),
                        Expression.Convert(
                            Expression.Call(
                                Expression.Convert(page, method.DeclaringType), 
                                method, 
                                Unpack(arguments, parameters)),
                            typeof(object))),
                    page,
                    arguments).Compile();
            }

            public override async Task<IActionResult> Execute(Page page, object[] arguments)
            {
                var result = await _thunk(page, arguments);
                return (IActionResult)result;
            }

            private static async Task<object> Convert<T>(object taskAsObject)
            {
                var task = (Task<T>)taskAsObject;
                return (object)await task;
            }
        }

        private class VoidHandlerMethod : HandlerMethod
        {
            private readonly Action<Page, object[]> _thunk;

            public VoidHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var page = Expression.Parameter(typeof(Page), "page");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Action<Page, object[]>>(
                    Expression.Call(
                        Expression.Convert(page, method.DeclaringType),
                        method,
                        Unpack(arguments, parameters)),
                    page,
                    arguments).Compile();
            }

            public override Task<IActionResult> Execute(Page page, object[] arguments)
            {
                _thunk(page, arguments);
                return Task.FromResult<IActionResult>(null);
            }
        }

        private class ActionResultHandlerMethod : HandlerMethod
        {
            private readonly Func<Page, object[], IActionResult> _thunk;

            public ActionResultHandlerMethod(HandlerParameter[] parameters, MethodInfo method)
                : base(parameters)
            {
                var page = Expression.Parameter(typeof(Page), "page");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Func<Page, object[], IActionResult>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(page, method.DeclaringType),
                            method,
                            Unpack(arguments, parameters)),
                        typeof(IActionResult)),
                    page,
                    arguments).Compile();
            }

            public override Task<IActionResult> Execute(Page page, object[] arguments)
            {
                return Task.FromResult(_thunk(page, arguments));
            }
        }
    }
}
