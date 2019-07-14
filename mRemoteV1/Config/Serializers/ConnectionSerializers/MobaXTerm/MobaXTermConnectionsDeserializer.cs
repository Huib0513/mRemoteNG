using System;
using System.Collections.Generic;
using System.Linq;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
/*using mRemoteNG.Connection.Protocol.Http;
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
        private static readonly Dictionary<string, ProtocolType> _connectionTypes = new Dictionary<string, ProtocolType>
                {
                    {"0", ProtocolType.SSH2 },
                    {"1", ProtocolType.Telnet },
                    {"2", ProtocolType.Rlogin },
                    {"4", ProtocolType.RDP },
                    {"5", ProtocolType.VNC },
                    {"11", ProtocolType.HTTPS } // TODO: Check for HTTP, ProtocolType.HTTP = 7
                        //{"3", "Xdmcp" }, // Not supported?
                        //{"6", "FTP"}, // Not supported?
                        //{"7", "sFTP" }, // Not supported?
                        //{"8", "Serial" }, // Open a serial port
                        //{"9", "File" }, // Open a local file
                        //{"10", "Shell" }, //Local shell
                        //{"12", "Mosh" },
                        //{"13", "AWS S3" },
                        //{"14", "Windows Subsystem for Linux"}
                        //ProtocolType.RAW,
                        //ProtocolType.ICA = 9,
                        //ProtocolType.IntApp = 20
        };

        public ConnectionTreeModel Deserialize(string serializedData)
        {
            var iniparser = new IniDataParser();
            var inidata = iniparser.Parse(serializedData);

            // Root of the new connection tree
            var root = new RootNodeInfo(RootNodeType.Connection);

            //Iterate through all the sections in the ini file
            foreach (SectionData section in inidata.Sections)
            {
                // Multiple folders may exist starting with "Bookmarks_", these are folders containing connections in a tree structure
                if ("Bookmarks" == section.SectionName.Substring(0, Math.Min(9, section.SectionName.Length)))
                {
                    root.AddChild(ParseFolder(section));
                    // TODO hierarchy: fix root node and create tree of subnodes
                    /*
                        [Bookmarks_2]
                        SubRep=Test
                        ImgNum=41
                        Pi Zero Bredeweg intern=#129#0%192.168.8.99%22%pi%%-1%-1%%%22%%0%0%0%_ProfileDir_\Portables\MobaXterm\slash\ed25519-DellXPS.ppk%%-1%0%0%0%%1080%%0%0%1#MobaFont%10%0%0%0%15%236,236,236%0,0,0%180,180,192%0%-1%0%%xterm%-1%0%0,0,0%54,54,54%255,96,96%255,128,128%96,255,96%128,255,128%255,255,54%255,255,128%96,96,255%128,128,255%255,54,255%255,128,255%54,255,255%128,255,255%236,236,236%255,255,255%80%24%0%1%-1%<none>%%0#0#

                        [Bookmarks_3]
                        SubRep=Test\Test level 2
                    */
                }
            }

            var connectionTreeModel = new ConnectionTreeModel();
            connectionTreeModel.AddRootNode(root);
            return connectionTreeModel;
        }

        private ContainerInfo ParseFolder(SectionData folder)
        {
            // Create Container for the current list of connections
            var folderId = Guid.NewGuid().ToString();
            var folderName = folder.Keys["SubRep"];
            var connectionFolder = new ContainerInfo(folderId);
            connectionFolder.Name = folderName;

            // TODO Figure out if there is an "Id" in MobaXTerm (and the relevance if there is)
            // TODO Add icon to folder

            //Iterate through all the keys in the section, these contain the connections
            foreach (KeyData connection in folder.Keys)
            {
                // Skip administration keys: folder name and icon identifier
                if (("SubRep" != connection.KeyName) && ("ImgNum" != connection.KeyName))
                {
                    var connectionInfo = ParseSingleBookmark(connection.KeyName, connection.Value);
                    connectionFolder.AddChild(connectionInfo);
                    //folderMapping.Add(connectionInfo, folderId); // Second argument is the parent
                }
            }

            return connectionFolder;
        }

        private ConnectionInfo ParseSingleBookmark(string name, string value)
        {
            // TODO Figure out if there is an "Id" in MobaXTerm (and the relevance if there is)
            var nodeId = Guid.NewGuid().ToString();

            var connectionRecord = new ConnectionInfo(nodeId);

            connectionRecord.Name = string.IsNullOrEmpty(name)
                ? "Unnamed connection"
                : name;
            connectionRecord.Hostname = configvalues[1];

            // All values are seperated by '%'
            var configvalues = value.Split('%');

            // The first part of the values looks like "#3-digit icon id#protocol id": split based on #
            // Try to retrieve the connection type, if it does not exist, the protocol is not supported
            if (_connectionTypes.TryGetValue(configvalues[0].Split('#')[2], out var connectionType))
            {
                // TODO Check if the field order is SSH protocol specific!
                connectionRecord.Protocol = connectionType;
                connectionRecord.Port = configvalues[2];
                connectionRecord.Username = configvalues[3];

            }
            else
            {
                connectionRecord.Description = Language.strMobaProtocolNotSupported;
            }

            /*
            *          TODO: 
                        connectionRecord.Description;
                        connectionRecord.Icon;
                        connectionRecord.Password;
                        connectionRecord.Domain;
                        connectionRecord.Hostname;
                        connectionRecord.VmId;
                        connectionRecord.PuttySession;
                        connectionRecord.LoadBalanceInfo;
                        And the rest :)
            */

            return connectionRecord;
        }
    }
}