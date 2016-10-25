namespace AspnetIdentitySample.Controllers
{
    using AspnetIdentitySample.IdentityExtensions;
    using AspnetIdentitySample.Models;

    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;
    using Microsoft.Owin.Security;

    /// <summary>
    /// factory for claims identity
    /// </summary>
    /// <seealso cref="System.Web.Mvc.Controller" />
    public class ClaimsIdentityFactoryController : Controller
    {
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}