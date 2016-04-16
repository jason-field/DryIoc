﻿using System.Linq;
using DryIoc;
using NUnit.Framework;
using DryIoc.MefAttributedModel.UnitTests.CUT;

namespace DryIocZero.UnitTests
{
    [TestFixture]
    public class ZeroContainerTests
    {
        [Test]
        public void Can_Register_default_delegate()
        {
            var container = new Container();
            container.RegisterDelegate(r => new Potato());

            var potato = container.Resolve(typeof(Potato), false);
            
            Assert.IsNotNull(potato);
        }

        [Test]
        public void Can_Register_keyed_delegate()
        {
            var container = new Container();
            container.RegisterDelegate(r => new Potato(), "mashed");

            var potato = container.Resolve(typeof(Potato), "mashed");

            Assert.IsNotNull(potato);
        }

        [Test]
        public void Should_throw_if_null_keyed_service_is_nor_registered_nor_generated()
        {
            var container = new Container();

            var ex = Assert.Throws<ContainerException>(() => 
                container.Resolve(typeof(Potato), null));

            Assert.AreEqual(Error.UnableToResolveDefaultService, ex.Error);
        }

        [Test]
        public void Should_throw_if_non_null_keyed_service_is_nor_registered_nor_generated()
        {
            var container = new Container();

            var ex = Assert.Throws<ContainerException>(() =>
                container.Resolve(typeof(Potato), "x"));

            Assert.AreEqual(Error.UnableToResolveKeyedService, ex.Error);
        }

        [Test]
        public void Can_Register_Instance()
        {
            var container = new Container();
            var potato = new Potato();
            container.RegisterInstance(potato);

            var resolvedPotato = container.Resolve(typeof(Potato), false);

            Assert.AreSame(potato, resolvedPotato);
        }

        internal class Potato {}

        [Test]
        public void Can_open_scope()
        {
            var container = new Container();
            container.Register(typeof(Potato), (r, scope) => new Potato());
            using (var scope = container.OpenScope())
            {
                var potato = scope.Resolve(typeof(Potato), false);
                Assert.IsNotNull(potato);
            }
        }

        [Test]
        public void Dispose_should_remove_registrations()
        {
            var container = new Container();
            container.Register(typeof(Potato), (r, scope) => new Potato());
            container.Dispose();
            Assert.Throws<ContainerException>(() => container.Resolve(typeof(Potato), false));
        }

        [Test]
        public void Can_resolve_singleton()
        {
            var container = new Container();

            var service = container.Resolve(typeof(ISomeDb), false);
            Assert.IsNotNull(service);
            Assert.AreSame(service, container.Resolve(typeof(ISomeDb), false));
        }

        [Test]
        public void Can_resolve_singleton_with_key()
        {
            var container = new Container();

            var service = container.Resolve(typeof(IMultiExported), "c");
            Assert.IsNotNull(service);
            Assert.AreSame(service, container.Resolve(typeof(IMultiExported), "c"));
        }

        [Test]
        public void Will_throw_for_not_registered_service_type()
        {
            var container = new Container();

            var ex = Assert.Throws<ContainerException>(
                () => container.Resolve(typeof(NotRegistered), false));

            Assert.AreEqual(ex.Error, Error.UnableToResolveDefaultService);
        }

        [Test]
        public void Will_return_null_for_not_registered_service_type_with_IfUnresolved_option()
        {
            var container = new Container();

            var nullService = container.Resolve(typeof(NotRegistered), true);

            Assert.IsNull(nullService);
        }

        [Test]
        public void Can_resolve_many()
        {
            var container = new Container();

            var handlers = container.ResolveMany(typeof(ISomeDb)).Cast<ISomeDb>().ToArray<ISomeDb>();

            Assert.AreEqual(1, handlers.Length);
        }

        [Test]
        public void Can_resolve_generated_only_service()
        {
            var container = new Container();
            var b = container.ResolveGeneratedOrGetDefault(typeof(B));
            Assert.IsNotNull(b);

            var nr = container.ResolveGeneratedOrGetDefault(typeof(NotRegistered));
            Assert.IsNull(nr);
        }

