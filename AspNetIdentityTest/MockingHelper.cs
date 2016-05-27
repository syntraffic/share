namespace AspnetIdentityTest
{
    using AspnetIdentitySample.Models;
    using Microsoft.AspNet.Identity;
    using Microsoft.Owin.Security;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;

    /// <summary>
    /// Helper class to create mocking context
    /// </summary>
    static class MockingHelper
    {
        private static Mock<MyDbContext> m_Context;
        private static Mock<ControllerContext> m_controllerContext;
        private static Mock<UserManager<ApplicationUser>> m_applicationUserManager;
        private static string  userId = "39e59a67-d311-4715-845b-7e60702ec3af";
        private static string username = "ram_jrs@outlook.com";
        private static ApplicationUser user = new ApplicationUser { Id = userId, UserName = username };
        /// <summary>
        /// Gets the mocked database context.
        /// </summary>
        /// <value>
        /// The database context.
        /// </value>
        public static MyDbContext DBContext
        {
            get
            {
                return m_Context.Object;
            }
        }

        /// <summary>
        /// Gets the mocked controller context.
        /// </summary>
        /// <value>
        /// The controller context.
        /// </value>
        public static ControllerContext ControllerContext
        {
            get
            {
                if (m_controllerContext == null)
                {
                    InitHttpContext();
                }
                return m_controllerContext.Object;
            }
        }

        /// <summary>
        /// Gets the mocked application manager.
        /// </summary>
        /// <value>
        /// The application manager.
        /// </value>
        public static UserManager<ApplicationUser> ApplicationManager
        {
            get
            {
                if (m_applicationUserManager == null)
                {
                    InitMockingAuthenticationLayer();
                }
                return m_applicationUserManager.Object;
            }
        }

        
        /// <summary>
        /// Initializes the mocking authentication layer.
        /// </summary>
        private static void InitMockingAuthenticationLayer()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            m_applicationUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object);
            m_applicationUserManager.Setup(u => u.FindByIdAsync(user.Id)).Returns(Task.FromResult(user));
            m_applicationUserManager.Setup(u => u.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).Returns(Task.FromResult(IdentityResult.Success));
        }

        /// <summary>
        /// Initializes the mocking.
        /// </summary>
        public static void InitMocking()
        {
            if (m_Context != null) // already initialized
            {
                return;
            }

            m_Context = new Mock<MyDbContext>();

            var todoList = new List<ToDo>();
            var mockedToDbSet = GetQueryableMockDbSet<ToDo>(todoList);
            mockedToDbSet.Setup(d => d.FindAsync(It.IsAny<object[]>())).Returns<object[]>(ids => Task.FromResult(todoList.Find(t => t.Id == (int)ids[0])));
            m_Context.Setup(c => c.ToDoes).Returns(mockedToDbSet.Object);

            var myUsersList = new List<MyUserInfo>();
            m_Context.Setup(c => c.MyUserInfo).Returns(GetQueryableMockDbSet<MyUserInfo>(myUsersList).Object);

        }

        /// <summary>
        /// Gets the queryable mock database set.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceList">The source list.</param>
        /// <returns></returns>
        private static Mock<DbSet<T>> GetQueryableMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var dbSet = new Mock<DbSet<T>>();

            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>((s) => sourceList.Add(s));
            dbSet.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>(s => sourceList.Remove(s));
            dbSet.As<IDbAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator())
                .Returns(new TestDbAsyncEnumerator<T>(sourceList.GetEnumerator()));
            dbSet.As<IQueryable<T>>().Setup(m => m.Provider)
                .Returns(new TestDbAsyncQueryProvider<T>(queryable.Provider));

            return dbSet;
        }

        /// <summary>
        /// Initializes the HTTP context.
        /// </summary>
        private static void InitHttpContext()
        {
            if (m_controllerContext != null)
                return;

            var m_HttpContext = new Mock<System.Web.HttpContextBase>();
            List<Claim> claims = new List<Claim>{
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", username),
                new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", userId)
            };
            var genericIdentity = new GenericIdentity("");
            genericIdentity.AddClaims(claims);
            var genericPrincipal = new GenericPrincipal(genericIdentity, new string[] { });

            m_HttpContext.SetupGet(x => x.User).Returns(genericPrincipal);
            m_controllerContext = new Mock<ControllerContext>();
            m_controllerContext.Setup(t => t.HttpContext).Returns(m_HttpContext.Object);
        }

        public static void AddToDo(ToDo newTodo, bool isAddUser = true)
        {
            if (isAddUser)
            {
                newTodo.User = user;
            }
            else 
            {
                newTodo.User = new ApplicationUser { Id = "0", UserName = "No User" };
            }
            DBContext.ToDoes.Add(newTodo);
        }
    }

    internal class TestDbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestDbAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestDbAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestDbAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(expression));
        }

        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<TResult>(expression));
        }
    }

    internal class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
    {
        public TestDbAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestDbAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IDbAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TestDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return GetAsyncEnumerator();
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestDbAsyncQueryProvider<T>(this); }
        }
    }

    internal class TestDbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestDbAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_inner.MoveNext());
        }

        public T Current
        {
            get { return _inner.Current; }
        }

        object IDbAsyncEnumerator.Current
        {
            get { return Current; }
        }
    }

}
