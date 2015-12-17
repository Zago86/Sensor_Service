using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;


namespace Sensor_Service
{
    static public class ConnectionSetAndOpen
    {
        static public List<SerialPort> SP_Port = new List<SerialPort>();
        static public List<Socket> ListSocketDeviceConnected = new List<Socket>();
        static public List<TcpListener> listLISTENER = new List<TcpListener>();
        
        static public void OpenConnection()
        {
            try
            {
                lock (ConnectedDevices.TotaldeviceConnected)
                {
					//per non riavviare sempre socket già aperti
                    int iNumListenerOld = listLISTENER.Count;
		            for (int iAllDevice = 0; iAllDevice < ConnectedDevices.TotaldeviceConnected.Count; iAllDevice++)
                    {
                        ConnectedDevices.ModelAndNameDevice device = ConnectedDevices.TotaldeviceConnected[iAllDevice];
                        switch (device.sensor_connectionType)
                        {
                            case "RS485":
                                {
                                    OpenSerialPort(ref device);                                    
                                }
                                break;

                            case "TCP/IP":
                                {
                                    string endPoint = device.sensor_address + ":7000";

                                    if (listLISTENER.FindAll(x => x.LocalEndpoint.ToString() == endPoint).Count < 1)
                                    {
                                        listLISTENER.Add(new TcpListener(IPAddress.Parse(device.sensor_address), 7000));
                                    }
                                }
                                break;
                        }
                    }
                    if(iNumListenerOld < listLISTENER.Count && listLISTENER != null)
                    {
                        //inizio l'ascolto per ogni indirizzo IP che non è stato ancora aperto(caso in cui aggiunta di un nuovo file o cambio)
                        InitListenerTCP_IP(ref listLISTENER, ref ConnectedDevices.TotaldeviceConnected, iNumListenerOld);
                    }                                      
                }
            }
            catch (Exception e)
            {
                Sensor.logger.Error(e.ToString);                
            }             
        }

        static public void CloseConnectionOpened(string FilePath)
        {
            //chiudo le connessioni che sono aperte
            if (ConnectedDevices.TotaldeviceConnected != null && ConnectedDevices.TotaldeviceConnected.Count > 0)
            {
                List<ConnectedDevices.ModelAndNameDevice> tempList = ConnectedDevices.TotaldeviceConnected.FindAll(x => x.pathFileConnection == FilePath);
                foreach (ConnectedDevices.ModelAndNameDevice tempDevice in tempList)
                {
                    //COMUNICAZIONE TCP/IP
                    if (tempDevice.sensor_connectionType == "TCP/IP")
                    {
                        //chiudo l'ascolto e lo rimuovo dalla lista
                        if (listLISTENER != null && listLISTENER.Count > 0)
                        {
                            List<TcpListener> tempListener = listLISTENER.FindAll(x => x.LocalEndpoint.ToString().Split(':')[0] == tempDevice.sensor_address);
                            foreach (TcpListener ilistener in tempListener)
                            {
                                ilistener.Stop();
                                TickData.SetConnectionStatus(tempDevice.sensor_connectionType, ilistener.LocalEndpoint.ToString().Split(':')[0], ConnectedDevices.StatusConnectionDevis.NotConnected);
                            }
                            listLISTENER.RemoveAll(x => x.LocalEndpoint.ToString().Split(':')[0] == tempDevice.sensor_address);
                        }                        
                    }
                    //COMUNICAZIONE RS485
                    if (tempDevice.sensor_connectionType == "RS485")
                    {
                        //chuodo le porte com aperte
                        if (SP_Port != null && SP_Port.Count > 0)
                        {
                            List<SerialPort> tempSerialPort = SP_Port.FindAll(x => x.PortName == tempDevice.sensor_address);
                            foreach (SerialPort iSerialPort in tempSerialPort)
                            {
                                iSerialPort.Close();
                                TickData.SetConnectionStatus(tempDevice.sensor_connectionType, tempDevice.sensor_address, ConnectedDevices.StatusConnectionDevis.NotConnected);
                            }
                            SP_Port.RemoveAll(x => x.PortName == tempDevice.sensor_address);
                        }
                    }
                }
            }
        }


        #region TCP/IP

