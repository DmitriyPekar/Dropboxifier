using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dropboxifier.Model;
using System.Xml.Serialization;
using System.Windows;
using System.IO;

namespace Dropboxifier.ViewModel
{
    public class DropboxifierViewModel : ViewModelBase
    {
        public const string LINKED_FOLDERS_FILENAME = "DropboxifierLinks.xml";
        public string DropboxifiedLinksFilePath
        {
            get { return DropboxFolder + "\\" + LINKED_FOLDERS_FILENAME; }
        }

        /// <summary>
        /// Current program version
        /// </summary>
        private readonly Version m_currentVersion = new Version(0, 1, 8);
        public Version CurrentVersion
        {
            get { return m_currentVersion; }
        }

        /// <summary>
        /// The window's title, holy cow!
        /// </summary>
        public string WindowTitle
        {
            get { return "Dropboxifier by DG - v" + CurrentVersion; }
        }

        public enum ResolveLinkMode
        {
            /// <summary>
            /// Merges the source data with the destination data. Moves non-existing files. Replaces newer files.
            /// </summary>
            MergeData,

            /// <summary>
            /// Copies the source data and replaces the contents of the Dropbox folder
            /// </summary>
            ReplaceDestData, 

            /// <summary>
            /// Deletes the source data and creates a symbolic link to the Dropbox folder
            /// </summary>
            DeleteSourceAndLink
        };

        /// <summary>
        /// Collection of known linked folders. Gets serialized and saved.
        /// </summary>
        public ObservableCollection<LinkedFolder> LinkedFolders { get; private set; }

        /// <summary>
        /// Event called when no dropbox marker was found in the Dropbox folder.
        /// The "Data" of the event argument should be set to true if you want to create the link folder.
        /// </summary>
        public event EventHandler<EventArgs<bool>> NoLinkFolderFound;

        /// <summary>
        /// Event called if linked folder errors are found - these links will be marked unresolved on this PC
        /// </summary>
        public event EventHandler<EventArgs<List<LinkedFolder>>> LinkedFolderErrors;

        /// <summary>
        /// Event fired when trying to dropboxify and readonly files are found.
        /// The "Data" of the argument should be set to true if you want to make files writeable and continue
        /// </summary>
        public event EventHandler<EventArgs<bool>> DropboxifyReadOnlyFiles;

        public string DropboxFolder
        {
            get { return Properties.Settings.Default.DropboxFolder; }
            set 
            { 
                Properties.Settings.Default.DropboxFolder = value.Trim();
                LoadDropboxifiedFolders();
                OnPropertyChanged("DropboxFolder"); 
            }
        }

        public string NextLinkName
        {
            get { return Properties.Settings.Default.LastLinkName; }
            set { Properties.Settings.Default.LastLinkName= value; OnPropertyChanged("NextLinkName"); }
        }

        public string NextLinkSource
        {
            get { return Properties.Settings.Default.LastLinkSource; }
            set { Properties.Settings.Default.LastLinkSource = value; OnPropertyChanged("NextLinkSource"); }
        }

        public DropboxifierViewModel()
        {
            LinkedFolders = new ObservableCollection<LinkedFolder>();
        }

        protected override void OnDispose()
        {
            Properties.Settings.Default.Save();
            base.OnDispose();
        }

        public void DropboxifyNext(bool deleteDest)
        {
            LinkedFolder next = new LinkedFolder
            { 
                LinkName = NextLinkName
            };
            next.SetDataForCurrentPC(NextLinkSource, GetNextDestinationDirectory());

            Dropboxify(next, deleteDest);

            LinkedFolders.Add(next);
            SaveDropboxifiedFolders(true);
        }

