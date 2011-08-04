﻿using System;
using System.Web;

namespace System.Web.WebPages {
    internal class WebPageHttpModule : IHttpModule {
        internal static EventHandler Initialize;
        internal static EventHandler ApplicationStart;
        internal static EventHandler BeginRequest;
        internal static EventHandler EndRequest;
        private static bool s_appStartExecuted = false;
        private static object s_appStartExecutedLock = new object();
        private static object s_hasBeenRegisteredKey = new object();

        internal static bool AppStartExecuteCompleted {
            get;
            set; 
        }

        public void Dispose() {
        }

        public void Init(HttpApplication application) {
            if (application.Context.Items[s_hasBeenRegisteredKey] != null) {
                // registration for this module has already run for this HttpApplication instance
                return;
            }

            application.Context.Items[s_hasBeenRegisteredKey] = true;

            InitApplication(application);
        }

        internal static void InitApplication(HttpApplication application) {
            // We need to run StartApplication first, so that any exception thrown during execution of the StartPage gets
            // recorded on StartPage.Exception
            StartApplication(application);
            InitializeApplication(application);
        }

        internal static void InitializeApplication(HttpApplication application) {
            InitializeApplication(application, OnApplicationPostResolveRequestCache, Initialize);
        }

        internal static void InitializeApplication(HttpApplication application, EventHandler onApplicationPostResolveRequestCache, EventHandler initialize) {
            if (initialize != null) {
                initialize(application, EventArgs.Empty);
            }
            application.PostResolveRequestCache += onApplicationPostResolveRequestCache;
            if (ApplicationStartPage.Exception != null || BeginRequest != null) {
                application.BeginRequest += OnBeginRequest;
            }

            application.EndRequest += OnEndRequest;
        }

        internal static void StartApplication(HttpApplication application) {
            StartApplication(application, ApplicationStartPage.ExecuteStartPage, ApplicationStart);
        }

        internal static void StartApplication(HttpApplication application, Action<HttpApplication> executeStartPage, EventHandler applicationStart) {
            // Application start events should happen only once per application life time.
            lock (s_appStartExecutedLock) {
                if (!s_appStartExecuted) {
                    s_appStartExecuted = true;

                    executeStartPage(application);
                    AppStartExecuteCompleted = true;
                    if (applicationStart != null) {
                        applicationStart(application, EventArgs.Empty);
                    }
                }
            }
        }

        internal static void OnApplicationPostResolveRequestCache(object sender, EventArgs e) {
            HttpContextBase context = new HttpContextWrapper(((HttpApplication)sender).Context);
            new WebPageRoute().DoPostResolveRequestCache(context);
        }

        internal static void OnBeginRequest(object sender, EventArgs e) {
            if (ApplicationStartPage.Exception != null) {
                // Throw it as a HttpException so as to
                // display the original stack trace information.
                throw new HttpException(null, ApplicationStartPage.Exception);
            }
            if (BeginRequest != null) {
                BeginRequest(sender, e);
            }
        }

        internal static void OnEndRequest(object sender, EventArgs e) {
            if (EndRequest != null) {
                EndRequest(sender, e);
            }

            var app = (HttpApplication)sender;
            RequestResourceTracker.DisposeResources(new HttpContextWrapper(app.Context));
        }
    }
}
