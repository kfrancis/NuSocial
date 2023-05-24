using CommunityToolkit.Mvvm.DependencyInjection;
using NuSocial.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Localization;

namespace NuSocial.Core
{
    internal interface ITinyApplication
    {
        event EventHandler ApplicationResume;
        event EventHandler ApplicationSleep;
    }

    internal static class ApplicationResolver
    {
        public static ITinyApplication? Current { get; set; }
    }

    public abstract class AppBase : Application, ITinyApplication
    {
        private static Action? _exitAction;
        private readonly WeakEventManager _eventManager = new();

        protected AppBase(IServiceProvider services)
        {
            Ioc.Default.ConfigureServices(services);
            ApplicationResolver.Current = this;
        }

        event EventHandler ITinyApplication.ApplicationResume
        {
            add
            {
                if (value != null)
                    _eventManager.AddEventHandler(nameof(ITinyApplication.ApplicationResume), value);
            }

            remove
            {
                if (value != null)
                    _eventManager.RemoveEventHandler(nameof(ITinyApplication.ApplicationResume), value);
            }
        }

        event EventHandler ITinyApplication.ApplicationSleep
        {
            add
            {
                if (value != null)
                    _eventManager.AddEventHandler(nameof(ITinyApplication.ApplicationSleep), value);
            }

            remove
            {
                if (value != null)
                    _eventManager.RemoveEventHandler(nameof(ITinyApplication.ApplicationSleep), value);
            }
        }

        public static Action? ExitApplication
        {
            get
            {
                return _exitAction;
            }
            set
            {
                _exitAction = value ?? new Action(DoNothing);
            }
        }

        public static async void LoadPersistedSession()
        {
            var accessTokenManager = Ioc.Default.GetRequiredService<IAccessTokenManager>();
            var dataStorageService = Ioc.Default.GetRequiredService<IDataStorageService>();
            var applicationContext = Ioc.Default.GetRequiredService<IApplicationContext>();

            accessTokenManager.AuthenticateResult = await dataStorageService.RetrieveAuthenticateResult();
            applicationContext.Load(await dataStorageService.RetrieveLoginInfo());
        }

        public static async Task OnAccessTokenRefresh(string newAccessToken)
        {
            await Ioc.Default.GetRequiredService<IDataStorageService>().StoreAccessTokenAsync(newAccessToken);
        }

        public static Task OnSessionTimeout()
        {
            return Ioc.Default.GetRequiredService<IAuthService>().LogoutAsync();
        }

        protected override void OnResume()
        {
            base.OnResume();

            _eventManager.HandleEvent(this, EventArgs.Empty, nameof(ITinyApplication.ApplicationResume));
        }

        protected override void OnSleep()
        {
            base.OnSleep();

            _eventManager.HandleEvent(this, EventArgs.Empty, nameof(ITinyApplication.ApplicationSleep));
        }

        private static void DoNothing()
        {
            // Nothing
        }
    }

    public interface IApplicationContext
    {
        UserConfiguration Configuration { get; set; }

        User LoginInfo { get; }

        void ClearLoginInfo();

        void SetLoginInfo(User loginInfo);

        void SetAsHost();

        void SetAsTenant(string tenancyName, int tenantId);

        LanguageInfo CurrentLanguage { get; set; }

        void Load(User loginInfo);
    }

    internal class WeakEventManager
    {
        private readonly Dictionary<string, List<(WeakReference Reference, MethodInfo Info)>> _eventHandlers = new();

        public void AddEventHandler<TEventArgs>(string eventName, EventHandler<TEventArgs> value)
            where TEventArgs : EventArgs
        {
            if (value.Target != null)
                BuildEventHandler(eventName, value.Target, value.GetMethodInfo());
        }

        public void AddEventHandler(string eventName, EventHandler value)
        {
            if (value.Target != null)
                BuildEventHandler(eventName, value.Target, value.GetMethodInfo());
        }

        public void HandleEvent(object sender, object args, string eventName)
        {
            var toRaise = new List<(object Object, MethodInfo Info)>();

            if (_eventHandlers.TryGetValue(eventName, out var targets))
            {
                foreach (var target in targets.ToList())
                {
                    var obj = target.Reference.Target;

                    if (obj == null)
                    {
                        targets.Remove(target);
                    }
                    else
                    {
                        toRaise.Add((obj, target.Info));
                    }
                }
            }

            foreach (var target in toRaise)
            {
                target.Info.Invoke(target.Object, new[] { sender, args });
            }
        }

        public void RemoveEventHandler<TEventArgs>(string eventName, EventHandler<TEventArgs> value)
        where TEventArgs : EventArgs
        {
            if (value.Target != null)
                RemoveEventHandlerImpl(eventName, value.Target, value.GetMethodInfo());
        }

        public void RemoveEventHandler(string eventName, EventHandler value)
        {
            if (value.Target != null)
                RemoveEventHandlerImpl(eventName, value.Target, value.GetMethodInfo());
        }

        private void BuildEventHandler(string eventName, object handlerTarget, MethodInfo methodInfo)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var target))
            {
                target = new List<(WeakReference Reference, MethodInfo Info)>();
                _eventHandlers.Add(eventName, target);
            }

            target.Add((new WeakReference(handlerTarget), methodInfo));
        }

        private void RemoveEventHandlerImpl(string eventName, object handlerTarget, MemberInfo methodInfo)
        {
            if (_eventHandlers.TryGetValue(eventName, out var targets))
            {
                var targetsToRemove = targets.Where(t => t.Reference.Target == handlerTarget &&
                    t.Info.Name == methodInfo.Name).ToList();

                foreach (var target in targetsToRemove)
                {
                    targets.Remove(target);
                }
            }
        }
    }
}