        private static void InitListenerTCP_IP(ref List<TcpListener> listLISTENER, ref List<ConnectedDevices.ModelAndNameDevice> totaldeviceConnected, int idx)
        {
            for(int i = idx; i < listLISTENER.Count; i++)
            {
                DoBeginAcceptSocket(listLISTENER[i], ref totaldeviceConnected);
            }           
        }

        private static void DoBeginAcceptSocket(TcpListener listener, ref List<ConnectedDevices.ModelAndNameDevice> totaldeviceConnected)
        {
            try
            {
                // Start to listen for connections from a client.
                Sensor.logger.Info("Waiting for a connection {0}", listener.LocalEndpoint.ToString());
                
                // Accept the connection. 
                // BeginAcceptSocket() creates the accepted socket.
                listener.Start();
                listener.BeginAcceptSocket(new AsyncCallback(DoAcceptSocketCallback), listener);                              
                
                //var prova = from pippo in totaldeviceConnected where pippo.sensor_address == listener.LocalEndpoint.ToString().Split(':')[0] select pippo;
            }
            catch(Exception)
            {
                listener.Stop();                
                TickData.SetConnectionStatus("TCP/IP", listener.LocalEndpoint.ToString().Split(':')[0], ConnectedDevices.StatusConnectionDevis.NotConnected);
            }
        }

        private static void DoAcceptSocketCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            try
            {

                Socket clientSocket = listener.EndAcceptSocket(ar);
                ListSocketDeviceConnected.Add(clientSocket);
                //setto lo stato della connessione che poi invio al db               
                TickData.SetConnectionStatus("TCP/IP", listener.LocalEndpoint.ToString().Split(':')[0], ConnectedDevices.StatusConnectionDevis.Connected);
                
                listener.BeginAcceptSocket(DoAcceptSocketCallback, listener);
                Sensor.logger.Info("Client connected: {0} ", listener.LocalEndpoint.ToString());
            }
            catch (Exception)
            {
                listener.Stop();                
                TickData.SetConnectionStatus("TCP/IP", listener.LocalEndpoint.ToString().Split(':')[0], ConnectedDevices.StatusConnectionDevis.NotConnected);
            }
        }      

        static public List<Socket> GetListOfSocket()
        {
            lock (ListSocketDeviceConnected)
            {
                return ListSocketDeviceConnected;
            }
        }

        internal static void DeleteSocket(Socket tempSocket)
        {
            lock (ListSocketDeviceConnected)
            {
                ListSocketDeviceConnected.Remove(tempSocket);
            }
        }
        #endregion TCP/IP

        #region PORTA RS485
        static private void OpenSerialPort(ref ConnectedDevices.ModelAndNameDevice device)
        {
            try
            {
                SerialPort serialPortToOpen =  ConfigSerialPort(device.sensor_address); 
                if(serialPortToOpen!= null)
                {
                    serialPortToOpen.Open();
                    Sensor.logger.Info("SerialPort: {0} open", serialPortToOpen.PortName);

                    SP_Port.Add(serialPortToOpen);                    
                    TickData.SetConnectionStatus(device.sensor_connectionType, device.sensor_address, ConnectedDevices.StatusConnectionDevis.Connected);
                }
                else //se è uguale a null significa che la porta è già aperta
                {
                    TickData.SetConnectionStatus(device.sensor_connectionType, device.sensor_address, ConnectedDevices.StatusConnectionDevis.Connected);
                }                            
            }
            catch (Exception ex)
            {                
                TickData.SetConnectionStatus(device.sensor_connectionType, device.sensor_address, ConnectedDevices.StatusConnectionDevis.NotConnected);
                Sensor.logger.Error(ex.ToString);
            }
        }

