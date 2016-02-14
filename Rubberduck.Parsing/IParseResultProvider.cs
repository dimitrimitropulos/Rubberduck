﻿using System;
using System.Collections.Generic;
using Microsoft.Vbe.Interop;
using Rubberduck.Parsing.VBA;

namespace Rubberduck.Parsing
{
    public class ParseStartedEventArgs : EventArgs
    {
        public ParseStartedEventArgs(IEnumerable<string> projectNames)
        {
            _projectNames = projectNames;
        }

        private readonly IEnumerable<string> _projectNames;
        public IEnumerable<string> ProjectNames { get { return _projectNames; } }
    }

    public class ResolutionProgressEventArgs : EventArgs
    {
        private readonly VBComponent _component;
        private readonly decimal _percentProgress;

        public ResolutionProgressEventArgs(VBComponent component, decimal percentProgress)
        {
            _component = component;
            _percentProgress = percentProgress;
        }

        public VBComponent Component { get { return _component; } }
        public decimal PercentProgress { get { return _percentProgress; } }
    }

    public class ParseProgressEventArgs : EventArgs
    {
        private readonly VBComponent _component;
        private readonly ParserState _state;

        public ParseProgressEventArgs(VBComponent component, ParserState state)
        {
            _component = component;
            _state = state;
        }

        public VBComponent Component { get { return _component; } }
        public ParserState State { get { return _state; } }
    }
}
