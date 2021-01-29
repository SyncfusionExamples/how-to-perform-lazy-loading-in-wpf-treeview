using Syncfusion.UI.Xaml.TreeView;
using Syncfusion.UI.Xaml.TreeView.Engine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WPF_TreeView_Lazy_Loading
{
    /// <summary>
    /// ViewModel class that implements <see cref="Command"/> for load on demand. 
    /// </summary>
    public class ViewModel
    {
        public ObservableCollection<Directory> Directories { get; set; }
        
        public ICommand TreeViewLoadOnDemandCommand { get; set; }

        public ViewModel()
        {
            this.Directories = this.GetDrives();
            TreeViewLoadOnDemandCommand = new LoadOnDemandCommand(ExecuteOnDemandLoading, CanExecuteOnDemandLoading); 
        }         

        /// <summary>
        /// CanExecute method is called before expanding and initialization of node. Returns whether the node has child nodes or not.
        /// Based on return value, expander visibility of the node is handled.  
        /// </summary>
        /// <param name="sender">TreeViewNode is passed as default parameter </param>
        /// <returns>Returns true, if the specified node has child items to load on demand and expander icon is displayed for that node, else returns false and icon is not displayed.</returns>
        private bool CanExecuteOnDemandLoading(object sender)
        {
            var hasChildNodes = ((sender as TreeViewNode).Content as Directory).HasChildNodes;
            if (hasChildNodes)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Execute method is called when any item is requested for load-on-demand items.
        /// </summary>
        /// <param name="obj">TreeViewNode is passed as default parameter </param>
        private void ExecuteOnDemandLoading(object obj)
        {
            var node = obj as TreeViewNode;

            // Skip the repeated population of child items when every time the node expands.
            if (node.ChildNodes.Count > 0)
            {
                node.IsExpanded = true;
                return;
            }
            //Animation starts for expander to show progressing of load on demand
            node.ShowExpanderAnimation = true;            
            Directory Directory = node.Content as Directory;
            Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background,
               new Action(async () =>
               {
                   await Task.Delay(1000);                

                   //Fetching child items to add
                   var items = GetDirectories(Directory);

                   // Populating child items for the node in on-demand
                   node.PopulateChildNodes(items);
                   if (items.Count() > 0)
                       //Expand the node after child items are added.
                       node.IsExpanded = true;

                   //Stop the animation after load on demand is executed, if animation not stopped, it remains still after execution of load on demand.
                   node.ShowExpanderAnimation = false;                   

               }));
        }

        private ObservableCollection<Directory> GetDrives()
        {
            ObservableCollection<Directory> directories = new ObservableCollection<Directory>();
            DriveInfo[] driveInfos = DriveInfo.GetDrives();
            foreach (DriveInfo driveInfo in driveInfos)
            {
                directories.Add(new Directory() { Name = driveInfo.Name, FullName = driveInfo.Name, HasChildNodes = true });
            }
            return directories;
        }

        public IEnumerable<Directory> GetDirectories(Directory directory)
        {
            ObservableCollection<Directory> directories = new ObservableCollection<Directory>();
            DirectoryInfo dirInfo = new DirectoryInfo(directory.FullName);
           
            foreach (DirectoryInfo directoryInfo in dirInfo.GetDirectories())
            {
                try
                {
                    directories.Add(new Directory()
                    {   
                        Name = directoryInfo.Name,
                        HasChildNodes = directoryInfo.GetDirectories().Length > 0 || directoryInfo.GetFiles().Length > 0,
                        FullName = directoryInfo.FullName
                    });
                }
                catch { }
            }
            foreach (FileInfo fileInfo in dirInfo.GetFiles())
            {
                directories.Add(new Directory()
                {
                    Name = fileInfo.Name,
                    HasChildNodes = false,
                    FullName = fileInfo.FullName
                });
            }
            return directories;
        }
    }

    public class LoadOnDemandCommand : ICommand
    {
        public LoadOnDemandCommand(Action<object> executeAction,
                               Predicate<object> canExecute)
        {
            this.executeAction = executeAction;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        Action<object> executeAction;
        Predicate<object> canExecute;

        private bool canExecuteCache = true;
        public bool CanExecute(object parameter)
        {
            if (canExecute != null)
            {
                bool tempCanExecute = canExecute(parameter);

                if (canExecuteCache != tempCanExecute)
                {
                    canExecuteCache = tempCanExecute;
                    RaiseCanExecuteChanged();
                }
            }
            return canExecuteCache;

        }

        public void Execute(object parameter)
        {
            executeAction(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, new EventArgs());
            }
        }

    }
}