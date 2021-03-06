using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rubberduck.Parsing.Nodes;
using Rubberduck.VBEditor;
using Rubberduck.Parsing.Annotations;

namespace Rubberduck.Parsing.Symbols
{
    public class DeclarationFinder
    {
        private readonly IDictionary<QualifiedModuleName, CommentNode[]> _comments;
        private readonly IDictionary<QualifiedModuleName, IAnnotation[]> _annotations;
        private readonly IDictionary<string, Declaration[]> _declarationsByName;

        public DeclarationFinder(
            IReadOnlyList<Declaration> declarations, 
            IEnumerable<CommentNode> comments,
            IEnumerable<IAnnotation> annotations)
        {
            _comments = comments.GroupBy(node => node.QualifiedSelection.QualifiedName)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());
            _annotations = annotations.GroupBy(node => node.QualifiedSelection.QualifiedName)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());

            _declarationsByName = declarations.GroupBy(declaration => new
                {
                    IdentifierName = declaration.Project != null && 
                        declaration.DeclarationType == DeclarationType.Project
                            ? declaration.Project.Name
                            : declaration.IdentifierName
                })
                .ToDictionary(grouping => grouping.Key.IdentifierName, grouping => grouping.ToArray());
        }

        private readonly HashSet<Accessibility> _projectScopePublicModifiers =
            new HashSet<Accessibility>(new[]
            {
                Accessibility.Public,
                Accessibility.Global,
                Accessibility.Friend,
                Accessibility.Implicit,
            });

        public IEnumerable<CommentNode> ModuleComments(QualifiedModuleName module)
        {
            CommentNode[] result;
            if (_comments.TryGetValue(module, out result))
            {
                return result;
            }

            return new List<CommentNode>();
        }

        public IEnumerable<IAnnotation> ModuleAnnotations(QualifiedModuleName module)
        {
            IAnnotation[] result;
            if (_annotations.TryGetValue(module, out result))
            {
                return result;
            }

            return new List<IAnnotation>();
        }

        public IEnumerable<Declaration> MatchTypeName(string name)
        {
            return MatchName(name).Where(declaration =>
                declaration.DeclarationType == DeclarationType.Class ||
                declaration.DeclarationType == DeclarationType.UserDefinedType ||
                declaration.DeclarationType == DeclarationType.Enumeration);
        }

        public IEnumerable<Declaration> MatchName(string name)
        {
            Declaration[] result;
            if (_declarationsByName.TryGetValue(name, out result))
            {
                return result;
            }
            if (_declarationsByName.TryGetValue("_" + name, out result))
            {
                return result;
            }
            if (_declarationsByName.TryGetValue("I" + name, out result))
            {
                return result;
            }
            if (_declarationsByName.TryGetValue("_I" + name, out result))
            {
                return result;
            }
            return new List<Declaration>();
        }

        public Declaration FindProject(Declaration currentScope, string name)
        {
            Declaration result = null;
            try
            {
                result = MatchName(name).SingleOrDefault(project => project.DeclarationType == DeclarationType.Project 
                    && (currentScope == null || project.ProjectName == currentScope.ProjectName));
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine("Multiple matches found for project '{0}'.\n{1}", name, exception);
            }

            return result;
        }

        public Declaration FindStdModule(Declaration parent, string name, bool includeBuiltIn = false)
        {
            Declaration result = null;
            try
            {
                var matches = MatchName(name);
                result = matches.SingleOrDefault(declaration => declaration.DeclarationType == DeclarationType.Module
                    && (parent == null || parent.Equals(declaration.ParentDeclaration))
                    && (includeBuiltIn || !declaration.IsBuiltIn));
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine("Multiple matches found for std.module '{0}'.\n{1}", name, exception);
            }

            return result;
        }

        public Declaration FindUserDefinedType(Declaration parent, string name, bool includeBuiltIn = false)
        {
            Declaration result = null;
            try
            {
                var matches = MatchName(name);
                result = matches.SingleOrDefault(declaration => declaration.DeclarationType == DeclarationType.UserDefinedType
                    && (parent == null || parent.Equals(declaration.ParentDeclaration))
                    && (includeBuiltIn || !declaration.IsBuiltIn));
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Multiple matches found for user-defined type '{0}'.\n{1}", name, exception);
            }

            return result;
        }

        public Declaration FindEnum(Declaration parent, string name, bool includeBuiltIn = false)
        {
            Declaration result = null;
            try
            {
                var matches = MatchName(name);
                result = matches.SingleOrDefault(declaration => declaration.DeclarationType == DeclarationType.Enumeration
                    && (parent == null || parent.Equals(declaration.ParentDeclaration))
                    && (includeBuiltIn || !declaration.IsBuiltIn));
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Multiple matches found for enum type '{0}'.\n{1}", name, exception);
            }

            return result;
        }

        public Declaration FindClass(Declaration parent, string name, bool includeBuiltIn = false)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            Declaration result = null;
            try
            {
                result = MatchName(name).SingleOrDefault(declaration => declaration.DeclarationType == DeclarationType.Class
                    && parent.Equals(declaration.ParentDeclaration)
                    && (includeBuiltIn || !declaration.IsBuiltIn));
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine("Multiple matches found for class '{0}'.\n{1}", name, exception);
            }

            return result;
        }
    }
}