using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dropboxifier
{
    static public class FileUtils
    {
        public enum FolderMode
        {
            /// <summary>
            /// Operate on sub-folders recursively
            /// </summary>
            Recursive,

            /// <summary>
            /// Operate on just the specified folder
            /// </summary>
            NonRecursive,
        }

        public enum CopyFileMode
        {
            /// <summary>
            /// Replace all existing files
            /// </summary>
            Replace,

            /// <summary>
            /// Replace existing files if the file write date is newer
            /// </summary>
            ReplaceIfNewer,

            /// <summary>
            /// Do not replace existing files
            /// </summary>
            SkipIfExists
        }

		/// <summary>
        /// Copy a folder to another folder with optional recursive behavior.
        /// </summary>
        static public void CopyFolder(string source, string dest, FolderMode mode, CopyFileMode fileMode)
        {
            DirectoryInfo sourceDI = new DirectoryInfo(source);
            DirectoryInfo destDI = new DirectoryInfo(dest);

            CopyFolder(sourceDI, destDI, mode, fileMode);
        }

        /// <summary>
        /// Create a folder if it does not exist
        /// </summary>
        static public void CreateFolderIfDoesNotExist(string path)
        {
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(path));
            if (!di.Exists)
            {
                di.Create();
            }
        }

        /// <summary>
        /// Moves a folder to another location. If the folders are on different volumes,
        /// the source folder will be copied to the destination folder then deleted.
        /// </summary>
        static public void MoveFolder(string source, string dest)
        {
            MoveFolder(new DirectoryInfo(source), new DirectoryInfo(dest));
        }

        /// <summary>
        /// Moves a folder to another location. If the folders are on different volumes,
        /// the source folder will be copied to the destination folder then deleted.
        /// </summary>
        static public void MoveFolder(DirectoryInfo source, DirectoryInfo dest)
        {
            if (source.Root == dest.Root)
            {
                source.MoveTo(dest.FullName);
            }
            else
            {
                dest.Create();
                CopyFolder(source, dest, FolderMode.Recursive, CopyFileMode.Replace);
                DeleteFolder(source.FullName);
            }
        }

        /// <summary>
        /// Copy a folder to another folder with optional recursive behavior.
        /// </summary>
		static public void CopyFolder(DirectoryInfo sourceInfo, DirectoryInfo destInfo, FolderMode mode, CopyFileMode fileMode)
        {
            if (!destInfo.Exists)
            {
                destInfo.Create();
            }

            foreach (FileInfo fi in sourceInfo.GetFiles())
            {
                CopyFile(fi, destInfo, fileMode);
            }

			if (mode == FolderMode.Recursive)
            {
                foreach (DirectoryInfo di in sourceInfo.GetDirectories())
                {
                    DirectoryInfo childDestDir = new DirectoryInfo(destInfo.FullName + "\\" + di.Name);
                    CopyFolder(di, childDestDir, mode, fileMode);
                }
            }
        }

        /// <summary>
        /// Copy a list of specified files.
        /// </summary>
        static public void CopyFiles(string sourceDir, string destDir, List<string> relativeFiles, CopyFileMode fileMode)
        {
            foreach (string file in relativeFiles)
            {
                FileInfo sourceInfo = new FileInfo(sourceDir + "\\" + file);
                DirectoryInfo destInfo = new DirectoryInfo(Path.GetDirectoryName(destDir + "\\" + file));

                CopyFile(sourceInfo, destInfo, fileMode);
            }
        }

        /// <summary>
        /// Copy a specified file.
        /// </summary>
        /// <param name="source">Source file or directory</param>
        /// <param name="dest">Destination file or directory</param>
        /// <param name="mode">The copy mode to use</param>
        static public void CopyFile(string source, string dest, CopyFileMode mode)
        {
            FileInfo sourceInfo = new FileInfo(source);
            if (!sourceInfo.Exists)
            {
                return;
            }

            DirectoryInfo destInfo = new DirectoryInfo(dest);

            CopyFile(sourceInfo, destInfo, mode);
        }

        /// <summary>
        /// Copy a specified file.
        /// </summary>
        /// <param name="sourceInfo">Source file to copy.</param>
        /// <param name="destInfo">Destination directory.</param>
        /// <param name="mode">The copy mode to use.</param>
        static public void CopyFile(FileInfo sourceInfo, DirectoryInfo destInfo, CopyFileMode mode)
        {
            System.Diagnostics.Debug.Assert(sourceInfo.Exists, "Source file does not exist!");

            // Avoid a missing folder exception
            if (!destInfo.Exists)
            {
                destInfo.Create();
            }

            // Avoid copying over a read only file exception
            FileInfo destPathInfo = new FileInfo(destInfo.FullName + "\\" + sourceInfo.Name);

            if (destPathInfo.Exists)
            {
                if (mode == CopyFileMode.Replace ||
                    (mode == CopyFileMode.ReplaceIfNewer && destPathInfo.LastWriteTime < sourceInfo.LastWriteTime))
                {
                    destPathInfo.IsReadOnly = false;
                    destPathInfo.Delete();
                }
                else // SkipIfExists or ReplaceIfNewer fails
                {
                    return;
                }
            }

            // Perform the actual copy
            sourceInfo.CopyTo(destPathInfo.FullName, true);
        }

		/// <summary>
		/// Make the specified file read/writable
		/// </summary>
		static public void MakeFileWritable(FileInfo fileInfo)
		{
			if (fileInfo.Exists)
			{
				fileInfo.IsReadOnly = false;
			}
		}

		/// <summary>
		/// Make the specified file read/writable
		/// </summary>
		static public void MakeFolderContentsWritable(DirectoryInfo dirInfo, FolderMode mode)
		{
			foreach (FileInfo fi in dirInfo.GetFiles())
			{
				MakeFileWritable(fi);
			}

			if (mode == FolderMode.Recursive)
			{
				foreach (DirectoryInfo di in dirInfo.GetDirectories())
				{
					DirectoryInfo childDestDir = new DirectoryInfo(dirInfo.FullName + "\\" + di.Name);
					MakeFolderContentsWritable(childDestDir, mode);
				}
			}
		}

		/// <summary>
		/// Delete the specified folder (and any sub-folders/files)
		/// </summary>
		static public void DeleteFolder(string dir)
		{
            DeleteFolder(new DirectoryInfo(dir));
		}

        /// <summary>
        /// Delete the specified folder (and any sub-folders/files)
        /// </summary>
        static public void DeleteFolder(DirectoryInfo dirInfo)
        {
            if (dirInfo.Exists)
            {
                MakeFolderContentsWritable(dirInfo, FolderMode.Recursive);
                dirInfo.Delete(true);
            }
        }

        /// <summary>
        /// Given a directory select a random file from it.
        /// </summary>
        static public string SelectRandomFileFromFolder(string folderName)
        {
            DirectoryInfo di = new DirectoryInfo(folderName);

            if (di.Exists)
            {
                FileInfo[] files = di.GetFiles();
                if (files.Length > 0)
                {
                    Random randGenerator = new Random();
                    int index = randGenerator.Next(0, files.Length - 1);
                    return files[index].FullName;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Computes the MD5 checksum for a single file.
        /// </summary>
        public static string ComputeMD5ForFile(string file)
        {
            FileInfo finfo = new FileInfo(file);
            BinaryReader br = new BinaryReader(finfo.OpenRead());
            byte[] fileBytes = br.ReadBytes((int)finfo.Length);
            br.Close();

            MD5 hasher = MD5.Create();
            byte[] hash = hasher.ComputeHash(fileBytes);

            StringBuilder hashString = new StringBuilder();
            foreach (byte b in hash)
            {
                hashString.Append(b.ToString("x2"));
            }
            return hashString.ToString();
        }
    }
}
