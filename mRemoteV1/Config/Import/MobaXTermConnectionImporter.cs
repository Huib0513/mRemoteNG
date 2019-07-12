﻿using System.IO;
using System.Linq;
using mRemoteNG.App;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.Serializers.Csv;
using mRemoteNG.Container;
using mRemoteNG.Messages;

namespace mRemoteNG.Config.Import
{
    public class MobaXTermConnectionImporter : IConnectionImporter<string>
    {
        public void Import(string filePath, ContainerInfo destinationContainer)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, "Unable to import file. File path is null.");
                return;
            }

            if (!File.Exists(filePath))
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                                                    $"Unable to import file. File does not exist. Path: {filePath}");

            var dataProvider = new FileDataProvider(filePath);
            var iniString = dataProvider.Load();
            var mobaXtermConnectionsDeserializer = new MobaXTermConnectionsDeserializer();
            var connectionTreeModel = mobaXtermConnectionsDeserializer.Deserialize(iniString);

            var rootImportContainer = new ContainerInfo { Name = Path.GetFileNameWithoutExtension(filePath) };
            rootImportContainer.AddChildRange(connectionTreeModel.RootNodes.First().Children.ToArray());
            destinationContainer.AddChild(rootImportContainer);

            return;
        }
    }
}
