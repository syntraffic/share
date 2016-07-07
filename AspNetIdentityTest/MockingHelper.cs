namespace AspnetIdentityTest
{
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Mvc;

    using AspnetIdentitySample.Models;
    
    using Microsoft.AspNet.Identity;
    
    using Moq;
    
    /// <summary>
    /// Helper class to create mocking context
    /// </summary>
    static class MockingHelper
    {
        /// <summary>
        /// The mocked DB context
        /// </summary>
        private static Mock<MyDbContext> m_Context;

        /// <summary>
        /// The mocked Controller context
        /// </summary>
        private static Mock<ControllerContext> m_controllerContext;

        /// <summary>
        /// The mocked application user manager
        /// </summary>
        private static Mock<UserManager<ApplicationUser>> m_applicationUserManager;

        /// <summary>
        /// The user identifier
        /// </summary>
        private static string  userId = "39e59a67-d311-4715-845b-7e60702ec3af";

        /// <summary>
        /// The username
        /// </summary>
        private static string username = "help@syntraffic.com";

        /// <summary>
        /// Initialize a default application user
        /// </summary>
        private static ApplicationUser user = new ApplicationUser { Id = userId, UserName = username, HomeTown = "Seattle", MyUserInfo = new MyUserInfo() { FirstName = "Syn", LastName = "Traffic", Id = 1 } };

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

    /// <summary>
    /// Implementation of query provider for the entity framework
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <seealso cref="System.Data.Entity.Infrastructure.IDbAsyncQueryProvider" />
    internal class TestDbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
    {
        /// <summary>
        /// The query provider
        /// </summary>
        private readonly IQueryProvider _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbAsyncQueryProvider{TEntity}"/> class.
        /// </summary>
        /// <param name="inner">The inner.</param>
        internal TestDbAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Constructs an <see cref="T:System.Linq.IQueryable" /> object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>
        /// An <see cref="T:System.Linq.IQueryable" /> that can evaluate the query represented by the specified expression tree.
        /// </returns>
        public IQueryable CreateQuery(Expression expression)
        {
            return new TestDbAsyncEnumerable<TEntity>(expression);
        }

        /// <summary>
        /// Constructs an <see cref="T:System.Linq.IQueryable`1" /> object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements of the <see cref="T:System.Linq.IQueryable`1" /> that is returned.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>
        /// An <see cref="T:System.Linq.IQueryable`1" /> that can evaluate the query represented by the specified expression tree.
        /// </returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestDbAsyncEnumerable<TElement>(expression);
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>
        /// The value that results from executing the specified query.
        /// </returns>
        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        /// <summary>
        /// Executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>
        /// The value that results from executing the specified query.
        /// </returns>
        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        /// <summary>
        /// Asynchronously executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the value that results from executing the specified query.
        /// </returns>
        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(expression));
        }

        /// <summary>
        /// Asynchronously executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the value that results from executing the specified query.
        /// </returns>
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<TResult>(expression));
        }
    }

    /// <summary>
    /// Implementation of enumerable query for entity framework 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Linq.EnumerableQuery{T}" />
    /// <seealso cref="System.Data.Entity.Infrastructure.IDbAsyncEnumerable{T}" />
    /// <seealso cref="System.Linq.IQueryable{T}" />
    internal class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbAsyncEnumerable{T}"/> class.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        public TestDbAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbAsyncEnumerable{T}"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        public TestDbAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        /// <summary>
        /// Gets the asynchronous enumerator.
        /// </summary>
        /// <returns></returns>
        public IDbAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new TestDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        /// <summary>
        /// Gets an enumerator that can be used to asynchronously enumerate the sequence.
        /// </summary>
        /// <returns>
        /// Enumerator for asynchronous enumeration over the sequence.
        /// </returns>
        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return GetAsyncEnumerator();
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        IQueryProvider IQueryable.Provider
        {
            get { return new TestDbAsyncQueryProvider<T>(this); }
        }
    }

    /// <summary>
    /// Implementation of the database async enumerator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Data.Entity.Infrastructure.IDbAsyncEnumerator{T}" />
    internal class TestDbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
    {
        /// <summary>
        /// The internal enumerator 
        /// </summary>
        private readonly IEnumerator<T> _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbAsyncEnumerator{T}"/> class.
        /// </summary>
        /// <param name="inner">The inner.</param>
        public TestDbAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _inner.Dispose();
        }

        /// <summary>
        /// Advances the enumerator to the next element in the sequence, returning the result asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the sequence.
        /// </returns>
        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_inner.MoveNext());
        }

        /// <summary>
        /// Gets the current element.
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        public T Current
        {
            get { return _inner.Current; }
        }

        /// <summary>
        /// Gets the current element.
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        object IDbAsyncEnumerator.Current
        {
            get { return Current; }
        }
    }

}
