In this sample, we are going to see about how to perform the lazy loading aka load on demand with the use case of Windows folder browser. 
In just three following steps, you can simply perform the lazy loading in the WPF TreeView, 

1.	Creating a TreeView with Data Binding
2.	Creating Load on-demand Command in ViewModel
3.	Binding LoadOnDemandCommand of TreeView with ViewModel command

## Creating a TreeView with Data Binding

Create the Model and ViewModel which has the data of drives and directories for the initial set of nodes to be displayed when TreeView is loading. 

**Name** property holds the drive or directory name **FullName** property holds the full path of the directory.

### Model
```c#
public class Directory : INotifyPropertyChanged
    {
        #region Fields

        string name;
        bool hasChildNodes;
        string fullName;


        #endregion

        #region Properties

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged("Name");
            }
        }

        public string FullName
        {
            get { return fullName; }
            set
            {
                fullName = value;
                RaisePropertyChanged("FullName");
            }
        }

        public bool HasChildNodes
        {
            get { return hasChildNodes; }
            set
            {
                hasChildNodes = value;
                RaisePropertyChanged("HasChildNodes");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

```

### ViewModel

Initially nodes are loaded with the drives available in Windows machine by getting the drive collection using DriveInfo.GetDrives(). The drive collection is set to the Directories property of ViewModel.

```c#
    public class ViewModel
    {
        public ObservableCollection<Directory> Directories { get; set; }
        
        public ViewModel()
        {
            this.Directories = this.GetDrives();
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
}
```

### XAML

* Add the ViewModel class as DataContext for Window
* Bind Directories property from ViewModel to ItemsSource property of TreeView.
* Set the ItemTemplate property of TreeView to display the content. Here, Label is added and Content of Label is bound to Name property.

```xml
        <Syncfusion:SfTreeView ItemsSource="{Binding Directories}"
                               ItemHeight="30"
                               HorizontalAlignment="Left" 
                               IsAnimationEnabled="True"
                               Margin="25,0,0,0" 
                               VerticalAlignment="Top" 
                               Width="250">
            <Syncfusion:SfTreeView.ItemTemplate>
                <DataTemplate>
                    <Label
                        VerticalContentAlignment="Center"
                        Content="{Binding Name}"
                        FocusVisualStyle="{x:Null}"
                        />
                    </DataTemplate>
            </Syncfusion:SfTreeView.ItemTemplate>
        </Syncfusion:SfTreeView>

```
## Creating Load on-demand Command in ViewModel

When you tap the expander of node, LoadOnDemandCommand will be executed. Create the command from ICommand interface and create the property in ViewModel in type of created command from ICommand interface.

### LoadOnDemandCommand

```c#
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
```
### Create property in ViewModel in type of LoadOnDemandCommand

```c#
    public class ViewModel
    { 
………………
public ICommand TreeViewLoadOnDemandCommand { get; set; }
………………

    }
```

### Implement Execute method of command

* Load the child nodes from the Execute method. Execute method will get called when user expands the tree node. 
* Show or hide busy indicator in the place of expander by setting TreeViewNode.ShowExpanderAnimation until the data fetched.
* The inner directories and files are retrieved from GetDirectories method. Populate the child nodes by calling TreeViewNode.PopulateChildNodes method by passing the child items collection which is retrieved from GetDirectories.
* When command executes, expanding operation will not be handled by TreeView. So, you have to set TreeViewNode.IsExpanded property to true to expand the tree node after populating child nodes.
* You can skip population of child items again and again when every time the node expands, based on TreeViewNode.ChildNodes count.

```c#
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
```

### Implement CanExecute method of command

Return true from CanExecute method to show the expander for node if the node has child nodes. Otherwise, return false if that node does not have child nodes.

```c#
private bool CanExecuteOnDemandLoading(object sender)
        {
            var hasChildNodes = ((sender as TreeViewNode).Content as Directory).HasChildNodes;
            if (hasChildNodes)
                return true;
            else
                return false;
        }
```

Initialize the TreeViewLoadOnDemandCommand,

```c#
public ViewModel()
        {             
……………
            TreeViewLoadOnDemandCommand = new LoadOnDemandCommand(ExecuteOnDemandLoading, CanExecuteOnDemandLoading);
        }
```

## Binding LoadOnDemandCommand of TreeView with ViewModel command

Finally, bind the TreeView.LoadOnDemandCommand property to ViewModel’s TreeViewLoadOnDemandCommand property.
That’s all. Lazy loading aka load on demand implementation has been done for WPF TreeView. 

```xml
<Syncfusion:SfTreeView ItemsSource="{Binding Directories}"
                               LoadOnDemandCommand="{Binding TreeViewLoadOnDemandCommand}"
                               ItemHeight="30"
                               HorizontalAlignment="Left" 
                               IsAnimationEnabled="True"
                               Margin="25,0,0,0" 
                               VerticalAlignment="Top" 
                               Width="250">
            <Syncfusion:SfTreeView.ItemTemplate>
                <DataTemplate>
                    <Label
                        VerticalContentAlignment="Center"
                        Content="{Binding Name}"
                        FocusVisualStyle="{x:Null}"
                        />
                    </DataTemplate>
            </Syncfusion:SfTreeView.ItemTemplate>
        </Syncfusion:SfTreeView>

```



