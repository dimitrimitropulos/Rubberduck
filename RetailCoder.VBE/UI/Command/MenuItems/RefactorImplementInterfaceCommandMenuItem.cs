using System.Windows.Input;
using Rubberduck.Parsing.VBA;
using Rubberduck.UI.Command.MenuItems.ParentMenus;

namespace Rubberduck.UI.Command.MenuItems
{
    public class RefactorImplementInterfaceCommandMenuItem : CommandMenuItemBase
    {
        public RefactorImplementInterfaceCommandMenuItem(ICommand command) 
            : base(command)
        {
        }

        public override string Key { get { return "RefactorMenu_ImplementInterface"; } }
        public override int DisplayOrder { get { return (int)RefactoringsMenuItemDisplayOrder.ImplementInterface; } }

        public override bool EvaluateCanExecute(RubberduckParserState state)
        {
            return state.Status == ParserState.Ready;
        }
    }
}