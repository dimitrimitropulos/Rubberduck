﻿
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Rubberduck.API
{
    [ComVisible(true)]
    public interface IIdentifierReference
    {
        Declaration Declaration { get; }
        Declaration ParentScope { get; }
        Declaration ParentNonScoping { get; }
        int StartLine { get; }
        int StartColumn { get; }
        int EndLine { get; }
        int EndColumn { get; }
    }

    [ComVisible(true)]
    [Guid(ClassId)]
    [ProgId(ProgId)]
    [ComDefaultInterface(typeof(IIdentifierReference))]
    [EditorBrowsable(EditorBrowsableState.Always)]
    public class IdentifierReference : IIdentifierReference
    {
        private const string ClassId = "57F78E64-8ADF-4D81-A467-A0139B877D14";
        private const string ProgId = "Rubberduck.IdentifierReference";

        private readonly Parsing.Symbols.IdentifierReference _reference;

        public IdentifierReference(Parsing.Symbols.IdentifierReference reference)
        {
            _reference = reference;
        }

        private Declaration _declaration;
        public Declaration Declaration
        {
            get { return _declaration ?? (_declaration = new Declaration(_reference.Declaration)); }
        }

        private Declaration _parentScoping;
        public Declaration ParentScope
        {
            get { return _parentScoping ?? (_parentScoping = new Declaration(_reference.ParentScoping)); }
        }

        private Declaration _parentNonScoping;
        public Declaration ParentNonScoping
        {
            get { return _parentNonScoping ?? (_parentNonScoping = new Declaration(_reference.ParentNonScoping)); }
        }

        public int StartLine { get { return _reference.Selection.StartLine; } }
        public int EndLine { get { return _reference.Selection.EndLine; } }
        public int StartColumn { get { return _reference.Selection.StartColumn; } }
        public int EndColumn { get { return _reference.Selection.EndColumn; } }
    }
}
