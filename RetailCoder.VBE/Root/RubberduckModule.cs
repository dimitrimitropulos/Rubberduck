﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Vbe.Interop;
using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Extensions.NamedScope;
using Ninject.Modules;
using Rubberduck.Common;
using Rubberduck.Inspections;
using Rubberduck.Navigation.CodeExplorer;
using Rubberduck.Parsing;
using Rubberduck.Parsing.VBA;
using Rubberduck.Settings;
using Rubberduck.SmartIndenter;
using Rubberduck.UI;
using Rubberduck.UI.CodeExplorer;
using Rubberduck.UI.CodeInspections;
using Rubberduck.UI.Command;
using Rubberduck.UI.Command.MenuItems;
using Rubberduck.UI.ToDoItems;
using Rubberduck.UI.UnitTesting;
using Rubberduck.VBEditor.VBEHost;

namespace Rubberduck.Root
{
    public class RubberduckModule : NinjectModule
    {
        private readonly IKernel _kernel;
        private readonly VBE _vbe;
        private readonly AddIn _addin;

        public RubberduckModule(IKernel kernel, VBE vbe, AddIn addin)
        {
            _kernel = kernel;
            _vbe = vbe;
            _addin = addin;
        }

        public override void Load()
        {
            Debug.Print("in RubberduckModule.Load()");

            // bind VBE and AddIn dependencies to host-provided instances.
            _kernel.Bind<VBE>().ToConstant(_vbe);
            _kernel.Bind<AddIn>().ToConstant(_addin);
            _kernel.Bind<RubberduckParserState>().ToSelf().InSingletonScope();
            
            BindCodeInspectionTypes();

            var assemblies = new[]
            {
                Assembly.GetExecutingAssembly(),
                Assembly.GetAssembly(typeof(IHostApplication)),
                Assembly.GetAssembly(typeof(IRubberduckParser)),
                Assembly.GetAssembly(typeof(IIndenter))
            };

            ApplyConfigurationConvention(assemblies);
            ApplyDefaultInterfacesConvention(assemblies);
            ApplyAbstractFactoryConvention(assemblies);

            Rebind<IIndenter>().To<Indenter>().InSingletonScope();
            Rebind<IIndenterSettings>().To<IndenterSettings>();
            Bind<TestExplorerModelBase>().To<StandardModuleTestExplorerModel>().InSingletonScope();
            Rebind<IRubberduckParser>().To<RubberduckParser>().InSingletonScope();

            Bind<IPresenter>().To<TestExplorerDockablePresenter>()
                .WhenInjectedInto<TestExplorerCommand>()
                .InSingletonScope()
                .WithConstructorArgument<IDockableUserControl>(new TestExplorerWindow { ViewModel = _kernel.Get<TestExplorerViewModel>() });

            Bind<IPresenter>().To<CodeInspectionsDockablePresenter>()
                .WhenInjectedInto<RunCodeInspectionsCommand>()
                .InSingletonScope()
                .WithConstructorArgument<IDockableUserControl>(new CodeInspectionsWindow { ViewModel = _kernel.Get<InspectionResultsViewModel>() });
            
            Bind<IPresenter>().To<CodeExplorerDockablePresenter>()
                .WhenInjectedInto<CodeExplorerCommand>()
                .InSingletonScope()
                .WithConstructorArgument<IDockableUserControl>(new CodeExplorerWindow { ViewModel = _kernel.Get<CodeExplorerViewModel>() });

            Bind<IPresenter>().To<ToDoExplorerDockablePresenter>()
                .WhenInjectedInto<ToDoExplorerCommand>()
                .InSingletonScope()
                .WithConstructorArgument<IDockableUserControl>(new ToDoExplorerWindow { ViewModel = _kernel.Get<ToDoExplorerViewModel>() });

            //BindWindowsHooks();
            Debug.Print("completed RubberduckModule.Load()");
        }

        private void BindWindowsHooks()
        {
            _kernel.Rebind<ITimerHook>().To<TimerHook>()
                .InSingletonScope()
                .WithConstructorArgument("mainWindowHandle", (IntPtr)_vbe.MainWindow.HWnd);

            _kernel.Rebind<IRubberduckHooks>().To<RubberduckHooks>()
                .InSingletonScope()
                .WithConstructorArgument("mainWindowHandle", (IntPtr)_vbe.MainWindow.HWnd);
        }

        private void ApplyDefaultInterfacesConvention(IEnumerable<Assembly> assemblies)
        {
            _kernel.Bind(t => t.From(assemblies)
                .SelectAllClasses()
                // inspections & factories have their own binding rules
                .Where(type => !type.Name.EndsWith("Factory") && !type.GetInterfaces().Contains(typeof(IInspection)))
                .BindDefaultInterface()
                .Configure(binding => binding.InCallScope())); // TransientScope wouldn't dispose disposables
        }

        // note: settings namespace classes are injected in singleton scope
        private void ApplyConfigurationConvention(IEnumerable<Assembly> assemblies)
        {
            _kernel.Bind(t => t.From(assemblies)
                .SelectAllClasses()
                .InNamespaceOf<Configuration>()
                .BindAllInterfaces()
                .Configure(binding => binding.InSingletonScope()));
        }

        // note convention: abstract factory interface names end with "Factory".
        private void ApplyAbstractFactoryConvention(IEnumerable<Assembly> assemblies)
        {
            _kernel.Bind(t => t.From(assemblies)
                .SelectAllInterfaces()
                .Where(type => type.Name.EndsWith("Factory")) 
                .BindToFactory()
                .Configure(binding => binding.InSingletonScope()));
        }

        // note: InspectionBase implementations are discovered in the Rubberduck assembly via reflection.
        private void BindCodeInspectionTypes()
        {
            var inspections = Assembly.GetExecutingAssembly()
                                      .GetTypes()
                                      .Where(type => type.BaseType == typeof (InspectionBase));

            // multibinding for IEnumerable<IInspection> dependency
            foreach (var inspection in inspections)
            {
                _kernel.Bind<IInspection>().To(inspection).InSingletonScope();
            }
        }
    }
}
