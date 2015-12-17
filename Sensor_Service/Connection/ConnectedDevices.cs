using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Timers;
using System.Xml;


namespace Sensor_Service
{
    public class ConnectedDevices
    {
        #region VARIABILI       

        //List<Seneca> iAllDeviceSeneca = new List<Seneca>();
        string[] NameDeviceAccepted = { "Nemo_96_HD", "Seneca" };
        static Timer timerSendMessage = new Timer();

        public enum StatusConnectionDevis { NotConnected, Connected };
        public struct ModelAndNameDevice
        {
            public string pathFileConnection;
            public string installation_name;
            public string connection_name;
            public string connection_imei;
            public string sensor_modello;
            public string sensor_name;
            public string sensor_connectionType;
            public string sensor_address;
            public int sensor_slaveAddress;
            public StatusConnectionDevis sensor_statusConnection;
            public StatusConnectionDevis sensor_statusComunication;
        }

        public static List<ModelAndNameDevice> TotaldeviceConnected;
        public static List<MessageToSendAndDeviceInfo> MessageToSend = new List<MessageToSendAndDeviceInfo>();
        public struct MessageToSendAndDeviceInfo
        {
            public ModelAndNameDevice DeviceInfo;
            public byte[] messageToSend;
            public byte[] messageToRead;
        }

        ModelAndNameDevice deviceInfo;

        FileSystemWatcher watcher;

        public List<MessageToSendAndDeviceInfo> MessaggiDaInviare
        {
            get
            {
                lock (MessageToSend)
                {
                    return MessageToSend;
                }
            }
        }

        string szPathFileConnection = @"E:\Dottorato\Sensor\PrimoStep\SW\Sensor_Service\Sensor_Service\Connection\ ";
        
        #endregion VARIABILI

        public ConnectedDevices()
        {
            try
            {
                //faccio partire il thread che controlla la cartella dove sono presenti le installazioni
                CheckFolderConnection();
                TotaldeviceConnected = new List<ModelAndNameDevice>();
                DectedNumberAndTypeOfDevice();
            }
            catch (Exception e)
            {
                Sensor.logger.Error(e.ToString);                
            }
        }

        private void DectedNumberAndTypeOfDevice()
        {
            string[] szFilesOnFolderConnection = Directory.GetFiles(szPathFileConnection);
            foreach (string FileConnectionTemp in szFilesOnFolderConnection)
            {
                LoadConnectionAndAddList(FileConnectionTemp);
            }
        }

