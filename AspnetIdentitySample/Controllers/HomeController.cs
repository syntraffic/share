namespace AspnetIdentitySample.Controllers
{
    using AspnetIdentitySample.Models;
    
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    
    using System.Web.Mvc;

    /// <summary>
    /// home controller
    /// </summary>
    /// <seealso cref="System.Web.Mvc.Controller" />
    public class HomeController : Controller
    {
        /// <summary> 
        /// db context
        /// </summary>
        private MyDbContext db;

        /// <summary>
        /// The application user manager
        /// </summary>
        private UserManager<ApplicationUser> manager;

        public HomeController()
        {
            db = new MyDbContext();
            manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="userManager">The user manager.</param>
        public HomeController(MyDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            manager = userManager;
        }

        public ActionResult Index()
        {
            return View();
        }

        // Only Authenticated users can access their profile
        [Authorize]
        public ActionResult Profile()
        {
            // Get the current logged in User and look up the user in ASP.NET Identity
            var currentUser = manager.FindById(User.Identity.GetUserId()); 
            
            // Retrieve the profile information about the logged in user
            ViewBag.HomeTown = currentUser.HomeTown;
            ViewBag.FirstName = currentUser.MyUserInfo.FirstName;

            return View();
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}