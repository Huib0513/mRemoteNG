using System;
using System.Collections.Generic;
using System.Linq;
using mRemoteNG.Connection;
/*using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.Http;
using mRemoteNG.Connection.Protocol.ICA;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.VNC;*/
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using IniParser.Parser;
using IniParser.Model;

namespace mRemoteNG.Config.Serializers.Csv
{
    public class MobaXTermConnectionsDeserializer : IDeserializer<string, ConnectionTreeModel>
    {
        // TODO FIRST! Refactor completely, building up tree as we go


        public ConnectionTreeModel Deserialize(string serializedData)
        {
            var iniparser = new IniDataParser();
            var inidata = iniparser.Parse(serializedData);

            // used to map a connectioninfo to its parent
            var parentMapping = new Dictionary<ConnectionInfo, string>();

            //Iterate through all the sections in the ini file
            foreach (SectionData section in inidata.Sections)
            {
                // Multiple folders may exist starting with "Bookmarks_", these are folders containing connections in a tree structure
                if ("Bookmarks" == section.SectionName.Substring(0, Math.Min(9, section.SectionName.Length)))
                {
                    var folderMapping = ParseFolder(section);

                    // TODO hierarchy
                    /*
                        [Bookmarks_2]
                        SubRep=Test
                        ImgNum=41
                        Pi Zero Bredeweg intern=#129#0%192.168.8.99%22%pi%%-1%-1%%%22%%0%0%0%_ProfileDir_\Portables\MobaXterm\slash\ed25519-DellXPS.ppk%%-1%0%0%0%%1080%%0%0%1#MobaFont%10%0%0%0%15%236,236,236%0,0,0%180,180,192%0%-1%0%%xterm%-1%0%0,0,0%54,54,54%255,96,96%255,128,128%96,255,96%128,255,128%255,255,54%255,255,128%96,96,255%128,128,255%255,54,255%255,128,255%54,255,255%128,255,255%236,236,236%255,255,255%80%24%0%1%-1%<none>%%0#0#

                        [Bookmarks_3]
                        SubRep=Test\Test level 2
                    */
                    foreach (var connection in folderMapping)
                    {
                        parentMapping.Add(connection.Key, connection.Value);
                    }
                }
            }

            var root = CreateTreeStructure(parentMapping);
            var connectionTreeModel = new ConnectionTreeModel();
            connectionTreeModel.AddRootNode(root);
            return connectionTreeModel;
        }

        private RootNodeInfo CreateTreeStructure(Dictionary<ConnectionInfo, string> parentMapping)
        {
            var root = new RootNodeInfo(RootNodeType.Connection);

            foreach (var node in parentMapping)
            {
                // no parent mapped, add to root
                if (string.IsNullOrEmpty(node.Value))
                {
                    root.AddChild(node.Key);
                    continue;
                }

                // search for parent in the list by GUID
                var parent = parentMapping
                            .Keys
                            .OfType<ContainerInfo>()
                            .FirstOrDefault(info => info.ConstantID == node.Value);

                if (parent != null)
                {
                    parent.AddChild(node.Key);
                }
                else
                {
                    root.AddChild(node.Key);
                }
            }

            return root;
        }

        private Dictionary<ConnectionInfo, string> ParseFolder(SectionData folder)
        {
            var folderMapping = new Dictionary<ConnectionInfo, string>();

            var folderName = folder.Keys["SubRep"];
            var folderId = "";

            if (!string.IsNullOrEmpty(folderName))
            {
                // Create container for folder if it has a name, otherwise it is the root folder
                // TODO Figure out if there is an "Id" in MobaXTerm (and the relevance if there is)
                // TODO Add icon to folder
                folderId = Guid.NewGuid().ToString();

                var connectionRecord = new ContainerInfo(folderId);
                connectionRecord.Name = folderName;
                folderMapping.Add(connectionRecord, "");
            }

            //Iterate through all the keys in the section, these contain the connections
            foreach (KeyData connection in folder.Keys)
            {
                // Skip administration keys: folder name and icon identifier
                if (("SubRep" != connection.KeyName) && ("ImgNum" != connection.KeyName))
                {
                    var connectionInfo = ParseSingleBookmark(connection.KeyName, connection.Value);
                    folderMapping.Add(connectionInfo, folderId); // Second argument is the parent
                }
            }

            return folderMapping;
        }

        private ConnectionInfo ParseSingleBookmark(string name, string values)
        {
            // TODO Figure out if there is an "Id" in MobaXTerm (and the relevance if there is)
            var nodeId = Guid.NewGuid().ToString();

            var connectionRecord = new ConnectionInfo(nodeId);

            connectionRecord.Name = string.IsNullOrEmpty(name)
                ? "Unnamed connection"
                : name;
            /*
            *          TODO: copy / paste from CsvConnections deserializer: actually parse the current value
                        connectionRecord.Description =
                            headers.Contains("Description") ? connectionCsv[headers.IndexOf("Description")] : "";
                        connectionRecord.Icon = headers.Contains("Icon") ? connectionCsv[headers.IndexOf("Icon")] : "";
                        connectionRecord.Panel = headers.Contains("Panel") ? connectionCsv[headers.IndexOf("Panel")] : "";
                        connectionRecord.Username = headers.Contains("Username") ? connectionCsv[headers.IndexOf("Username")] : "";
                        connectionRecord.Password = headers.Contains("Password") ? connectionCsv[headers.IndexOf("Password")] : "";
                        connectionRecord.Domain = headers.Contains("Domain") ? connectionCsv[headers.IndexOf("Domain")] : "";
                        connectionRecord.Hostname = headers.Contains("Hostname") ? connectionCsv[headers.IndexOf("Hostname")] : "";
                        connectionRecord.VmId = headers.Contains("VmId") ? connectionCsv[headers.IndexOf("VmId")] : "";
                        connectionRecord.PuttySession = headers.Contains("PuttySession") ? connectionCsv[headers.IndexOf("PuttySession")] : "";
                        connectionRecord.LoadBalanceInfo = headers.Contains("LoadBalanceInfo")
                            ? connectionCsv[headers.IndexOf("LoadBalanceInfo")]
                            : "";
            */

            return connectionRecord;
        }
    }
}