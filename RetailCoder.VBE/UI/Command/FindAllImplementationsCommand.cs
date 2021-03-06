using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Vbe.Interop;
using Rubberduck.Common;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.UI.Controls;
using Rubberduck.VBEditor;
using Rubberduck.VBEditor.VBEInterfaces.RubberduckCodePane;

namespace Rubberduck.UI.Command
{
    /// <summary>
    /// A command that finds all implementations of a specified method, or of the active interface module.
    /// </summary>
    [ComVisible(false)]
    public class FindAllImplementationsCommand : CommandBase
    {
        private readonly INavigateCommand _navigateCommand;
        private readonly IMessageBox _messageBox;
        private readonly RubberduckParserState _state;
        private readonly ISearchResultsWindowViewModel _viewModel;
        private readonly SearchResultPresenterInstanceManager _presenterService;
        private readonly VBE _vbe;

        public FindAllImplementationsCommand(INavigateCommand navigateCommand, IMessageBox messageBox, RubberduckParserState state, VBE vbe, ISearchResultsWindowViewModel viewModel, SearchResultPresenterInstanceManager presenterService)
        {
            _navigateCommand = navigateCommand;
            _messageBox = messageBox;
            _state = state;
            _vbe = vbe;
            _viewModel = viewModel;
            _presenterService = presenterService;
        }

        public override bool CanExecute(object parameter)
        {
            if (_vbe.ActiveCodePane == null && _state.Status != ParserState.Ready)
            {
                return false;
            }

            // todo: make this work for Code/Project Explorer context menus too (may require a new command implementation)
            var target = FindTarget(parameter);
            var canExecute = target != null;

            Debug.WriteLine("{0}.CanExecute evaluates to {1}", GetType().Name, canExecute);
            return canExecute;
        }

        public override void Execute(object parameter)
        {
            if (_state.Status != ParserState.Ready)
            {
                return;
            }

            var declaration = FindTarget(parameter);
            if (declaration == null)
            {
                return;
            }

            var viewModel = CreateViewModel(declaration);
            if (!viewModel.SearchResults.Any())
            {
                _messageBox.Show(string.Format(RubberduckUI.AllReferences_NoneFound, declaration.IdentifierName), RubberduckUI.Rubberduck, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (viewModel.SearchResults.Count == 1)
            {
                _navigateCommand.Execute(viewModel.SearchResults.Single().GetNavigationArgs());
                return;
            }

            _viewModel.AddTab(viewModel);
            _viewModel.SelectedTab = viewModel;

            try
            {
                var presenter = _presenterService.Presenter(_viewModel);
                presenter.Show();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private SearchResultsViewModel CreateViewModel(Declaration target)
        {
            var results = FindImplementations(target).Select(declaration =>
                new SearchResultItem(
                    declaration.ParentScopeDeclaration,
                    new NavigateCodeEventArgs(declaration.QualifiedName.QualifiedModuleName, declaration.Selection),
                    declaration.QualifiedName.QualifiedModuleName.Component.CodeModule.Lines[declaration.Selection.StartLine, 1].Trim()));

            var viewModel = new SearchResultsViewModel(_navigateCommand,
                string.Format(RubberduckUI.SearchResults_AllImplementationsTabFormat, target.IdentifierName), target, results);

            return viewModel;
        }

        private Declaration FindTarget(object parameter)
        {
            var declaration = parameter as Declaration;
            if (declaration != null)
            {
                return declaration;
            }

            return _state.FindSelectedDeclaration(_vbe.ActiveCodePane);
        }

        private IEnumerable<Declaration> FindImplementations(Declaration target)
        {
            var items = _state.AllDeclarations;
            string name;
            var implementations = (target.DeclarationType == DeclarationType.Class
                ? FindAllImplementationsOfClass(target, items, out name)
                : FindAllImplementationsOfMember(target, items, out name)) ?? new List<Declaration>();

            return implementations;
        }

        private IEnumerable<Declaration> FindAllImplementationsOfClass(Declaration target, IEnumerable<Declaration> declarations, out string name)
        {
            if (target.DeclarationType != DeclarationType.Class)
            {
                name = string.Empty;
                return null;
            }

            var identifiers = declarations as IList<Declaration> ?? declarations.ToList();

            var result = target.References
                .Where(reference => reference.Context.Parent is VBAParser.ImplementsStmtContext)
                .SelectMany(reference => identifiers.Where(identifier => identifier.IdentifierName == reference.QualifiedModuleName.ComponentName))
                .ToList();

            name = target.ComponentName;
            return result;
        }

        private IEnumerable<Declaration> FindAllImplementationsOfMember(Declaration target, IEnumerable<Declaration> declarations, out string name)
        {
            if (!target.DeclarationType.HasFlag(DeclarationType.Member))
            {
                name = string.Empty;
                return null;
            }

            var items = declarations as IList<Declaration> ?? declarations.ToList();

            var isInterface = items.FindInterfaces()
                .Select(i => i.QualifiedName.QualifiedModuleName.ToString())
                .Contains(target.QualifiedName.QualifiedModuleName.ToString());

            if (isInterface)
            {
                name = target.ComponentName + "." + target.IdentifierName;
                return items.FindInterfaceImplementationMembers(target.IdentifierName)
                       .Where(item => item.IdentifierName == target.ComponentName + "_" + target.IdentifierName);
            }

            var member = items.FindInterfaceMember(target);
            name = member.ComponentName + "." + member.IdentifierName;
            return items.FindInterfaceImplementationMembers(member.IdentifierName)
                   .Where(item => item.IdentifierName == member.ComponentName + "_" + member.IdentifierName);
        }
    }
}