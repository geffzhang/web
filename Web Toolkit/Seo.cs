﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace NeoSmart.Web
{
    class CachedMethod
    {
        public string Controller;
        public string Action;
    }

    public class Seo
    {
        private static readonly Dictionary<string, CachedMethod> MethodCache = new Dictionary<string, CachedMethod>();
        public static void SeoRedirect(Controller controller, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            string key = string.Format("{0}:{1}", filePath, lineNumber);
            CachedMethod lastMethod;
            if (!MethodCache.TryGetValue(key, out lastMethod))
            {
                var method = new StackFrame(1).GetMethod();
                lastMethod = new CachedMethod
                    {
                        Action = method.Name,
                        Controller = method.DeclaringType.Name.Remove(method.DeclaringType.Name.Length - "Controller".Length)
                    };
                MethodCache.Add(key, lastMethod);
            }

            string destination;
            if (DetermineSeoRedirect(controller, lastMethod, out destination))
            {
                controller.Response.RedirectPermanent(destination);
            }
        }

        private static bool DetermineSeoRedirect(Controller controller, CachedMethod method, out string destination)
        {
            string currentAction = (string)controller.RouteData.Values["action"];
            string currentController = (string)controller.RouteData.Values["controller"];

            //Case Redirect
            if (currentAction != method.Action || currentController != method.Controller)
            {
                destination = HttpContext.Current.Request.Url.AbsolutePath.Replace(currentAction, method.Action).Replace(currentController, method.Controller);
                return true;
            }

            //Trailing-backslash configuration
            bool isIndex = method.Action == "Index";
            if ((isIndex && !HttpContext.Current.Request.Url.AbsolutePath.EndsWith("/")) || (!isIndex && HttpContext.Current.Request.Url.AbsolutePath.EndsWith("/")))
            {
                destination = string.Format("{0}{1}", HttpContext.Current.Request.Url.AbsolutePath.TrimEnd(new[] { '/' }), isIndex ? "/" : "");
                return true;
            }

            //No Index in link
            if (isIndex && HttpContext.Current.Request.Url.AbsolutePath.EndsWith("/Index/"))
            {
                const string search = "Index/";
                destination = HttpContext.Current.Request.Url.AbsolutePath.Substring(0, HttpContext.Current.Request.Url.AbsolutePath.Length - search.Length);
                return true;
            }

            destination = HttpContext.Current.Request.Url.AbsolutePath;
            return false;
        }
    }
}
