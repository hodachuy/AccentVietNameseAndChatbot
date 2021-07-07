using Autofac;
using Autofac.Integration.Mvc;
using System.Web;
using System.Web.Mvc;

namespace BotProject.Web.Infrastructure.Core
{
    /// <summary>
    ///  Khoi tao instansce khong thong qua constructure.
    /// </summary>
    public static class ServiceFactory
    {
        public static THelper Get<THelper>()
        {
            if (HttpContext.Current != null)
            {
                var key = string.Concat("factory-", typeof(THelper).Name);
                if (!HttpContext.Current.Items.Contains(key))
                {
                    var resolvedService = DependencyResolver.Current.GetService<THelper>();
                    HttpContext.Current.Items.Add(key, resolvedService);
                }
                return (THelper)HttpContext.Current.Items[key];
            }
            return DependencyResolver.Current.GetService<THelper>();
        }
        public static THelper GetService<THelper>()
        {
            if (HttpContext.Current != null)
            {
                var key = string.Concat("factory-", typeof(THelper).Name);
                if (!HttpContext.Current.Items.Contains(key))
                {
                    var resolvedService = DependencyResolver.Current.GetService<THelper>();
                    HttpContext.Current.Items.Add(key, resolvedService);
                }
                return (THelper)HttpContext.Current.Items[key];
            }

            using (var c = AutofacDependencyResolver.Current.ApplicationContainer.BeginLifetimeScope("AutofacWebRequest"))
            {
                var userRepo = c.Resolve<THelper>();
                return userRepo;
            }
        }
    }
}