        private void Dropboxify(LinkedFolder folderInfo, bool deleteDest)
        {
            if (folderInfo.SourceForCurrentPC.ToUpper().Contains(DropboxFolder.ToUpper()))
            {
                throw new Exception(String.Format("Cannot Dropboxify a folder in your Dropbox folder ({0} -> {1}) - aborted", folderInfo.SourceForCurrentPC, folderInfo.DestForCurrentPC));
            }

            foreach (LinkedFolder folder in LinkedFolders)
            {
                if (folder.LinkName == folderInfo.LinkName)
                {
                    throw new Exception(String.Format("There is already a link named {0} - please use a unique name", folderInfo.LinkName));
                }

                if (folder.SourceForCurrentPC == folderInfo.SourceForCurrentPC)
                {
                    throw new Exception(String.Format("Already entry for source {0} - aborted", folderInfo.SourceForCurrentPC));
                }

                if (folder.DestForCurrentPC == folderInfo.DestForCurrentPC)
                {
                    throw new Exception(String.Format("Already entry for destination {0} - aborted", folderInfo.DestForCurrentPC));
                }
            }

            if (!Directory.Exists(folderInfo.SourceForCurrentPC))
            {
                throw new DirectoryNotFoundException(folderInfo.SourceForCurrentPC);
            }


            FileAttributes attribs = File.GetAttributes(folderInfo.SourceForCurrentPC);
            if ((attribs & FileAttributes.ReparsePoint) != 0)
            {
                // source is not a symbolic link - something is wrong!
                throw new Exception("Source directory is a symbolic link. Cannot dropboxify.");
            }

            if (Directory.Exists(folderInfo.DestForCurrentPC))
            {
                if (deleteDest)
                {
                    FileUtils.DeleteFolder(folderInfo.DestForCurrentPC);
                }
                else
                {
                    throw new Exception("Cannot dropboxify - destination folder already exists");
                }
            }
            if (!Directory.Exists(Path.GetDirectoryName(folderInfo.DestForCurrentPC)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(folderInfo.DestForCurrentPC));
            }

            try
            {
                FileUtils.MoveFolder(folderInfo.SourceForCurrentPC, folderInfo.DestForCurrentPC);
            }
            catch (UnauthorizedAccessException)
            {
                if (DropboxifyReadOnlyFiles != null)
                {
                    EventArgs<bool> args = new EventArgs<bool>(false);
                    DropboxifyReadOnlyFiles(this, args);

                    if (args.Data)
                    {
                        FileUtils.MakeFolderContentsWritable(new DirectoryInfo(folderInfo.SourceForCurrentPC), FileUtils.FolderMode.Recursive);
                        FileUtils.MoveFolder(folderInfo.SourceForCurrentPC, folderInfo.DestForCurrentPC);
                    }
                }
            }
            catch (Exception)
            {
                // any other error - move files back
                FileUtils.CopyFolder(folderInfo.DestForCurrentPC, folderInfo.SourceForCurrentPC, FileUtils.FolderMode.Recursive, FileUtils.CopyFileMode.SkipIfExists);
                FileUtils.DeleteFolder(folderInfo.DestForCurrentPC);

                throw;
            }

            Utils.CreateSymbolicLink(folderInfo.SourceForCurrentPC, folderInfo.DestForCurrentPC, Utils.SYMBOLIC_LINK_FLAG_DIRECTORY);

            NextLinkName = "";
            NextLinkSource = "";
        }

        public string GetNextDestinationDirectory()
        {
            return DropboxFolder + "\\" + NextLinkName;
        }

        public void UnDropboxify(LinkedFolder folderInfo, bool? deleteDropboxSubfolder)
        {
            if (Directory.Exists(folderInfo.SourceForCurrentPC))
            {
                FileAttributes attribs = File.GetAttributes(folderInfo.SourceForCurrentPC);
                if ((attribs & FileAttributes.ReparsePoint) == 0)
                {
                    // source is not a symbolic link - something is wrong!
                    RemoveLinkedFolder(folderInfo, false);
                    throw new Exception("Source directory is not a symbolic link - entry removed. Files will remain in Dropbox in their current state and will need to be merged manually.");
                }

                Directory.Delete(folderInfo.SourceForCurrentPC);
            }

            if (Directory.Exists(folderInfo.DestForCurrentPC))
            {
                FileUtils.CopyFolder(folderInfo.DestForCurrentPC, folderInfo.SourceForCurrentPC, FileUtils.FolderMode.Recursive, FileUtils.CopyFileMode.Replace);
            }

            // Remove the link - if it's the last link, also delete the Dropbox subfolder if specified
            RemoveLinkedFolder(folderInfo, deleteDropboxSubfolder ?? false);
        }

        /// <returns>True if it was the final reference that was remove</returns>
        private void RemoveLinkedFolder(LinkedFolder folder, bool deleteIfFinalRef)
        {
            bool finalRef = false;

            string destForCurrentPC = folder.DestForCurrentPC;
            folder.RemoveDataForCurrentPC();
            if (folder.Sources.Count == 0)
            {
                LinkedFolders.Remove(folder);
                finalRef = true;
            }
            else
            {
                // Force binding update. This retarded >:(
                LinkedFolders.Remove(folder);
                LinkedFolders.Add(folder);
            }
            SaveDropboxifiedFolders(true);

            if (finalRef && deleteIfFinalRef &&
                Directory.Exists(destForCurrentPC))
            {
                FileUtils.DeleteFolder(destForCurrentPC);
            }
        }