        [Test]
        public void Can_resolve_generated_only_keyed_service()
        {
            var container = new Container();
            var m = container.ResolveGeneratedOrGetDefault(typeof(MultiExported), "b");
            Assert.IsNotNull(m);

            var b = container.ResolveGeneratedOrGetDefault(typeof(B), "b");
            Assert.IsNull(b);
        }

        [Test]
        public void Can_resolve_generated_only_many_services()
        {
            var container = new Container();
            var ms = container.ResolveManyGeneratedOrGetEmpty(typeof(IMultiExported));
            Assert.IsTrue(ms.Any());

            var ns = container.ResolveManyGeneratedOrGetEmpty(typeof(NotRegistered));
            Assert.IsTrue(!ns.Any());
        }

        [Test]
        public void Can_resolve_many_of_both_generated_and_runtime_default_and_keyed_services()
        {
            var container = new Container();

            container.Register(typeof(IMultiExported), (context, scope) => new AnotherMulti());
            container.Register(typeof(IMultiExported), "another", (context, scope) => new AnotherMulti());

            var ms = container.ResolveMany(typeof(IMultiExported)).Cast<IMultiExported>().ToArray();
            Assert.AreEqual(2, ms.Count(m => m.GetType() == typeof(AnotherMulti)));
            Assert.Greater(ms.Length, 2);
        }

        [Test]
        public void Can_resolve_many_of_both_generated_and_runtime_keyed_service_with_specified_key()
        {
            var container = new Container();

            container.Register(typeof(IMultiExported), "c", (context, scope) => new AnotherMulti());

            var ms = container.ResolveMany(typeof(IMultiExported), "c").Cast<IMultiExported>().ToArray();
            Assert.AreEqual(1, ms.Count(m => m.GetType() == typeof(AnotherMulti)));
            Assert.AreEqual(2, ms.Length);
        }

        [Test]
        public void Should_exclude_composite_key_from_many()
        {
            var container = new Container();

            var ms = container.ResolveMany(typeof(IMultiExported), compositeParentKey: "c").Cast<IMultiExported>().ToArray();
            Assert.AreEqual(2, ms.Length);
        }

        [Test]
        public void Can_register_into_resolution_scope_at_runtime()
        {
            var container = new Container();
            var yID = container.GetNextFactoryID();
            container.Register(typeof(X), (context, scope) => new X(
                    (Y)context.Scopes.GetOrCreateResolutionScope(ref scope, typeof(X), null)
                        .GetOrAdd(yID, () => new Y()),
                    (Y)context.Scopes.GetOrCreateResolutionScope(ref scope, typeof(X), null)
                        .GetOrAdd(yID, () => new Y())));

            var x = container.Resolve<X>();
            Assert.IsNotNull(x.Y1);
            Assert.AreSame(x.Y1, x.Y2);
        }

        [Test]
        public void Can_register_into_matching_resolution_scope_at_runtime()
        {
            var container = new Container();
            var yID = container.GetNextFactoryID();
            container.Register(typeof(X), "a", (context, scope) => new X(
                    (Y)context.Scopes.GetMatchingResolutionScope(
                        context.Scopes.GetOrCreateResolutionScope(ref scope, typeof(X), "a"),
                        typeof(X), "a", false, true)
                        .GetOrAdd(yID, () => new Y()),
                    (Y)context.Scopes.GetOrCreateResolutionScope(ref scope, typeof(X), "a")
                        .GetOrAdd(yID, () => new Y())));

            var x = container.Resolve<X>(serviceKey: "a");
            Assert.IsNotNull(x.Y1);
            Assert.AreSame(x.Y1, x.Y2);
        }

        internal class AnotherMulti : IMultiExported { }

        internal class NotRegistered {}

        internal class X
        {
            public Y Y1 { get; private set; }

            public Y Y2 { get; private set; }

            public X(Y y1, Y y2)
            {
                Y1 = y1;
                Y2 = y2;
            }
        }

        internal class Y
        {
        }
    }
}