using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AspnetIdentitySample.Controllers;
using System.Web.Mvc;
using System.Collections.Specialized;
using System.Globalization;
using AspnetIdentitySample.Models;
using AspnetIdentityTest;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Microsoft.AspNet.Identity;
using System.Net;

namespace AspNetIdentityTest
{
    [TestClass]
    public class ToDoControllerTest
    {
        [TestMethod]
        public void TestToDoIndex()
        {

            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 1, Description = "Test desc", IsDone = false};
            MockingHelper.AddToDo(toDo);
            var result = controller.Index();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var model = (result as ViewResult).Model as IEnumerable<ToDo>;
            Assert.IsNotNull(model);
            Assert.AreEqual(true, model.Any(t => t.Id == toDo.Id));
        }

        [TestMethod]
        public void TestToDoDetailsWithNoId()
        {
            var controller = CreateToDoController(isAuthorized: false);
            var result = controller.Details(null).Result;
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            var httpResult = result as HttpStatusCodeResult;
            Assert.AreEqual((int)HttpStatusCode.BadRequest, httpResult.StatusCode);
        }

        [TestMethod]
        public void TestToDoDetailsWithInvalidId()
        {
            var controller = CreateToDoController();
            var result = controller.Details(0).Result;
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void TestToDoDetailsWithValidIdAndUnauthorizedUser()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 2, Description = "Test desc", IsDone = false };
            MockingHelper.AddToDo(toDo, false);
            var result = controller.Details(2).Result;
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            var httpResult = result as HttpStatusCodeResult;
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, httpResult.StatusCode);
        }


        [TestMethod]
        public void TestToDoDetails()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 3, Description = "3rd description", IsDone = false };
            MockingHelper.AddToDo(toDo);
            var result = controller.Details(toDo.Id).Result;
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var dataModel = (result as ViewResult).Model as ToDo;
            Assert.AreEqual(toDo.Id, dataModel.Id);
        }

        [TestMethod]
        public void TestToDoEditWithNoId()
        {
            var controller = CreateToDoController(isAuthorized: false);
            int? id = null;
            var result = controller.Edit(id).Result;
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            var httpResult = result as HttpStatusCodeResult;
            Assert.AreEqual((int)HttpStatusCode.BadRequest, httpResult.StatusCode);
        }

        [TestMethod]
        public void TestToDoEditWithInvalidId()
        {
            var controller = CreateToDoController();
            var result = controller.Edit(0).Result;
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void TestToDoEditWithValidIdAndUnauthorizedUser()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 4, Description = "4th desc", IsDone = false };
            MockingHelper.AddToDo(toDo, false);
            var result = controller.Edit(2).Result;
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            var httpResult = result as HttpStatusCodeResult;
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, httpResult.StatusCode);
        }


        [TestMethod]
        public void TestToDoEdit()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 5, Description = "5th description", IsDone = false };
            MockingHelper.AddToDo(toDo);
            var result = controller.Edit(toDo.Id).Result;
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var dataModel = (result as ViewResult).Model as ToDo;
            Assert.AreEqual(toDo.Id, dataModel.Id);
        }

        [TestMethod]
        public void TestToDoEditPostInvalidModel()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 5,  IsDone = false };
            MockingHelper.AddToDo(toDo);
            controller.ModelState.AddModelError("Description", "Description is required.");
            var result = controller.Edit(toDo).Result;
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void TestToDoEditPost()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 6, Description = "6th description", IsDone = false };
            MockingHelper.AddToDo(toDo);
            toDo.IsDone = true;
            var result = controller.Edit(toDo).Result;
            Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));
        }

        [TestMethod]
        public void TestToDoCreate()
        {
            var controller = CreateToDoController();
            var result = controller.Create();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void TestToDoCreatePostInvalidModel()
        {
            var newTask = new ToDo { };
            var controller = CreateToDoController(newTask);
            controller.ModelState.AddModelError("Description", "Description is required.");
            var result = controller.Create(newTask).Result;
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void TestToDoCreatePost()
        {
            var newTask = new ToDo { Id = 7, IsDone = false, Description = "7th description" };
            var controller = CreateToDoController(newTask);
            var result = controller.Create(newTask).Result;
            Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));
            Assert.AreEqual(true, MockingHelper.DBContext.ToDoes.Any(t => t.Id == newTask.Id));
        }

        [TestMethod]
        public void TestToDoDeleteWithNoId()
        {
            var controller = CreateToDoController();
            var result = controller.Delete(null).Result;
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            var httpResult = result as HttpStatusCodeResult;
            Assert.AreEqual((int)HttpStatusCode.BadRequest, httpResult.StatusCode);
        }

        [TestMethod]
        public void TestToDoDeleteWithInvalidId()
        {
            var controller = CreateToDoController();
            var result = controller.Delete(0).Result;
            Assert.IsInstanceOfType(result, typeof(HttpNotFoundResult));
        }

        [TestMethod]
        public void TestToDoDeleteWithValidIdAndUnauthorizedUser()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 8, Description = "8th description", IsDone = false };
            MockingHelper.AddToDo(toDo, false);
            var result = controller.Delete(toDo.Id).Result;
            Assert.IsInstanceOfType(result, typeof(HttpStatusCodeResult));
            var httpResult = result as HttpStatusCodeResult;
            Assert.AreEqual((int)HttpStatusCode.Unauthorized, httpResult.StatusCode);
        }

        [TestMethod]
        public void TestToDoDelete()
        {
            var controller = CreateToDoController();
            var toDo = new ToDo { Id = 9, Description = "9th description", IsDone = false };
            MockingHelper.AddToDo(toDo);
            var result = controller.Delete(toDo.Id).Result;
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var dataModel = (result as ViewResult).Model as ToDo;
            Assert.AreEqual(toDo.Id, dataModel.Id);
        }

        private ToDoController CreateToDoController(object ViewModel = null, bool isAuthorized = true)
        {
            MockingHelper.InitMocking();
            ToDoController toDoController;
            if (isAuthorized)
            {
                toDoController = new ToDoController(MockingHelper.DBContext, MockingHelper.ApplicationManager);
            }
            else 
            {
                var userStore = new Mock<IUserStore<ApplicationUser>>();
                var applicationUserManager = new Mock<UserManager<ApplicationUser>>(userStore.Object);
                toDoController = new ToDoController(MockingHelper.DBContext, applicationUserManager.Object);
            }
            
            toDoController.ControllerContext = MockingHelper.ControllerContext;
            if (ViewModel != null)
            {
                var modelBinder = new System.Web.Mvc.ModelBindingContext()
                {
                    ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => ViewModel, ViewModel.GetType()),
                    ValueProvider = new NameValueCollectionValueProvider(new NameValueCollection(), CultureInfo.InvariantCulture)
                };
                var binder = new DefaultModelBinder().BindModel(toDoController.ControllerContext, modelBinder);
                toDoController.ModelState.Clear();
                toDoController.ModelState.Merge(modelBinder.ModelState);
            }
            return toDoController;
        }
    }
}
