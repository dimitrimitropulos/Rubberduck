﻿using System;
using Microsoft.Vbe.Interop;

namespace Rubberduck.SmartIndenter
{
    public interface IIndenter
    {
        event EventHandler<IndenterProgressEventArgs> ReportProgress;
        void IndentCurrentProcedure();
        void IndentCurrentModule();
        void Indent(VBProject project);
        void Indent(VBComponent module, bool reportProgress = true, int linesAlreadyRebuilt = 0);
        void Indent(VBComponent module, string procedureName, Selection selection, bool reportProgress = true, int linesAlreadyRebuilt = 0);
        void Indent(string[] lines, string moduleName, bool reportProgress = true, int linesAlreadyRebuilt = 0);
    }
}