        static private SerialPort ConfigSerialPort(string szPort)
        {
            SerialPort serialPortTemp = null;           
            if (SP_Port.Count == 0)
            {
                serialPortTemp = new SerialPort(szPort);

                serialPortTemp.BaudRate = Sensor_Service.Properties.Settings.Default.BaudRate;
                
                SetParity(serialPortTemp, Sensor_Service.Properties.Settings.Default.Parity);
                SetStopBits(serialPortTemp, Sensor_Service.Properties.Settings.Default.StopBits);                
            }

            for (int iSerialPort = 0; iSerialPort < SP_Port.Count; iSerialPort++ )
            {
                if(SP_Port[iSerialPort].PortName != szPort)
                { 
                    serialPortTemp = new SerialPort(szPort);
            
                    serialPortTemp.BaudRate = Sensor_Service.Properties.Settings.Default.BaudRate;
                    SetParity(serialPortTemp, Sensor_Service.Properties.Settings.Default.Parity);
                    SetStopBits(serialPortTemp, Sensor_Service.Properties.Settings.Default.StopBits);                    
                }                            
            }
            return serialPortTemp;
        }

        static private int SetStopBits(SerialPort port, int stopBits)
        {
            int iRet = 0;
            switch (stopBits)
            {
                case 0:
                    port.StopBits = StopBits.None;
                    break;
                case 1:
                    port.StopBits = StopBits.One;
                    break;
                case 2:
                    port.StopBits = StopBits.Two;
                    break;
                case 3:
                    port.StopBits = StopBits.OnePointFive;
                    break;

                default:
                    iRet = -1;
                    break;
            }
            if (iRet == 0)
            {
                Sensor.logger.Info("Setting StopBits: {0}", port.StopBits);
            }
            else
            {
                Sensor.logger.Error("Error Setting StopBits");
            }
            return iRet;

        }

        static private int SetParity(SerialPort port, int iParity)
        {
            int iRet = 0;
            switch (iParity)
            {
                case 0:
                    port.Parity = Parity.None;
                    break;
                case 1:
                    port.Parity = Parity.Odd;
                    break;
                case 2:
                    port.Parity = Parity.Even;
                    break;
                case 3:
                    port.Parity = Parity.Mark;
                    break;
                case 4:
                    port.Parity = Parity.Space;
                    break;

                default:
                    iRet = -1;
                    break;
            }

            if (iRet == 0)
            {
                Sensor.logger.Info("Setting Parity: {0}", port.Parity);   
            }
            else
            {
                Sensor.logger.Error("Error Setting Parity");
            }

            return iRet;
        }

        static public List<SerialPort> GetListOfSerialPort()
        {
            lock (SP_Port)
            {
                return SP_Port;
            }
        }
        internal static void DeleteSerialPort(SerialPort tempSerialPort)
        {
            lock (SP_Port)
            {
                SP_Port.Remove(tempSerialPort);
            }
        }
        
        #endregion PORTA RS485

        #region CONNECTION STATUS
        static public bool GetConnectionStatus()
        {
            if (SP_Port == null && SP_Port.Count == 0)
            {
                return false;
            }
            else if(SP_Port.Count == 0)
            {
                return false;
            }
            else
            {
                return true;
            }

        }       

        internal static void CloseComunication( ref List<ConnectedDevices.ModelAndNameDevice> connectedDevices)
        {
            lock(connectedDevices)
            {
                if (SP_Port != null && SP_Port.Count > 0)
                {
                    for (int iSerialPort = 0; iSerialPort < SP_Port.Count; iSerialPort++)
                    {
                        SP_Port[iSerialPort].Close();

                        for (int iDevice = 0; iDevice < connectedDevices.Count; iDevice++)
                        {
                            ConnectedDevices.ModelAndNameDevice deviceTemp = connectedDevices[iDevice];
                            if (deviceTemp.sensor_address == SP_Port[iSerialPort].PortName)
                            {                                
                                deviceTemp.sensor_statusConnection = ConnectedDevices.StatusConnectionDevis.NotConnected;
                                connectedDevices[iDevice] = deviceTemp;
                            }
                        }
                        Sensor.logger.Info("SerialPort: {0} Close", SP_Port[iSerialPort].PortName); 
                    }
                    SP_Port.Clear();
                }

                //chiudo tutti i socket
                if (ListSocketDeviceConnected.Count > 0 && ListSocketDeviceConnected != null)
                {
                    for (int iSocket = 0; iSocket < ListSocketDeviceConnected.Count; iSocket++)
                    {
                        ListSocketDeviceConnected[iSocket].Close();
                        Sensor.logger.Info("Socket Close");
                    }                    
                }
            }            
        }       
        #endregion CONNECTION STATUS
    }
}
