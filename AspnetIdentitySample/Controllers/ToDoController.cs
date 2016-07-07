namespace AspnetIdentitySample.Controllers
{
    using System.Data.Entity;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Mvc;

    using AspnetIdentitySample.Models;

    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;

    /// <summary>
    /// controller for todo actions
    /// </summary>
    /// <seealso cref="System.Web.Mvc.Controller" />
    [Authorize]
    public class ToDoController : Controller
    {
        /// <summary> 
        /// db context
        /// </summary>
        private MyDbContext db;

        /// <summary>
        /// The application user manager
        /// </summary>
        private UserManager<ApplicationUser> manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoController"/> class.
        /// </summary>
        public ToDoController()
        {
            db = new MyDbContext();
            manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoController"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="userManager">The user manager.</param>
        public ToDoController(MyDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            manager = userManager;
        }

        // GET: /ToDo/
        // GET ToDo for the logged in user
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var currentUser = manager.FindById(User.Identity.GetUserId());
            return View(db.ToDoes.ToList().Where(todo => todo.User.Id == currentUser.Id));
        }

        // GET: /ToDo/All
        /// <summary>
        /// Alls this instance.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles="Admin")]
        public async Task<ActionResult> All()
        {
            return View(await db.ToDoes.ToListAsync());
        }

        // GET: /ToDo/Details/5
        /// <summary>
        /// Details of the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async Task<ActionResult> Details(int? id)
        {
            var currentUser = await manager.FindByIdAsync(User.Identity.GetUserId()); 
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ToDo todo = await db.ToDoes.FindAsync(id);
            if (todo == null)
            {
                return HttpNotFound();
            }
            if (todo.User.Id != currentUser.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
            return View(todo);
        }

        // GET: /ToDo/Create
        /// <summary>
        /// Creates a new todo.
        /// </summary>
        /// <returns></returns>
        public ActionResult Create()
        {
            return View();
        }

        // POST: /ToDo/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Creates the specified todo.
        /// </summary>
        /// <param name="todo">The todo.</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include="Id,Description,IsDone")] ToDo todo)
        {
            var currentUser = await manager.FindByIdAsync(User.Identity.GetUserId()); 
            if (ModelState.IsValid)
            {
                todo.User = currentUser;
                db.ToDoes.Add(todo);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(todo);
        }

        // GET: /ToDo/Edit/5
        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async Task<ActionResult> Edit(int? id)
        {
            var currentUser = await manager.FindByIdAsync(User.Identity.GetUserId()); 
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ToDo todo = await db.ToDoes.FindAsync(id);
            if (todo == null)
            {
                return HttpNotFound();
            }
            if (todo.User.Id != currentUser.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
            return View(todo);
        }

        // POST: /ToDo/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        /// <summary>
        /// Edits the specified todo.
        /// </summary>
        /// <param name="todo">The todo.</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include="Id,Description,IsDone")] ToDo todo)
        {
            if (ModelState.IsValid)
            {
                var task = db.ToDoes.Where(t => t.Id == todo.Id).FirstOrDefault();
                if (task == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                task.IsDone = todo.IsDone;
                task.Description = todo.Description;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(todo);
        }

        // GET: /ToDo/Delete/5
        /// <summary>
        /// Deletes the specified todo.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async Task<ActionResult> Delete(int? id)
        {
            var currentUser = await manager.FindByIdAsync(User.Identity.GetUserId()); 
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ToDo todo = await db.ToDoes.FindAsync(id);
            if (todo == null)
            {
                return HttpNotFound();
            }
            if (todo.User.Id != currentUser.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            } 
            return View(todo);
        }

        // POST: /ToDo/Delete/5
        /// <summary>
        /// Deletes the confirmed todo.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            ToDo todo = await db.ToDoes.FindAsync(id);
            db.ToDoes.Remove(todo);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Releases unmanaged resources and optionally releases managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
