// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class ExecutorFactory
    {
        public static Func<Page, Func<IActionResult>, Task> Create(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            MethodInfo getHandler = null;
            MethodInfo postHandler = null;

            foreach (var method in typeInfo.GetMethods())
            {
                if (method.Name.StartsWith("OnGet"))
                {
                    if (getHandler != null)
                    {
                        throw new InvalidOperationException("Only one OnGet method is allowed");
                    }

                    getHandler = method;
                }

                if (method.Name.StartsWith("OnPost"))
                {
                    if (postHandler != null)
                    {
                        throw new InvalidOperationException("Only one OnPost method is allowed");
                    }

                    postHandler = method;
                }
            }

            return new Executor()
            {
                OnGet = HandlerMethod.Create("GET", getHandler),
                OnPost = HandlerMethod.Create("POST", postHandler),
            }.Execute;
        }

        private class Executor
        {
            public HandlerMethod OnGet { get; set; }

            public HandlerMethod OnPost { get; set; }

            public async Task Execute(Page page, Func<IActionResult> @default)
            {
                if (OnGet != null && 
                    string.Equals(OnGet.HttpMethod, page.PageContext.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    var arguments = new object[OnGet.Parameters.Length];
                    for (var i = 0; i < OnGet.Parameters.Length; i++)
                    {
                        var parameter = OnGet.Parameters[i];
                        arguments[i] = await page.Binder.BindModelAsync(page.PageContext, parameter.Type, parameter.DefaultValue, parameter.Name);
                    }

                    var result = await OnGet.Execute(page, arguments);
                    if (result != null)
                    {
                        await result.ExecuteResultAsync(page.PageContext);
                        return;
                    }
                }

                if (OnPost != null &&
                    string.Equals(OnPost.HttpMethod, page.PageContext.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    var arguments = new object[OnPost.Parameters.Length];
                    for (var i = 0; i < OnPost.Parameters.Length; i++)
                    {
                        var parameter = OnPost.Parameters[i];
                        arguments[i] = await page.Binder.BindModelAsync(page.PageContext, parameter.Type, parameter.DefaultValue, parameter.Name);
                    }

                    var result = await OnPost.Execute(page, arguments);
                    if (result != null)
                    {
                        await result.ExecuteResultAsync(page.PageContext);
                        return;
                    }
                }

                await (@default().ExecuteResultAsync(page.PageContext));
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
            public static HandlerMethod Create(string httpMethod, MethodInfo method)
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
                    return new NonGenericTaskHandlerMethod(httpMethod, parameters, method);
                }
                else if (method.ReturnType == typeof(void))
                {
                    return new VoidHandlerMethod(httpMethod, parameters, method);
                }
                else if (
                    method.ReturnType.IsConstructedGenericType &&
                    method.ReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>) &&
                    typeof(IActionResult).IsAssignableFrom(method.ReturnType.GetTypeInfo().GetGenericArguments()[0]))
                {
                    return new GenericTaskHandlerMethod(httpMethod, parameters, method);
                }
                else if (typeof(IActionResult).IsAssignableFrom(method.ReturnType))
                {
                    return new ActionResultHandlerMethod(httpMethod, parameters, method);
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

            protected HandlerMethod(string httpMethod, HandlerParameter[] parameters)
            {
                HttpMethod = httpMethod;
                Parameters = parameters;
            }

            public string HttpMethod { get; }

            public HandlerParameter[] Parameters { get; }

            public abstract Task<IActionResult> Execute(Page page, object[] arguments);
        }

        private class NonGenericTaskHandlerMethod : HandlerMethod
        {
            private readonly Func<Page, object[], Task> _thunk;

            public NonGenericTaskHandlerMethod(string httpMethod, HandlerParameter[] parameters, MethodInfo method)
                : base(httpMethod, parameters)
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

            public GenericTaskHandlerMethod(string httpMethod, HandlerParameter[] parameters, MethodInfo method)
                : base(httpMethod, parameters)
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

            public VoidHandlerMethod(string httpMethod, HandlerParameter[] parameters, MethodInfo method)
                : base(httpMethod, parameters)
            {
                var page = Expression.Parameter(typeof(Page), "page");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Action<Page, object[]>>(
                    Expression.Call(
                        Expression.Convert(page, method.DeclaringType),
                        method,
                        Unpack(arguments, parameters))).Compile();
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

            public ActionResultHandlerMethod(string httpMethod, HandlerParameter[] parameters, MethodInfo method)
                : base(httpMethod, parameters)
            {
                var page = Expression.Parameter(typeof(Page), "page");
                var arguments = Expression.Parameter(typeof(object[]), "arguments");

                _thunk = Expression.Lambda<Func<Page, object[], IActionResult>>(
                    Expression.Convert(
                        Expression.Call(
                            Expression.Convert(page, method.DeclaringType),
                            method,
                            Unpack(arguments, parameters)),
                        typeof(IActionResult))).Compile();
            }

            public override Task<IActionResult> Execute(Page page, object[] arguments)
            {
                return Task.FromResult(_thunk(page, arguments));
            }
        }
    }
}
