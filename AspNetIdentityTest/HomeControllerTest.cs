namespace AspNetIdentityTest
{
    using System;
    using System.Web.Mvc;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    using AspnetIdentitySample.Controllers;
    using AspnetIdentityTest;

    /// <summary>
    /// tests for home controller
    /// </summary>
    [TestClass]
    public class HomeControllerTest
    {
        /// <summary>
        /// Tests the index method.
        /// </summary>
        [TestMethod]
        public void TestIndex()
        {
            // Arrange
            var controller = CreateHomeController();

            // Act
            var result = controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestProfile()
        {
            // Arrange
            var controller = CreateHomeController();

            // Act
            var result = controller.Profile() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestAbout()
        {
            // Arrange
            var controller = CreateHomeController();

            // Act
            var result = controller.About() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestContact()
        {
            // Arrange
            var controller = CreateHomeController();

            // Act
            var result = controller.Contact() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Creates the home controller using mocks.
        /// </summary>
        /// <returns></returns>
        private HomeController CreateHomeController()
        {
            MockingHelper.InitMocking();

            HomeController controller = new HomeController(MockingHelper.DBContext, MockingHelper.ApplicationManager);

            controller.ControllerContext = MockingHelper.ControllerContext;

            return controller;
        }
    }
}
