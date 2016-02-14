﻿using Microsoft.Vbe.Interop;
using Rubberduck.VBEditor.VBEInterfaces.RubberduckCodePane;

namespace Rubberduck.VBEditor
{
    public class ActiveCodePaneEditor : IActiveCodePaneEditor
    {
        private readonly VBE _vbe;
        private readonly ICodePaneWrapperFactory _wrapperFactory;

        public ActiveCodePaneEditor(VBE vbe, ICodePaneWrapperFactory wrapperFactory)
        {
            _vbe = vbe;
            _wrapperFactory = wrapperFactory;
        }

        private CodeModule Editor { get { return _vbe.ActiveCodePane == null ? null : _vbe.ActiveCodePane.CodeModule; } }

        public QualifiedSelection? GetSelection()
        {
            if (Editor == null)
            {
                return null;
            }

            var codePane = _wrapperFactory.Create(Editor.CodePane);
            return new QualifiedSelection(new QualifiedModuleName(codePane.CodeModule.Parent), codePane.Selection);
        }

        public void SetSelection(Selection selection)
        {
            if (Editor == null)
            {
                return;
            }

            var codePane = _wrapperFactory.Create(Editor.CodePane);
            codePane.Selection = selection;
        }

        public void SetSelection(QualifiedSelection selection)
        {
            _vbe.ActiveCodePane = selection.QualifiedName.Component.CodeModule.CodePane;
            SetSelection(selection.Selection);
        }

        public string GetLines(Selection selection)
        {
            // ReSharper disable once UseIndexedProperty
            return Editor.get_Lines(selection.StartLine, selection.LineCount);
        }

        public string GetSelectedProcedureScope(Selection selection)
        {
            var moduleName = Editor.Name;
            var projectName = Editor.Parent.Collection.Parent.Name;
            var parentScope = projectName + '.' + moduleName;

            vbext_ProcKind kind;
            var procStart = Editor.get_ProcOfLine(selection.StartLine, out kind);
            var procEnd = Editor.get_ProcOfLine(selection.EndLine, out kind);

            return procStart == procEnd
                ? parentScope + '.' + procStart
                : null;
        }

        public void DeleteLines(Selection selection)
        {
            Editor.DeleteLines(selection.StartLine, selection.LineCount);
        }

        public void ReplaceLine(int line, string content)
        {
            Editor.ReplaceLine(line, content);
        }

        public void InsertLines(int line, string content)
        {
            Editor.InsertLines(line, content);
        }
    }
}