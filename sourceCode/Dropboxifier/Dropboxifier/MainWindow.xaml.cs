using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Dropboxifier.ViewModel;
using Ookii.Dialogs.Wpf;
using System.Collections;
using Dropboxifier.Model;
using System.IO;

namespace Dropboxifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public DropboxifierViewModel DropboxVM { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            DropboxVM = new DropboxifierViewModel();
            DropboxVM.NoLinkFolderFound += DropboxVM_NoLinkFolderFound;
            DropboxVM.LinkedFolderErrors += DropboxVM_LinkedFolderErrors;
            DropboxVM.DropboxifyReadOnlyFiles += DropboxVM_DropboxifyReadOnlyFiles;

            DropboxVM.LoadDropboxifiedFolders();
        }

        void DropboxVM_DropboxifyReadOnlyFiles(object sender, EventArgs<bool> e)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.Content = String.Format("Read-only files were found in {0}. To move them into Dropbox they must be writeable. Make files writeable?", DropboxVM.NextLinkSource);
            dialog.Buttons.Clear();
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.No));

            dialog.MainIcon = TaskDialogIcon.Warning;
            dialog.WindowTitle = "Unauthorized Access";
            dialog.MainInstruction = "Make files writeable?";

            TaskDialogButton clickedButton = dialog.ShowDialog(this);
            if (clickedButton.ButtonType != ButtonType.Yes)
            {
                return;
            }
            e.Data = true;
        }

        void DropboxVM_LinkedFolderErrors(object sender, EventArgs<List<LinkedFolder>> e)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.Content = "The following links had errors and were marked as unresolved:\n";
            foreach (LinkedFolder link in e.Data)
            {
                dialog.Content += "\n" + link.LinkName + " - " + link.DestForCurrentPC;
            }
            dialog.Footer = "If the destination folder was deleted external from Dropboxifier the link must be undropboxified.";

            dialog.Buttons.Clear();
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Ok));

            dialog.MainIcon = TaskDialogIcon.Warning;
            dialog.WindowTitle = "Errors Found";

            dialog.ShowDialog(this);
        }

        void DropboxVM_NoLinkFolderFound(object sender, EventArgs<bool> e)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.Content = "No Dropboxifier links file found in " + DropboxVM.DropboxFolder + ". Create new link file?";
            dialog.Buttons.Clear();
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.No));

            dialog.MainIcon = TaskDialogIcon.Warning;
            dialog.WindowTitle = "No Dropboxifier Links";
            dialog.MainInstruction = "Create Dropboxifier Links File?";
            dialog.Footer = String.Format("There should be a Dropboxifier links file ({0}) in the selected Dropbox folder. " +
                "If this your first time running Dropbox select YES, otherwise you should retarget Dropboxifier to the correct Dropbox directory containing the links file.", DropboxifierViewModel.LINKED_FOLDERS_FILENAME);

            TaskDialogButton clickedButton = dialog.ShowDialog(this);
            if (clickedButton.ButtonType != ButtonType.Yes)
            {
                return;
            }
            e.Data = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = DropboxVM;
        }

        private void dropboxFolderSelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select Dropbox Folder";
            dialog.UseDescriptionForTitle = true;
            
            bool? ok = dialog.ShowDialog(this);
            if (ok != null && ok == true)
            {
                DropboxVM.DropboxFolder = dialog.SelectedPath;
            }
        }

        private void sourceDirFolderSelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select Link Source Folder";
            dialog.UseDescriptionForTitle = true;

            bool? ok = dialog.ShowDialog(this);
            if (ok != null && ok == true)
            {
                DropboxVM.NextLinkSource = dialog.SelectedPath;
            }
        }

        private void undropboxifyButton_OnClick(object sender, RoutedEventArgs e)
        {
            IList items = linkedFoldersDataGrid.SelectedItems;
            List<LinkedFolder> copiedList = items.OfType<LinkedFolder>().ToList();

            foreach (LinkedFolder linkedFolder in copiedList)
            {
                try
                {
                    DropboxVM.UnDropboxify(linkedFolder, ChkDeleteDropboxedFolder.IsChecked);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Exception Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void dropboxifyButton_OnClick(object sender, RoutedEventArgs e)
        {
            bool deleteDest = false;

            string dest = DropboxVM.GetNextDestinationDirectory();
            if (Directory.Exists(dest))
            {
                TaskDialog dialog = new TaskDialog();
                dialog.EnableHyperlinks = true;
                dialog.Content = String.Format("<A HREF=\"file:///{0}\">{1}</A> already exists. Continuing will delete the contents of this folder. \n\nContinue?", dest, dest);
                dialog.Buttons.Clear();
                dialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
                dialog.Buttons.Add(new TaskDialogButton(ButtonType.No));

                dialog.MainIcon = TaskDialogIcon.Warning;
                dialog.WindowTitle = "Folder Deletion Warning";
                dialog.MainInstruction = "Confirm Continuation";
                dialog.HyperlinkClicked += (o, args) => System.Diagnostics.Process.Start(args.Href);
                
                TaskDialogButton pressedButton = dialog.ShowDialog(this);
                if (pressedButton.ButtonType != ButtonType.Yes)
                {
                    return;
                }
                deleteDest = true;
            }
            try
            {
                DropboxVM.DropboxifyNext(deleteDest);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void hyperlink_Clicked(object sender, RoutedEventArgs e)
        {
            Hyperlink link = e.Source as Hyperlink;
            if (link != null)
            {
                System.Diagnostics.Process.Start(link.NavigateUri.ToString());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DropboxVM.SaveDropboxifiedFolders(true);
            Properties.Settings.Default.Save();
        }

        private void linkedFoldersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid grid = (DataGrid) sender;

            btnResolve.IsEnabled = false;

            IList selectedItems = grid.SelectedItems;
            foreach (object item in selectedItems)
            {
                LinkedFolder link = item as LinkedFolder;
                if (link != null && !link.ResolvedForCurrentPC)
                {
                    btnResolve.IsEnabled = true;
                    return;
                }
            }
        }

        private void btnResolve_Click(object sender, RoutedEventArgs e)
        {
            TaskDialog dialog = new TaskDialog();
            dialog.Content = "Select how you would like to resolve the selected unresolved links (this will apply to all unresolved selected links):" + 
                "\n\nMerge - Attemps to merge source directory with Dropbox. Files will be added or replaced if newer (recommended)" +
                "\n\nOverwrite - Overwrites the files in Dropbox with files from the selected directory" +
                "\n\nDelete and Link - Deletes the selected folder for linking and creates a link directly to Dropbox." +
                "\n                               THIS DATA CANNOT BE RECOVERED. USE WITH CAUTION.";
            dialog.Buttons.Clear();
            dialog.Buttons.Add(new TaskDialogButton("Merge"));
            dialog.Buttons.Add(new TaskDialogButton("Overwrite"));
            dialog.Buttons.Add(new TaskDialogButton("Delete and Link"));
            dialog.Buttons.Add(new TaskDialogButton(ButtonType.Cancel));

            dialog.Width = 300;

            dialog.MainIcon = TaskDialogIcon.Warning;
            dialog.WindowTitle = "Resolve Mode Selection";
            dialog.MainInstruction = "Select Resolve Mode";

            TaskDialogButton clickedButton = dialog.ShowDialog();
            if (clickedButton.ButtonType == ButtonType.Cancel || 
                clickedButton.ButtonType == ButtonType.Close)
            {
                return;
            }

            DropboxifierViewModel.ResolveLinkMode mode = DropboxifierViewModel.ResolveLinkMode.MergeData;
            if (clickedButton.Text == "Overwrite")
            {
                mode = DropboxifierViewModel.ResolveLinkMode.ReplaceDestData;
            }
            else if (clickedButton.Text == "Delete and Link")
            {

                mode = DropboxifierViewModel.ResolveLinkMode.DeleteSourceAndLink;
            }

            IList selectedRows = linkedFoldersDataGrid.SelectedItems;
            List<LinkedFolder> linkedFoldersCopy = new List<LinkedFolder>();
            foreach (object row in selectedRows)
            {
                LinkedFolder link = row as LinkedFolder;
                if (link != null && !link.ResolvedForCurrentPC)
                {
                    linkedFoldersCopy.Add(link);
                }
            }

            foreach (LinkedFolder folder in linkedFoldersCopy)
            {
                VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
                folderBrowser.Description = "Select folder for link " + folder.LinkName;

                bool? selected = folderBrowser.ShowDialog(this);
                if (selected != null && selected == true)
                {
                    // Give the user one last chance to understand their potential mistake...
                    if (mode == DropboxifierViewModel.ResolveLinkMode.DeleteSourceAndLink)
                    {
                        dialog.Buttons.Clear();
                        dialog.Buttons.Add(new TaskDialogButton(ButtonType.Yes));
                        dialog.Buttons.Add(new TaskDialogButton(ButtonType.No));

                        dialog.WindowTitle = "Confirm Dangerous Selection";
                        dialog.MainInstruction = "Confirm Deletion";
                        dialog.Content = "You have selected DELETE AND LINK. All data under\"" + folderBrowser.SelectedPath + "\" will be deleted." +
                                          "\nThis is IRREVERSIBLE. Are you sure you wish to do this?";

                        clickedButton = dialog.ShowDialog(this);
                        if (clickedButton.ButtonType != ButtonType.Yes)
                        {
                            return;
                        }
                    }

                    try
                    {
                        DropboxVM.ResolveLinkedFolder(folder, folderBrowser.SelectedPath, mode);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Exception occured when resolving: " + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // abort entire thing if user cancels
                    return;
                }
            }
        }
    }
}
