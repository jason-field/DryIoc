﻿using System;
using DryIoc.AttributedRegistration.UnitTests.CUT;
using NUnit.Framework;

namespace DryIoc.AttributedRegistration.UnitTests
{
    [TestFixture]
    public class ImportOrExportTests
    {
        [Test]
        public void I_can_export_ctor_param_service_on_resolve()
        {
            var container = new Container(AttributedRegistrator.DefaultSetup);
            container.RegisterExported(typeof(NativeUser));

            var user = container.Resolve<NativeUser>();

            Assert.That(user.Tool, Is.Not.Null);
        }

        [Test]
        public void I_can_specify_constructor_while_exporting_once_a_ctor_param_service()
        {
            var container = new Container(AttributedRegistrator.DefaultSetup);
            container.RegisterExported(typeof(HomeUser));

            var user = container.Resolve<HomeUser>();

            Assert.That(user.Tool.Message, Is.EqualTo("blah"));
        }

        [Test]
        public void I_can_specify_metadata()
        {
            var container = new Container(AttributedRegistrator.DefaultSetup);
            container.RegisterExported(typeof(MyCode));

            var code = container.Resolve<MyCode>();

            Assert.That(code.Tool, Is.Not.Null);
            Assert.That(code.ToolMeta, Is.EqualTo(MineMeta.Green));
        }

        [Test]
        public void I_can_import_or_export_fields_and_properties_as_well()
        {
            var container = new Container();
            container.ResolutionRules.UseImportExportAttributes();
            container.RegisterExported(typeof(ServiceWithFieldAndProperty));

            var service = container.Resolve<ServiceWithFieldAndProperty>();

            Assert.That(service.Field, Is.InstanceOf<AnotherService>());
            Assert.That(service.Property, Is.InstanceOf<AnotherService>());
        }

        [Test]
        public void When_something_else_than_Import_specified_It_should_throw()
        {
            var container = new Container(AttributedRegistrator.DefaultSetup);
            container.RegisterExported(typeof(WithUnregisteredExternalEdependency));

            Assert.Throws<ContainerException>(() => 
                container.Resolve<WithUnregisteredExternalEdependency>());
        }
    }

    [ExportAll]
    public class WithUnregisteredExternalEdependency
    {
        public ExternalTool Tool { get; set; }

        public WithUnregisteredExternalEdependency([SomeOther]ExternalTool tool)
        {
            Tool = tool;
        }
    }

    public class SomeOtherAttribute : Attribute
    {
    }
}