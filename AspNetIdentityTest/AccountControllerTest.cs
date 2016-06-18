namespace AspnetIdentityTest
{
    using System;
    using System.Web.Mvc;
    using System.Collections.Specialized;
    using System.Globalization;

    using AspnetIdentitySample.Models;
    using AspnetIdentitySample.Controllers;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit tests for account controller
    /// </summary>
    [TestClass]
    public class AccountControllerTest
    {
        /// <summary>
        /// Tests the GET for register.
        /// </summary>
        [TestMethod]
        public void TestRegisterGet()
        {
            var accountController = new AccountController();
            var result = accountController.Register();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void TestRegisterPostInvalidModel()
        {
            var registerModel = new AspnetIdentitySample.Models.RegisterViewModel { };
            var accountController = CreateAccountController(registerModel);
            var result = accountController.Register(registerModel);
            Assert.IsInstanceOfType(result.Result, typeof(ViewResult));
        }

        [TestMethod]
        public void TestAccountLoginGet()
        {
            var accountController = new AccountController();
            var result = accountController.Login(null);
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void TestAccountLoginPostInvalidModel()
        {
            var loginViewModel = new AspnetIdentitySample.Models.LoginViewModel { };
            var accountController = CreateAccountController(loginViewModel);
            var result = accountController.Login(loginViewModel, null);
            Assert.IsInstanceOfType(result.Result, typeof(ViewResult));
        }

        private AccountController CreateAccountController(object ViewModel)
        {
            var accountController = new AccountController(MockingHelper.ApplicationManager);
            accountController.ControllerContext = MockingHelper.ControllerContext;
            if (ViewModel != null)
            {
                var modelBinder = new ModelBindingContext()
                {
                    ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => ViewModel, ViewModel.GetType()),
                    ValueProvider = new NameValueCollectionValueProvider(new NameValueCollection(), CultureInfo.InvariantCulture)
                };
                var binder = new DefaultModelBinder().BindModel(accountController.ControllerContext, modelBinder);
                accountController.ModelState.Clear();
                accountController.ModelState.Merge(modelBinder.ModelState);
            }
            return accountController;
        }

    }
}