        public void LoadDropboxifiedFolders()
        {
            LinkedFolders.Clear();

            if (String.IsNullOrEmpty(DropboxFolder) || !Directory.Exists(DropboxFolder))
            {
                return;
            }

            // If there's no links folder, send an event
            if (!File.Exists(DropboxifiedLinksFilePath))
            {
                if (NoLinkFolderFound != null)
                {
                    EventArgs<bool> args = new EventArgs<bool>(false);
                    NoLinkFolderFound(this, args);

                    // If the event's data comes back as true, save (create) a links file and return
                    if (args.Data)
                    {
                        SaveDropboxifiedFolders(false);
                        return;
                    }
                    DropboxFolder = "";
                    return;
                }
            }

            // Load the links file
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<LinkedFolder>));
                FileStream fs = File.OpenRead(DropboxifiedLinksFilePath);
                LinkedFolders = serializer.Deserialize(fs) as ObservableCollection<LinkedFolder>;
                fs.Close();
                OnPropertyChanged("LinkedFolders");
            }
            catch(Exception e)
            {
                // If there's an error, don't use the specified folder
                MessageBox.Show("Failed to load linked folders file: " + e.Message);
                DropboxFolder = "";
            }

            CheckForErrors();
        }

        public void SaveDropboxifiedFolders(bool checkForExistance)
        {
            if (String.IsNullOrEmpty(DropboxFolder) || !Directory.Exists(DropboxFolder))
            {
                return;
            }

            if (!File.Exists(DropboxifiedLinksFilePath) && checkForExistance)
            {
                if (NoLinkFolderFound != null)
                {
                    EventArgs<bool> eventArgs = new EventArgs<bool>(false);
                    NoLinkFolderFound(this, eventArgs);

                    if (!eventArgs.Data)
                    {
                        DropboxFolder = "";
                        return;
                    }
                }
            }

            try
            {
                if (File.Exists(DropboxifiedLinksFilePath))
                {
                    File.Delete(DropboxifiedLinksFilePath);
                }
                XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<LinkedFolder>));
                FileStream fs = File.Create(DropboxifiedLinksFilePath);
                serializer.Serialize(fs, LinkedFolders);
                fs.Close();
            }
            catch(Exception e)
            {
                MessageBox.Show("Failed to save linked folders file: " + e.Message);
            }
        }

        public void ResolveLinkedFolder(LinkedFolder link, string newPerPcSource, ResolveLinkMode mode)
        {
            if (!Directory.Exists(newPerPcSource))
            {
                throw new DirectoryNotFoundException(newPerPcSource);
            }

            string newPerPcDest = DropboxFolder + "\\" + link.LinkName;

            if (mode == ResolveLinkMode.MergeData)
            {
                FileUtils.CopyFolder(newPerPcSource, newPerPcDest, FileUtils.FolderMode.Recursive, FileUtils.CopyFileMode.ReplaceIfNewer);
            }
            else if (mode == ResolveLinkMode.ReplaceDestData)
            {
                FileUtils.CopyFolder(newPerPcSource, newPerPcDest, FileUtils.FolderMode.Recursive, FileUtils.CopyFileMode.Replace);
            }
            else if (mode == ResolveLinkMode.DeleteSourceAndLink)
            {
                // this space intentionally left blank
                // source is deleted below as a common operation
            }
            else
            {
                throw new Exception("Unhandled resolve mode: " + mode.ToString());
            }
            FileUtils.DeleteFolder(newPerPcSource);
            Utils.CreateSymbolicLink(newPerPcSource, newPerPcDest, Utils.SYMBOLIC_LINK_FLAG_DIRECTORY);

            link.SetDataForCurrentPC(newPerPcSource, newPerPcDest);

            // Re-add to force a refresh of any bindings
            LinkedFolders.Remove(link);
            LinkedFolders.Add(link);

            SaveDropboxifiedFolders(true);
        }

        public void CheckForErrors()
        {
            List<LinkedFolder> errorList = new List<LinkedFolder>();
            foreach (LinkedFolder link in LinkedFolders)
            {
                if (!link.ResolvedForCurrentPC)
                {
                    continue;
                }

                if (!Directory.Exists(link.DestForCurrentPC))
                {
                    errorList.Add(link);
                    continue;
                }

                FileAttributes attribs = File.GetAttributes(link.SourceForCurrentPC);
                if ((attribs & FileAttributes.ReparsePoint) == 0)
                {
                    // source is not a symbolic link - something is wrong!
                    errorList.Add(link);
                }    
            }

            foreach (LinkedFolder errorLink in errorList)
            {
                RemoveLinkedFolder(errorLink, false);
            }

            if (errorList.Count > 0 && LinkedFolderErrors != null)
            {
                LinkedFolderErrors(this, new EventArgs<List<LinkedFolder>>(errorList));
            }
        }
    }
}