        private void LoadConnectionAndAddList(string fileConnectionTemp)
        {
            XmlDocument xmlDoc = new XmlDocument();
            //crontollo se il file è in uso, se dovesse esserlo rimango dentro il while e aspetto che si liberi
            while (IsFileInUse(new FileInfo(fileConnectionTemp)) == true) { }

            xmlDoc.Load(fileConnectionTemp);

            foreach (XmlNode node in xmlDoc.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element && node.Name == "Installations")
                {
                    foreach (XmlNode node_1 in node.ChildNodes)
                    {
                        foreach (XmlNode node_2 in node_1.ChildNodes)
                        {
                            foreach (XmlNode node_3 in node_2.ChildNodes)
                            {
                                if (node_3.NodeType == XmlNodeType.Element && node_3.Name == "Sensor")
                                {
                                    deviceInfo = new ModelAndNameDevice();
                                    deviceInfo.pathFileConnection = fileConnectionTemp;
                                    deviceInfo.installation_name = node_1.Attributes["name"].Value;
                                    deviceInfo.connection_imei = node_2.Attributes["imei"].Value;
                                    deviceInfo.connection_name = node_2.Attributes["name"].Value;

                                    deviceInfo.sensor_modello = node_3.Attributes["model"].Value;
                                    deviceInfo.sensor_name = node_3.Attributes["name"].Value;
                                    deviceInfo.sensor_connectionType = node_3.Attributes["connectionType"].Value;
                                    deviceInfo.sensor_address = node_3.Attributes["address"].Value;
                                    deviceInfo.sensor_slaveAddress = Convert.ToInt16(node_3.Attributes["slaveAddress"].Value);

                                    lock (TotaldeviceConnected)
                                    {
                                        TotaldeviceConnected.Add(deviceInfo);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void StartTimerPeriodicMessage()
        {
            timerSendMessage.Interval = 10000;
            timerSendMessage.Elapsed += OnTimerEventPeriodicMessage;
            timerSendMessage.AutoReset = true;
            timerSendMessage.Start();
        }

        private void OnTimerEventPeriodicMessage(object sender, ElapsedEventArgs e)
        {
            lock (TotaldeviceConnected)
            {
                if (TotaldeviceConnected.Count > 0)
                {
                    for (int iDevice = 0; iDevice < TotaldeviceConnected.Count; iDevice++)
                    {
                        MessageToSendAndDeviceInfo tempMessageToSendAndDeviceInfo = new MessageToSendAndDeviceInfo();
                        if (TotaldeviceConnected[iDevice].sensor_modello == "Nemo_96_HD")
                        {
                            ModBus.BuildMessage(TotaldeviceConnected[iDevice].sensor_slaveAddress, 3, 769, 47, out tempMessageToSendAndDeviceInfo.messageToSend);
                            tempMessageToSendAndDeviceInfo.DeviceInfo = TotaldeviceConnected[iDevice];
                            MessageToSend.Add(tempMessageToSendAndDeviceInfo);
                        }
                        if (TotaldeviceConnected[iDevice].sensor_modello == "Seneca")
                        {
                            ModBus.BuildMessage(TotaldeviceConnected[iDevice].sensor_slaveAddress, 3, 769, 47, out tempMessageToSendAndDeviceInfo.messageToSend);
                            tempMessageToSendAndDeviceInfo.DeviceInfo = TotaldeviceConnected[iDevice];
                            MessageToSend.Add(tempMessageToSendAndDeviceInfo);
                        }
                    }
                }
            }
        }

        public void OpenConnectionDevice()
        {
            ConnectionSetAndOpen.OpenConnection();
        }

        #region FileSystemWatcher
        private void CheckFolderConnection()
        {
            watcher = new FileSystemWatcher();
            watcher.Path = szPathFileConnection;
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            // Only watch text files.
            watcher.Filter = "*.xml";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnCreated);
            watcher.Deleted += new FileSystemEventHandler(OnDelete);

            watcher.EnableRaisingEvents = true;

        }

        private void OnDelete(object sender, FileSystemEventArgs e)
        {           
            DeleteInstallation(e.FullPath);
            Sensor.logger.Info("{0}, {1}, {2}", e.ChangeType, e.FullPath, TotaldeviceConnected.Count);           
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                LoadConnectionAndAddList(e.FullPath);
                Sensor.DataBase.CreateAllTableOnDB();
                ConnectionSetAndOpen.OpenConnection();
                Sensor.logger.Info("{0}, {1}, {2}", e.ChangeType, e.FullPath, TotaldeviceConnected.Count);
                watcher.EnableRaisingEvents = false; //aggiungo questo perchè altrimenti passa anche per OnChange quando aggiungo un nuovo file
            }
            finally
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            //questo è uno stratagemma per non entrare due volte qui quando cambio il file. 
            try
            {
                watcher.EnableRaisingEvents = false;
                DeleteInstallation(e.FullPath);
                LoadConnectionAndAddList(e.FullPath);
                Sensor.DataBase.CreateAllTableOnDB();
                ConnectionSetAndOpen.OpenConnection();
                Sensor.logger.Info("{0}, {1}, {2}", e.ChangeType, e.FullPath, TotaldeviceConnected.Count);
            }
            finally
            {
                watcher.EnableRaisingEvents = true;
            }            
        }

        private void DeleteInstallation(string FilePath)
        {
            lock (TotaldeviceConnected)
            {
                //chiudo le connessioni che ho aperto inerenti al file che è cambiato o è stato cancellato
                ConnectionSetAndOpen.CloseConnectionOpened(FilePath);
                
                TotaldeviceConnected.RemoveAll(x => x.pathFileConnection == FilePath);                
            }
        }
        #endregion FileSystemWatcher

        #region Utility file check if is in use
        private bool IsFileInUse(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
        #endregion Utility file check if is in use
    }
}
