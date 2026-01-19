# How to Perform Lazy Loading in WPF TreeView?

In this sample, we are going to see about how to perform the lazy loading aka load on demand with the use case of Windows folder browser. 
In just three following steps, you can simply perform the lazy loading in the [WPF TreeView](https://www.syncfusion.com/wpf-controls/treeview) (SfTreeView), 

1.	Creating a TreeView with Data Binding
2.	Creating Load on-demand Command in ViewModel
3.	Binding LoadOnDemandCommand of TreeView with ViewModel command

## Creating a TreeView with Data Binding

Create a class named Directory with the property **Name**, which denotes the name of the drive or directory, and the property **FullName**, which denotes the full path of the directory. The Directory class should be derived from [NotificationObject](https://help.syncfusion.com/cr/wpf/Syncfusion.Windows.Shared.NotificationObject.html), which has the implementation for the INotifyPropertyChanged interface.

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

Next, create a **ViewModel** class with the **Directories** property. This property holds the driver details of the system to create a file-exploring TreeView. When a user expands the driver, the child nodes will be loaded on demand.

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

After creating the ViewModel class:

* Add the ViewModel class as DataContext for Window.
* Bind the **Directories** property from the ViewModel to the [ItemsSource](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.SfTreeView.html#Syncfusion_UI_Xaml_TreeView_SfTreeView_ItemsSource) property of TreeView.
* Set the [ItemTemplate](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.SfTreeView.html#Syncfusion_UI_Xaml_TreeView_SfTreeView_ItemTemplate) property of TreeView to display the content. Here, Label is added, and the Content of Label is bound to the **Name**.

### XAML
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

Now, `TreeView` will be displayed with two nodes. Let's enable lazy loading in `TreeView` by creating [LoadOnDemandCommand](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.SfTreeView.html#Syncfusion_UI_Xaml_TreeView_SfTreeView_LoadOnDemandCommand). This will control the expander visibility based on the return value of the CanExecute command. The loading of child nodes is done in the Execute command.

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
    public ICommand TreeViewLoadOnDemandCommand { get; set; }

    public ViewModel()
    {             
        TreeViewLoadOnDemandCommand = new DelegateCommand(ExecuteOnDemandLoading, CanExecuteOnDemandLoading);
    }         
}
```

### Handling TreeNode expander visibility in the CanExecute method of command

Return true from the CanExecute method to show the expander for the node if the node has child nodes, or else return false. Here, the visibility of the expander is returned based on the HasChildNodes property in the Directory. The HasChildNodes property is set based on whether the directory has subdirectories or files when the node is populated.

``` c#
private bool CanExecuteOnDemandLoading(object sender)
{
   var hasChildNodes = ((sender as TreeViewNode).Content as Directory).HasChildNodes;
   if (hasChildNodes)
     return true;
   else
     return false;
}
```

### Lazy loading of TreeView in Execute method of command

Load the child nodes in the Execute method. Call the Execute method when a user expands the tree node.

Show or hide the busy indicator in the place of the expander by setting [TreeViewNode.ShowExpanderAnimation](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.Engine.TreeViewNode.html#Syncfusion_UI_Xaml_TreeView_Engine_TreeViewNode_ShowExpanderAnimation) until the data is fetched.

Retrieve the inner directories and files from the GetDirectories method. Populate the child nodes by calling the [TreeViewNode.PopulateChildNodes](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.Engine.TreeViewNode.html#Syncfusion_UI_Xaml_TreeView_Engine_TreeViewNode_PopulateChildNodes_System_Collections_IEnumerable_) method by passing the child items collection that is obtained from GetDirectories.

During command execution, the expanding operation will not be handled by TreeView. So, you have to set the [TreeViewNode.IsExpanded](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.Engine.TreeViewNode.html#Syncfusion_UI_Xaml_TreeView_Engine_TreeViewNode_IsExpanded) property to true to expand the tree node after populating child nodes.

You should skip the population of child items every time the node expands, based on the [TreeViewNode.ChildNodes](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.Engine.TreeViewNode.html#Syncfusion_UI_Xaml_TreeView_Engine_TreeViewNode_ChildNodes) count.

``` c#
private void ExecuteOnDemandLoading(object obj)
{
   var node = obj as TreeViewNode;

   // Skip the repeated population of child items every time the node expands.
   if (node.ChildNodes.Count > 0)
   {
      node.IsExpanded = true;
      return;
   }
   //Animation starts for expander to show progress of load on demand.
   node.ShowExpanderAnimation = true;            
   Directory Directory = node.Content as Directory;
   Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background,
    new Action(async () =>
    {
      await Task.Delay(1000);                

      //Fetching child items to add.
      var items = GetDirectories(Directory);

      // Populating child items for the node in on demand.
      node.PopulateChildNodes(items);
      if (items.Count() > 0)
         //Expand the node after child items are added.
         node.IsExpanded = true;

      //Stop the animation after load on demand is executed. If animation is not stopped, it remains after execution of load on demand.
      node.ShowExpanderAnimation = false;                   

     }));
    }
    public IEnumerable<Directory> GetDirectories(Directory directory)
    {
       var directories = new ObservableCollection<Directory>();
       var  dirInfo = new DirectoryInfo(directory.FullName);
           
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

### Binding load on-demand command of TreeView with ViewModel command

Finally, bind the [TreeView.LoadOnDemandCommand](https://help.syncfusion.com/cr/wpf/Syncfusion.UI.Xaml.TreeView.SfTreeView.html#Syncfusion_UI_Xaml_TreeView_SfTreeView_LoadOnDemandCommand) property to the ViewModel's TreeViewLoadOnDemandCommand property.

That's all. Lazy loading implementation is completely done for WPF TreeView.

``` xml
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

![TreeView perform lazy loading](TreeViewLazyLoading.gif)
