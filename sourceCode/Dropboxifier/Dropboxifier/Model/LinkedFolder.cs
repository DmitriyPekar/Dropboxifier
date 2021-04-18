using System.Collections.Generic;
using System.Xml.Serialization;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace Dropboxifier.Model
{   
    [Serializable]
    public class PerPCData : NotifyPropertyChangedBase
    {
        private string m_pcName;
        public string PCName
        {
            get { return m_pcName; }
            set { m_pcName = value; OnPropertyChanged("PCName"); }
        }

        private string m_source;
        public string Source
        {
            get { return m_source; }
            set { m_source = value; OnPropertyChanged("SourceDest"); }
        }

        private string m_dest;
        public string Destination
        {
            get { return m_dest; }
            set { m_dest = value; OnPropertyChanged("Destination"); }
        }
    }

    [Serializable]
    public class LinkedFolder : NotifyPropertyChangedBase
    {
        /// <summary>
        /// The link name
        /// </summary>
        private string m_linkName;
        public string LinkName 
        {
            get { return m_linkName; }
            set { m_linkName = value; OnPropertyChanged("LinkName"); }
        }

        /// <summary>
        /// Collection of PCs the link is synced on
        /// </summary>
        [XmlIgnore]
        public string SyncedPCsString
        {
            get
            {
                string result = _InternalSourceForSerializationDoNotTouch.Count > 0 ? _InternalSourceForSerializationDoNotTouch[0].PCName : "";
                for (int i = 1; i < _InternalSourceForSerializationDoNotTouch.Count; i++)
                {
                    result += "\n" + _InternalSourceForSerializationDoNotTouch[i].PCName;
                }
                return result;
            }
        }

        [XmlIgnore]
        public string SourceFoldersString
        {
            get
            {
                string result = _InternalSourceForSerializationDoNotTouch.Count > 0 ? _InternalSourceForSerializationDoNotTouch[0].Source : "";
                for (int i = 1; i < _InternalSourceForSerializationDoNotTouch.Count; i++)
                {
                    result += "\n" + _InternalSourceForSerializationDoNotTouch[i].Source;
                }
                return result;
            }
        }

        [XmlIgnore]
        public string DestFoldersString
        {
            get
            {
                string result = _InternalSourceForSerializationDoNotTouch.Count > 0 ? _InternalSourceForSerializationDoNotTouch[0].Destination : "";
                for (int i = 1; i < _InternalSourceForSerializationDoNotTouch.Count; i++)
                {
                    result += "\n" + _InternalSourceForSerializationDoNotTouch[i].Destination;
                }
                return result;
            }
        }

        /// <summary>
        /// Collection of sources
        /// </summary>
// ReSharper disable InconsistentNaming
        public List<PerPCData> _InternalSourceForSerializationDoNotTouch = new List<PerPCData>();
// ReSharper restore InconsistentNaming

        /// <summary>
        /// Get the collection of sources.
        /// </summary>
        [XmlIgnore]
        public ReadOnlyCollection<PerPCData> Sources
        {
            get { return new ReadOnlyCollection<PerPCData>(_InternalSourceForSerializationDoNotTouch); }
        }

        /// <summary>
        /// Sets the source directory for the current PC. Will replacing any existing source.
        /// </summary>
        public void SetDataForCurrentPC(string source, string dest)
        {
            RemoveDataForCurrentPC();

            _InternalSourceForSerializationDoNotTouch.Add(new PerPCData { PCName = Environment.MachineName, Source = source, Destination = dest });
            OnPropertyChanged("Sources");
        }

        /// <summary>
        /// Removes the source directory for the current PC.
        /// </summary>
        public void RemoveDataForCurrentPC()
        {
            foreach (PerPCData perSource in _InternalSourceForSerializationDoNotTouch)
            {
                if (perSource.PCName == Environment.MachineName)
                {
                    _InternalSourceForSerializationDoNotTouch.Remove(perSource);
                    return;
                }
            }
        }

        /// <summary>
        /// Gets the source directory for the current PC
        /// </summary>
        [XmlIgnore]
        public string SourceForCurrentPC
        {
            get 
            {
                foreach (PerPCData source in Sources)
                {
                   if (source.PCName == Environment.MachineName)
                   {
                       return source.Source;
                   }
                }
                return null;
            }
        }

        [XmlIgnore]
        public string DestForCurrentPC
        {
            get
            {
                foreach (PerPCData source in Sources)
                {
                    if (source.PCName == Environment.MachineName)
                    {
                        return source.Destination;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the resolved status for the current PC
        /// </summary>
        [XmlIgnore]
        public bool ResolvedForCurrentPC
        {
            get
            {
                string pcSource = SourceForCurrentPC;
                return (pcSource != null && Directory.Exists(pcSource));
            }
        }
    }
}
