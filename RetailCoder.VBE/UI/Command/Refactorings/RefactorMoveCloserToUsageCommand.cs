﻿using Microsoft.Vbe.Interop;
using Rubberduck.Parsing.VBA;
using Rubberduck.Refactorings.MoveCloserToUsage;
using Rubberduck.VBEditor;
using Rubberduck.VBEditor.VBEInterfaces.RubberduckCodePane;

namespace Rubberduck.UI.Command.Refactorings
{
    public class RefactorMoveCloserToUsageCommand : RefactorCommandBase
    {
        private readonly RubberduckParserState _state;
        private readonly ICodePaneWrapperFactory _wrapperWrapperFactory;

        public RefactorMoveCloserToUsageCommand(VBE vbe, RubberduckParserState state, IActiveCodePaneEditor editor, ICodePaneWrapperFactory wrapperWrapperFactory)
            : base(vbe, editor)
        {
            _state = state;
            _wrapperWrapperFactory = wrapperWrapperFactory;
        }

        public override void Execute(object parameter)
        {
            if (Vbe.ActiveCodePane == null)
            {
                return;
            }
            var codePane = _wrapperWrapperFactory.Create(Vbe.ActiveCodePane);
            var selection = new QualifiedSelection(new QualifiedModuleName(codePane.CodeModule.Parent), codePane.Selection);

            var refactoring = new MoveCloserToUsageRefactoring(_state, Editor, new MessageBox());
            refactoring.Refactor(selection);
        }
    }
}