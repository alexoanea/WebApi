﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.TestCommon;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.TestCommon;
using Moq;
using ODataPath = System.Web.OData.Routing.ODataPath;

namespace System.Web.OData.Builder
{
    public class LinkGenerationHelpersTest
    {
        private CustomersModelWithInheritance _model = new CustomersModelWithInheritance();

        [Theory]
        [InlineData(false, "http://localhost/Customers(42)")]
        [InlineData(true, "http://localhost/Customers(42)/NS.SpecialCustomer")]
        public void GenerateSelfLink_WorksToGenerateExpectedSelfLink_ForEntitySet(bool includeCast, string expectedIdLink)
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            var idLink = entityContext.GenerateSelfLink(includeCast);

            // Assert
            Assert.Equal(expectedIdLink, idLink.ToString());
        }

        [Theory]
        [InlineData(false, "http://localhost/Customers(42)/Orders")]
        [InlineData(true, "http://localhost/Customers(42)/NS.SpecialCustomer/Orders")]
        public void GenerateNavigationLink_WorksToGenerateExpectedNavigationLink_ForEntitySet(bool includeCast, string expectedNavigationLink)
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });
            IEdmNavigationProperty ordersProperty = _model.Customer.NavigationProperties().Single();

            // Act
            Uri uri = entityContext.GenerateNavigationPropertyLink(ordersProperty, includeCast);

            // Assert
            Assert.Equal(expectedNavigationLink, uri.AbsoluteUri);
        }

        [Theory]
        [InlineData(false, "http://localhost/Mary")]
        [InlineData(true, "http://localhost/Mary/NS.SpecialCustomer")]
        public void GenerateSelfLink_WorksToGenerateExpectedSelfLink_ForSingleton(bool includeCast, string expectedIdLink)
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Mary, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            var idLink = entityContext.GenerateSelfLink(includeCast);

            // Assert
            Assert.Equal(expectedIdLink, idLink.ToString());
        }

        [Theory]
        [InlineData(false, "http://localhost/Mary/Orders")]
        [InlineData(true, "http://localhost/Mary/NS.SpecialCustomer/Orders")]
        public void GenerateNavigationLink_WorksToGenerateExpectedNavigationLink_ForSingleton(bool includeCast, string expectedNavigationLink)
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Mary, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });
            IEdmNavigationProperty ordersProperty = _model.Customer.NavigationProperties().Single();

            // Act
            Uri uri = entityContext.GenerateNavigationPropertyLink(ordersProperty, includeCast);

            // Assert
            Assert.Equal(expectedNavigationLink, uri.AbsoluteUri);
        }

        [Theory]
        [InlineData(false, "http://localhost/MyOrders(42)/OrderLines(21)/OrderLines")]
        [InlineData(true, "http://localhost/MyOrders(42)/OrderLines(21)/NS.OrderLine/OrderLines")]
        public void GenerateNavigationLink_WorksToGenerateExpectedNavigationLink_ForContainedNavigation(
            bool includeCast,
            string expectedNavigationLink)
        {
            // NOTE: This test is generating a link that does not technically correspond to a valid model (specifically
            //       the extra OrderLines navigation), but it allows us to validate the nested navigation scenario
            //       without twisting the model unnecessarily.

            // Arrange
            IEdmEntityType myOrder = (IEdmEntityType)_model.Model.FindDeclaredType("NS.MyOrder");
            IEdmNavigationProperty orderLinesProperty = myOrder.NavigationProperties().Single(x => x.ContainsTarget);

            IEdmEntitySet entitySet = _model.Model.FindDeclaredEntitySet(("MyOrders"));
            IDictionary<string, object> parameters = new Dictionary<string, object>
            {
                {"ID", 42}
            };

            IDictionary<string, object> parameters2 = new Dictionary<string, object>
            {
                {"ID", 21}
            };

            var serializerContext = new ODataSerializerContext
            {
                Model = _model.Model,

                NavigationSource = _model.OrderLines,
                Path = new ODataPath(
                    new EntitySetSegment(entitySet),
                    new KeySegment(parameters.ToArray(), myOrder, entitySet),
                    new NavigationPropertySegment(orderLinesProperty, _model.OrderLines),
                    new KeySegment(parameters2.ToArray(),  _model.OrderLine, _model.OrderLines)),

                Url = GetODataRequest(_model.Model).GetUrlHelper(),
            };
            var entityContext = new ResourceContext(serializerContext, _model.OrderLine.AsReference(), new { ID = 21 });

            // Act
            Uri uri = entityContext.GenerateNavigationPropertyLink(orderLinesProperty, includeCast);

            // Assert
            Assert.Equal(expectedNavigationLink, uri.AbsoluteUri);
        }

        [Fact]
        public void GenerateNavigationLink_WorksToGenerateExpectedNavigationLink_ForNonContainedNavigation()
        {
            // Arrange
            IEdmEntityType myOrder = (IEdmEntityType)_model.Model.FindDeclaredType("NS.MyOrder");
            IEdmNavigationProperty orderLinesProperty = myOrder.NavigationProperties().Single(x => x.Name.Equals("NonContainedOrderLines"));

            IEdmEntitySet entitySet = _model.Model.FindDeclaredEntitySet(("MyOrders"));
            IDictionary<string, object> parameters = new Dictionary<string, object>
            {
                {"ID", 42}
            };

            IDictionary<string, object> parameters2 = new Dictionary<string, object>
            {
                {"ID", 21}
            };

            var serializerContext = new ODataSerializerContext
            {
                Model = _model.Model,

                NavigationSource = _model.OrderLines,
                Path = new ODataPath(
                    new EntitySetSegment(entitySet),
                    new KeySegment(parameters.ToArray(), myOrder, entitySet),
                    new NavigationPropertySegment(orderLinesProperty, _model.NonContainedOrderLines),
                    new KeySegment(parameters2.ToArray(), _model.OrderLine, _model.NonContainedOrderLines)),

                Url = GetODataRequest(_model.Model).GetUrlHelper(),
            };

            var entityContext = new ResourceContext(serializerContext, _model.OrderLine.AsReference(), new { ID = 21 });

            // Act
            Uri uri = entityContext.GenerateSelfLink(false);

            // Assert
            Assert.Equal("http://localhost/OrderLines(21)", uri.AbsoluteUri);
        }

        [Fact]
        public void GenerateSelfLink_ThrowsArgumentNull_EntityContext()
        {
            Assert.ThrowsArgumentNull(
                () => LinkGenerationHelpers.GenerateSelfLink(resourceContext: null, includeCast: false),
                "resourceContext");
        }

        [Fact]
        public void GenerateSelfLink_ThrowsArgument_IfUrlHelperIsNull()
        {
            ResourceContext context = new ResourceContext();

            Assert.ThrowsArgument(
                () => LinkGenerationHelpers.GenerateSelfLink(context, includeCast: false),
                "resourceContext",
                "The property 'Url' of ResourceContext cannot be null.");
        }

        [Fact]
        public void GenerateNavigationPropertyLink_ThrowsArgumentNull_EntityContext()
        {
            IEdmNavigationProperty navigationProperty = new Mock<IEdmNavigationProperty>().Object;

            Assert.ThrowsArgumentNull(
                () => LinkGenerationHelpers.GenerateNavigationPropertyLink(resourceContext: null, navigationProperty: navigationProperty, includeCast: false),
                "resourceContext");
        }

        [Fact]
        public void GenerateNavigationPropertyLink_ThrowsArgument_IfUrlHelperIsNull()
        {
            IEdmNavigationProperty navigationProperty = new Mock<IEdmNavigationProperty>().Object;
            ResourceContext context = new ResourceContext();

            Assert.ThrowsArgument(
                () => LinkGenerationHelpers.GenerateNavigationPropertyLink(context, navigationProperty, includeCast: false),
                "resourceContext",
                "The property 'Url' of ResourceContext cannot be null.");
        }

        [Fact]
        public void GenerateActionLinkForFeed_ThrowsArgumentNull_FeedContext()
        {
            // Arrange
            ResourceSetContext feedContext = null;
            IEdmAction action = new Mock<IEdmAction>().Object;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => feedContext.GenerateActionLink(action), "resourceSetContext");
        }

        [Fact]
        public void GenerateActionLinkForFeed_ThrowsArgumentNull_Action()
        {
            // Arrange
            ResourceSetContext resourceSetContext = new ResourceSetContext();

            // Act & Assert
            Assert.ThrowsArgumentNull(() => resourceSetContext.GenerateActionLink(action: null), "action");
        }

        [Fact]
        public void GenerateActionLinkForFeed_ThrowsActionNotBoundToCollectionOfEntity_IfActionHasNoParameters()
        {
            // Arrange
            ResourceSetContext context = new ResourceSetContext();
            Mock<IEdmAction> action = new Mock<IEdmAction>();
            action.Setup(a => a.Parameters).Returns(Enumerable.Empty<IEdmOperationParameter>());
            action.Setup(a => a.Name).Returns("SomeAction");

            // Act & Assert
            Assert.ThrowsArgument(
                () => context.GenerateActionLink(action.Object),
                "action",
                "The action 'SomeAction' is not bound to the collection of entity. Only actions that are bound to entities can have action links.");
        }

        [Fact]
        public void GenerateActionLinkForFeed_GeneratesLinkWithoutCast_IfEntitySetTypeMatchesActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            IEdmAction action = _model.Model.SchemaElements.OfType<IEdmAction>().First(a => a.Name == "UpgradeAll");
            Assert.NotNull(action); // Guard

            var context = new ResourceSetContext
            {
                Request = request,
                EntitySetBase = _model.Customers,
                Url = request.GetUrlHelper(),
            };

            // Act
            Uri link = context.GenerateActionLink(action);

            Assert.Equal("http://localhost/Customers/NS.UpgradeAll", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLinkForFeed_GeneratesLinkWithCast_IfEntitySetTypeDoesnotMatchActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            IEdmAction action = _model.Model.SchemaElements.OfType<IEdmAction>().First(a => a.Name == "UpgradeSpecialAll");
            Assert.NotNull(action); // Guard
            var context = new ResourceSetContext
            {
                Request = request,
                EntitySetBase = _model.Customers,
                Url = request.GetUrlHelper(),
            };

            // Act
            Uri link = context.GenerateActionLink(action);

            // Assert
            Assert.Equal("http://localhost/Customers/NS.SpecialCustomer/NS.UpgradeSpecialAll", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLinkForFeed_GeneratesLinkWithDownCast_IfElementTypeDerivesFromBindingParameterType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            IEdmAction action = _model.Model.SchemaElements.OfType<IEdmAction>().First(a => a.Name == "UpgradeAll");
            Assert.NotNull(action); // Guard
            IEdmEntitySet specialCustomers = new EdmEntitySet(_model.Container, "SpecialCustomers", _model.SpecialCustomer);

            var context = new ResourceSetContext
            {
                Request = request,
                EntitySetBase = specialCustomers,
                Url = request.GetUrlHelper(),
            };

            // Act
            Uri link = context.GenerateActionLink(action);

            // Assert
            Assert.Equal("http://localhost/SpecialCustomers/NS.Customer/NS.UpgradeAll", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_ThrowsArgumentNull_EntityContext()
        {
            ResourceContext entityContext = null;
            IEdmActionImport action = new Mock<IEdmActionImport>().Object;

            Assert.ThrowsArgumentNull(() => entityContext.GenerateActionLink(action.Action), "resourceContext");
        }

        [Fact]
        public void GenerateActionLink_ThrowsArgumentNull_Action()
        {
            ResourceContext entityContext = new ResourceContext();

            Assert.ThrowsArgumentNull(() => entityContext.GenerateActionLink(action: null), "action");
        }

        [Fact]
        public void GenerateActionLink_ThrowsActionNotBoundToEntity_IfActionHasNoParameters()
        {
            ResourceContext entityContext = new ResourceContext();
            Mock<IEdmAction> action = new Mock<IEdmAction>();
            action.Setup(a => a.Parameters).Returns(Enumerable.Empty<IEdmOperationParameter>());
            action.Setup(a => a.Name).Returns("SomeAction");

            Assert.ThrowsArgument(
                () => entityContext.GenerateActionLink(action.Object),
                "action",
                "The action 'SomeAction' is not bound to an entity. Only actions that are bound to entities can have action links.");
        }

        [Fact]
        public void GenerateActionLink_GeneratesLinkWithoutCast_IfEntitySetTypeMatchesActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.Customer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateActionLink(_model.UpgradeCustomer);

            Assert.Equal("http://localhost/Customers(42)/NS.upgrade", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_GeneratesLinkWithCast_IfEntitySetTypeDoesnotMatchActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateActionLink(_model.UpgradeSpecialCustomer);

            // Assert
            Assert.Equal("http://localhost/Customers(42)/NS.SpecialCustomer/NS.specialUpgrade", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_GeneratesLinkWithDownCast_IfElementTypeDerivesFromBindingParameterType()
        {
            // Arrange
            IEdmEntitySet specialCustomers = new EdmEntitySet(_model.Container, "SpecialCustomers", _model.SpecialCustomer);
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = specialCustomers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateActionLink(_model.UpgradeCustomer);

            // Assert
            Assert.Equal("http://localhost/SpecialCustomers(42)/NS.Customer/NS.upgrade", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_GeneratesLinkWithCast_IfEntitySetTypeDoesnotMatchActionEntityType_ForSingleton()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Mary, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateActionLink(_model.UpgradeSpecialCustomer);

            // Assert
            Assert.Equal("http://localhost/Mary/NS.SpecialCustomer/NS.specialUpgrade", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_GeneratesLinkWithDownCast_IfElementTypeDerivesFromBindingParameterType_ForSingleton()
        {
            // Arrange
            IEdmSingleton me = new EdmSingleton(_model.Container, "Me", _model.SpecialCustomer);
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = me, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateActionLink(_model.UpgradeCustomer);

            // Assert
            Assert.Equal("http://localhost/Me/NS.Customer/NS.upgrade", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateActionLink_ReturnsNull_ForContainment()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.OrderLines, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.OrderLine.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateActionLink(_model.Tag);

            // Assert
            Assert.Null(link);
        }

        [Fact]
        public void GenerateFunctionLinkForFeed_ThrowsArgumentNull_ResourceSetContext()
        {
            // Arrange
            ResourceSetContext resourceSetContext = null;
            IEdmFunction function = new Mock<IEdmFunction>().Object;

            // Act & Assert
            Assert.ThrowsArgumentNull(() => resourceSetContext.GenerateFunctionLink(function), "resourceSetContext");
        }

        [Fact]
        public void GenerateFunctionLinkForFeed_ThrowsArgumentNull_Function()
        {
            // Arrange
            ResourceSetContext feedContext = new ResourceSetContext();

            // Act & Assert
            Assert.ThrowsArgumentNull(() => feedContext.GenerateFunctionLink(function: null), "function");
        }

        [Fact]
        public void GenerateFunctionLinkForFeed_ThrowsFunctionNotBoundToCollectionOfEntity_IfFunctionHasNoParameters()
        {
            // Arrange
            ResourceSetContext context = new ResourceSetContext();
            Mock<IEdmFunction> function = new Mock<IEdmFunction>();
            function.Setup(a => a.Parameters).Returns(Enumerable.Empty<IEdmOperationParameter>());
            function.Setup(a => a.Name).Returns("SomeFunction");

            // Act & Assert
            Assert.ThrowsArgument(
                () => context.GenerateFunctionLink(function.Object),
                "function",
                "The function 'SomeFunction' is not bound to the collection of entity. Only functions that are bound to entities can have function links.");
        }

        [Fact]
        public void GenerateFunctionLinkForFeed_GeneratesLinkWithoutCast_IfEntitySetTypeMatchesActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            IEdmFunction function = _model.Model.SchemaElements.OfType<IEdmFunction>().First(a => a.Name == "IsAllUpgraded");
            Assert.NotNull(function); // Guard

            var context = new ResourceSetContext
            {
                Request = request,
                EntitySetBase = _model.Customers,
                Url = request.GetUrlHelper(),
            };

            // Act
            Uri link = context.GenerateFunctionLink(function);

            Assert.Equal("http://localhost/Customers/NS.IsAllUpgraded(param=@param)", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateFunctionLinkForFeed_GeneratesLinkWithCast_IfEntitySetTypeDoesnotMatchActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            IEdmFunction function = _model.Model.SchemaElements.OfType<IEdmFunction>().First(a => a.Name == "IsSpecialAllUpgraded");
            Assert.NotNull(function); // Guard
            var context = new ResourceSetContext
            {
                Request = request,
                EntitySetBase = _model.Customers,
                Url = request.GetUrlHelper(),
            };

            // Act
            Uri link = context.GenerateFunctionLink(function);

            // Assert
            Assert.Equal("http://localhost/Customers/NS.SpecialCustomer/NS.IsSpecialAllUpgraded(param=@param)", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateFunctionLinkForFeed_GeneratesLinkWithDownCast_IfElementTypeDerivesFromBindingParameterType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            IEdmFunction function = _model.Model.SchemaElements.OfType<IEdmFunction>().First(a => a.Name == "IsAllUpgraded");
            Assert.NotNull(function); // Guard
            IEdmEntitySet specialCustomers = new EdmEntitySet(_model.Container, "SpecialCustomers", _model.SpecialCustomer);

            var context = new ResourceSetContext
            {
                Request = request,
                EntitySetBase = specialCustomers,
                Url = request.GetUrlHelper(),
            };

            // Act
            Uri link = context.GenerateFunctionLink(function);

            // Assert
            Assert.Equal("http://localhost/SpecialCustomers/NS.Customer/NS.IsAllUpgraded(param=@param)", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateFunctionLink_GeneratesLinkWithoutCast_IfEntitySetTypeMatchesActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.Customer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateFunctionLink(_model.IsCustomerUpgraded);

            // Assert
            Assert.Equal("http://localhost/Customers(42)/NS.IsUpgradedWithParam(city=@city)", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateFunctionLink_GeneratesLinkWithCast_IfEntitySetTypeDoesnotMatchActionEntityType()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Customers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateFunctionLink(_model.IsSpecialCustomerUpgraded);

            // Assert
            Assert.Equal("http://localhost/Customers(42)/NS.SpecialCustomer/NS.IsSpecialUpgraded()", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateFunctionLink_GeneratesLinkWithDownCast_IfElementTypeDerivesFromBindingParameterType()
        {
            // Arrange
            IEdmEntitySet specialCustomers = new EdmEntitySet(_model.Container, "SpecialCustomers", _model.SpecialCustomer);
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = specialCustomers, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateFunctionLink(_model.IsCustomerUpgraded);

            // Assert
            Assert.Equal("http://localhost/SpecialCustomers(42)/NS.Customer/NS.IsUpgradedWithParam(city=@city)",
                link.AbsoluteUri);
        }

        [Fact]
        public void GenerateFunctionLink_GeneratesLinkWithCast_IfEntitySetTypeDoesnotMatchActionEntityType_ForSingleton()
        {
            // Arrange
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = _model.Mary, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateFunctionLink(_model.IsSpecialCustomerUpgraded);

            // Assert
            Assert.Equal("http://localhost/Mary/NS.SpecialCustomer/NS.IsSpecialUpgraded()", link.AbsoluteUri);
        }

        [Fact]
        public void GenerateFunctionLink_GeneratesLinkWithDownCast_IfElementTypeDerivesFromBindingParameterType_ForSingleton()
        {
            // Arrange
            IEdmSingleton me = new EdmSingleton(_model.Container, "Me", _model.SpecialCustomer);
            HttpRequestMessage request = GetODataRequest(_model.Model);
            var serializerContext = new ODataSerializerContext { Model = _model.Model, NavigationSource = me, Url = request.GetUrlHelper() };
            var entityContext = new ResourceContext(serializerContext, _model.SpecialCustomer.AsReference(), new { ID = 42 });

            // Act
            Uri link = entityContext.GenerateFunctionLink(_model.IsCustomerUpgraded);

            // Assert
            Assert.Equal("http://localhost/Me/NS.Customer/NS.IsUpgradedWithParam(city=@city)", link.AbsoluteUri);
        }

        private static HttpRequestMessage GetODataRequest(IEdmModel model)
        {
            HttpConfiguration configuration = new HttpConfiguration();
            string routeName = "Route";
            configuration.MapODataServiceRoute(routeName, null, model);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            request.SetConfiguration(configuration);
            request.EnableODataDependencyInjectionSupport(routeName);
            return request;
        }
    }
}
