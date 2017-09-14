﻿using NUnit.Framework;

namespace DryIoc.IssuesTests
{
    [TestFixture]
    public class Issue519_Dependency_of_singleton_not_working_when_using_child_container
    {
        [Test, Ignore("to fix")]
        public void Test()
        {
            var c = new Container();
            c.Register(typeof(IService), typeof(Service), Reuse.Singleton);

            var childContainer = c.WithRegistrationsCopy().OpenScope();

            childContainer.UseInstance<IServiceDependency>(new ServiceDependency());

            Assert.Throws<ContainerException>(() =>
                childContainer.Resolve<IService>());
        }

        public interface IService
        {
            IServiceDependency Dependency { get; }
        }

        public class Service : IService
        {
            public Service(IServiceDependency dependency)
            {
                Dependency = dependency;
            }

            public IServiceDependency Dependency { get; set; }
        }

        public interface IServiceDependency { }

        public class ServiceDependency : IServiceDependency { }
    }
}