using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DGSPlayer
{
    public delegate void EventDelegate();
    public class MainViewCommand : ICommand
    {
        EventDelegate eventDelegate;
        public MainViewCommand(EventDelegate eventDelegate)
        {
            this.eventDelegate = eventDelegate;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this.eventDelegate();
        }
    }
}
