using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

namespace Sensor_Service
{
    public class TickData
    {
        #region VARIABILI       
        private bool active;
        Thread threadTickdata;
        enum STATE_MACHINE_STATE_POLL_LOOP { S_IDLE, S_CHECK_NUM_MESSAGE_WRITE, S_WRITE_MESSAGE, S_READ_MESSAGE };
        private STATE_MACHINE_STATE_POLL_LOOP state;
        private List<ConnectedDevices.MessageToSendAndDeviceInfo> internalMessageToSend;

        Thread threadConvertDataAndSendToDB;
        enum STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB { S_IDLE, S_CHECK_NUM_MESSAGE_CONVERT, CONVERT_MESSAGE, SEND_TO_DB};
        private STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB stateConvertDataAndSendToDB;
        private List<ConnectedDevices.MessageToSendAndDeviceInfo> MessageToConvertAndSendToDB;
        private List<ConnectedDevices.MessageToSendAndDeviceInfo> InternalMessageToConvertAndSendToDB;
        ParametersMonitored.DataMonitored DataReadyToSendToDB;

        SerialPort tempSerialPort;
        ConnectedDevices.MessageToSendAndDeviceInfo tempMessage;
        int iLenght;
        System.Timers.Timer timeMaxWaitResponse = new System.Timers.Timer();
        int iTime = 2500; //ms

        Socket tempSocket;
        
        public struct DataTosend
        {
            public ParametersMonitored.DataMonitored data;
            public ConnectedDevices.ModelAndNameDevice deviceInfo;            
        }

        DataTosend dataForDB;

        int iDeviceInterrogato = -1;

        bool TimeoutRinging;

        #endregion VARIABILI

        public TickData()
        {            
            try
            {
                timeMaxWaitResponse.Interval = iTime;
                timeMaxWaitResponse.Elapsed += OnTimerEventTimeMaxWaitResponse;
                timeMaxWaitResponse.AutoReset = false;
                //threadTickdata
                active = true;
                state = STATE_MACHINE_STATE_POLL_LOOP.S_IDLE;
                threadTickdata = new Thread(pollLoop);
                threadTickdata.Start();
                //threadConvertDataAndSendToDB
                MessageToConvertAndSendToDB = new List<ConnectedDevices.MessageToSendAndDeviceInfo>();
                stateConvertDataAndSendToDB = STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.S_IDLE;
                threadConvertDataAndSendToDB = new Thread(convertDataAndSendToDB);
                threadConvertDataAndSendToDB.Start();
            }
            catch(Exception e)
            {
                Sensor.logger.Error(e.ToString);
            }
        }       


        #region Thread
        private void OnTimerEventTimeMaxWaitResponse(object sender, ElapsedEventArgs e)
        {
            TimeOutMaxWaitResponse();           
        }

        private void TimeOutMaxWaitResponse()
        {
            lock (internalMessageToSend)
            {                
                if (internalMessageToSend.Count > 0)
                {
                    internalMessageToSend.RemoveAt(0);                    
                    SetComunicationStatus(tempMessage.DeviceInfo, ConnectedDevices.StatusConnectionDevis.NotConnected);
                    Sensor.logger.Info("Slave Address {0}: message Timeout", tempMessage.DeviceInfo.sensor_slaveAddress);
                }                
                TimeoutRinging = true;
                timeMaxWaitResponse.Stop();
            }
        }

        private void pollLoop()
        {
            while (active)
            {
                switch (state)
                {                    
                    case STATE_MACHINE_STATE_POLL_LOOP.S_IDLE:
                        {
                            Sensor.logger.Debug("STATE_MACHINE_STATE.S_IDLE");
                            internalMessageToSend = Sensor.connectedDevices.MessaggiDaInviare;
                            if (internalMessageToSend != null)
                            {                         
                                if (internalMessageToSend.Count > 0)
                                {
                                    Sensor.logger.Info("Num Message To send: {0}", internalMessageToSend.Count);
                                    state = STATE_MACHINE_STATE_POLL_LOOP.S_WRITE_MESSAGE;
                                }
                                else
                                {
                                    state = STATE_MACHINE_STATE_POLL_LOOP.S_IDLE;
                                    Thread.Sleep(1000);
                                }
                            }
                            else
                            {
                                state = STATE_MACHINE_STATE_POLL_LOOP.S_IDLE;
                                Thread.Sleep(1000);
                            }
                        }
                    break;
                    
                    case STATE_MACHINE_STATE_POLL_LOOP.S_WRITE_MESSAGE:
                        {
                            Sensor.logger.Debug("STATE_MACHINE_STATE.S_WRITE_MESSAGE");
                            tempMessage = internalMessageToSend[0];                            
                                                    
                            if (tempMessage.messageToSend != null)
                            {                                
                                bool bRet = false;
                                switch (tempMessage.DeviceInfo.sensor_connectionType)
                                {
                                    case "RS485":
                                        {
                                            bRet = WriteMessageRS485();
                                            timeMaxWaitResponse.Start(); //non lo metto anche sul write tcp/ip perchè già ha un timer interno
                                        }
                                        break;
                                    case "TCP/IP":
                                        {
                                            bRet = WriteMessageTCP_IP();
                                        }
                                        break;
                                }                                

                                if (bRet == true)
                                {
                                    //faccio partire il timer la massima attesa di risposta                                    
                                    TimeoutRinging = false;
                                    state = STATE_MACHINE_STATE_POLL_LOOP.S_READ_MESSAGE;
                                }
                                else
                                {
                                    state = STATE_MACHINE_STATE_POLL_LOOP.S_IDLE;
                                }
                                //TODO vedere come gestire se il messaggio è nullo o la porta è chiusa ora vado in Idle
                                //così facendo continuo a inviare sempre lo stesso messaggio sulla stessa porta
                            }
                            
                        }
                        break;                    
                    
                    case STATE_MACHINE_STATE_POLL_LOOP.S_READ_MESSAGE:
                        {
                            Sensor.logger.Debug("STATE_MACHINE_STATE.S_READ_MESSAGE");
                            int iRet = -1;
                            switch (tempMessage.DeviceInfo.sensor_connectionType)
                            {
                                case "RS485":
                                    {
                                        iRet = ReadMessageRS485();
                                        
                                    }
                                    break;
                                case "TCP/IP":
                                    {
                                        iRet = ReadMessageTCP_IP();                                        
                                    }
                                    break;
                            }

                            if (iRet == 0 || TimeoutRinging == true || iRet == 1)
                            {                           
                                state = STATE_MACHINE_STATE_POLL_LOOP.S_IDLE;
                            }
                            //else if(iRet == 1)
                            //{
                            //    state = STATE_MACHINE_STATE_POLL_LOOP.S_IDLE;
                            //}
                            else
                            {
                                state = STATE_MACHINE_STATE_POLL_LOOP.S_READ_MESSAGE;
                            }
                        }
                        break;
                }               
            }
        }       

        private void convertDataAndSendToDB()
        {
            while (active)
            {
                switch (stateConvertDataAndSendToDB)
                {
                    case STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.S_IDLE:
                        {
                            if (MessageToConvertAndSendToDB != null && MessageToConvertAndSendToDB.Count > 0)
                            {
                                lock (MessageToConvertAndSendToDB)
                                {
                                    InternalMessageToConvertAndSendToDB = MessageToConvertAndSendToDB;
                                }

                                stateConvertDataAndSendToDB = STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.CONVERT_MESSAGE;
                            }
                            else
                            {
                                stateConvertDataAndSendToDB = STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.S_IDLE;
                            }
                        }
                        break;

                    case STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.CONVERT_MESSAGE:
                        {
                            ConnectedDevices.MessageToSendAndDeviceInfo tempMessageToConvert = InternalMessageToConvertAndSendToDB[0];

                            if (tempMessageToConvert.messageToRead != null)
                            {
                                //converto in base al tipo di modello    
                                DataReadyToSendToDB = ConvertDataRead(tempMessageToConvert);
                                Sensor.logger.Info("message Slave: {0} convertito", tempMessageToConvert.DeviceInfo.sensor_slaveAddress);
                                dataForDB = new DataTosend();
                                dataForDB.data = DataReadyToSendToDB;
                                dataForDB.deviceInfo = tempMessageToConvert.DeviceInfo;                                
                                stateConvertDataAndSendToDB = STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.SEND_TO_DB;
                            }
                        }
                        break;

                    case STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.SEND_TO_DB:
                        {
                            Sensor.DataBase.SendToDB(dataForDB);
                            InternalMessageToConvertAndSendToDB.RemoveAt(0);
                            stateConvertDataAndSendToDB = STATE_MACHINE_STATE_CONVERT_DATA_SEND_DB.S_IDLE;
                        }
                        break;
                }
            }
        }
        public void StopThreds()
        {
            active = false;
        }

        public int[] Getstate()
        {
            int[] StateAndSlaveAddress = new int[2];
            if (state == STATE_MACHINE_STATE_POLL_LOOP.S_WRITE_MESSAGE)
            {
                StateAndSlaveAddress[0] = 0;
                StateAndSlaveAddress[1] = iDeviceInterrogato;
            }
            else if (state == STATE_MACHINE_STATE_POLL_LOOP.S_READ_MESSAGE)
            {
                StateAndSlaveAddress[0] = 1;
                StateAndSlaveAddress[1] = iDeviceInterrogato;
            }
            else
            {
                StateAndSlaveAddress[0] = 2;
                StateAndSlaveAddress[1] = 0;
            }
            return StateAndSlaveAddress;
        }
        #endregion Thread

        #region TCP/IP
        private bool WriteMessageTCP_IP()
        {
            bool bRet = false;

            List<Socket> tempListSocket = ConnectionSetAndOpen.GetListOfSocket();

            tempSocket = ConnectionSetAndOpen.GetListOfSocket().Find(x => ((IPEndPoint)(x.LocalEndPoint)).Address.ToString() == tempMessage.DeviceInfo.sensor_address);

            if (tempSocket != null)
            {
                if (tempSocket.Connected)
                {
                    try
                    {
                        tempSocket.Send(tempMessage.messageToSend);

                        //ricavo quanto deve essere lungo in messaggio di risposta
                        iLenght = (int)(tempMessage.messageToSend[4] << 8);
                        iLenght += (int)(tempMessage.messageToSend[5]);
                        iLenght = iLenght * 2 + 5;

                        iDeviceInterrogato = tempMessage.DeviceInfo.sensor_slaveAddress;

                        bRet = true;
                    }
                    catch(Exception)
                    {
                        ConnectionSetAndOpen.DeleteSocket(tempSocket);                        
                        SetConnectionStatus(tempMessage.DeviceInfo.sensor_connectionType, tempMessage.DeviceInfo.sensor_address, ConnectedDevices.StatusConnectionDevis.NotConnected);
                    }
                }
                else
                {
                    //se non c'è la connessione con la porta butto via dalla lista della connessione dei socket il socket non aperto
                    ConnectionSetAndOpen.DeleteSocket(tempSocket);                    
                    SetConnectionStatus(tempMessage.DeviceInfo.sensor_connectionType, tempMessage.DeviceInfo.sensor_address, ConnectedDevices.StatusConnectionDevis.NotConnected);
                }
            }
            else
            {
                lock (internalMessageToSend)
                {
                    internalMessageToSend.RemoveAt(0);
                }
            }

            return bRet;
        }
        private int ReadMessageTCP_IP()
        {
            int iRet = -1;
            if (tempSocket != null)
            {
                try
                {
                    tempSocket.ReceiveTimeout = iTime ;                    

                    if (tempSocket.Receive(tempMessage.messageToRead = new byte[iLenght]) == iLenght)
                    {                        
                        SetComunicationStatus(tempMessage.DeviceInfo, ConnectedDevices.StatusConnectionDevis.Connected);
                        MessageToConvertAndSendToDB.Add(tempMessage);
                    }                  

                    //rimuovo il messaggio che ho gestito dalla lista
                    internalMessageToSend.RemoveAt(0);
                    iRet = 0;
                }
                
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        TimeOutMaxWaitResponse();
                    }
                    else
                    {
                        Sensor.logger.Error(e.ToString);
                        if(internalMessageToSend.Count > 0)
                        {
                            internalMessageToSend.RemoveAt(0);
                        }
                        iRet = 1;
                        SetConnectionStatus(tempMessage.DeviceInfo.sensor_connectionType, tempMessage.DeviceInfo.sensor_address, ConnectedDevices.StatusConnectionDevis.NotConnected);
                    }
                }                           
            }
            return iRet;
        }
        #endregion TCP/IP
        #region RS485
        private bool WriteMessageRS485()
        {
            bool bRet = false;
                       
            tempSerialPort = ConnectionSetAndOpen.GetListOfSerialPort().Find(x => x.PortName.Contains(tempMessage.DeviceInfo.sensor_address));
            if (tempSerialPort != null)
            {
                if (tempSerialPort.IsOpen)
                {
                    tempSerialPort.DiscardInBuffer();
                    tempSerialPort.Write(tempMessage.messageToSend, 0, tempMessage.messageToSend.Length);                    

                    //ricavo quanto deve essere lungo in messaggio di risposta
                    iLenght = (int)(tempMessage.messageToSend[4] << 8);
                    iLenght += (int)(tempMessage.messageToSend[5]);
                    iLenght = iLenght * 2 + 5;

                    iDeviceInterrogato = tempMessage.DeviceInfo.sensor_slaveAddress;                   

                    bRet = true;                   
                }
                else
                {
                    //se non c'è la connessione con la porta butto via dalla lista della connessione dei socket il socket non aperto
                    ConnectionSetAndOpen.DeleteSerialPort(tempSerialPort);
                }                
            }
            else
            {
                lock (internalMessageToSend)
                {
                    internalMessageToSend.RemoveAt(0);
                }                
            }
            return bRet;
        }
        private int ReadMessageRS485()
        {
            int iRet = -1;
            if (tempSerialPort != null && tempSerialPort.BytesToRead == iLenght)
            {                
                tempMessage.messageToRead = new byte[iLenght];
                tempSerialPort.Read(tempMessage.messageToRead, 0, iLenght);
                                
                SetComunicationStatus(tempMessage.DeviceInfo, ConnectedDevices.StatusConnectionDevis.Connected);
                MessageToConvertAndSendToDB.Add(tempMessage);

                //rimuovo il messaggio che ho gestito dalla lista
                internalMessageToSend.RemoveAt(0);
                iRet = 0;
            }

            return iRet;
        }
        #endregion RS485

        private ParametersMonitored.DataMonitored ConvertDataRead(ConnectedDevices.MessageToSendAndDeviceInfo tempMessage)
        {
            ParametersMonitored.DataMonitored DataConvert = new ParametersMonitored.DataMonitored(); ;
            switch (tempMessage.DeviceInfo.sensor_modello)
            {
                case "Nemo_96_HD":
                    {
                        DataConvert = Nemo_96_HD.Convert(tempMessage.messageToRead);
                    }
                    break;

                case "Seneca":
                    {
                        //DataConvert = Nemo_96_HD.Convert(tempMessage.messageToRead);
                    }
                    break;
            }
            return DataConvert;
        }

        #region Comunication & Connection Status
        private void SetComunicationStatus(ConnectedDevices.ModelAndNameDevice deviceInfo, ConnectedDevices.StatusConnectionDevis ComunicationStatus)
        {
            lock (ConnectedDevices.TotaldeviceConnected)
            {
                for (int i = 0; i < ConnectedDevices.TotaldeviceConnected.Count; i++)
                {
                    if (ConnectedDevices.TotaldeviceConnected[i].connection_imei == deviceInfo.connection_imei &&
                        ConnectedDevices.TotaldeviceConnected[i].connection_name == deviceInfo.connection_name &&
                        ConnectedDevices.TotaldeviceConnected[i].installation_name == deviceInfo.installation_name &&
                        ConnectedDevices.TotaldeviceConnected[i].pathFileConnection == deviceInfo.pathFileConnection &&
                        ConnectedDevices.TotaldeviceConnected[i].sensor_address == deviceInfo.sensor_address &&
                        ConnectedDevices.TotaldeviceConnected[i].sensor_connectionType == deviceInfo.sensor_connectionType &&
                        ConnectedDevices.TotaldeviceConnected[i].sensor_modello == deviceInfo.sensor_modello &&
                        ConnectedDevices.TotaldeviceConnected[i].sensor_name == deviceInfo.sensor_name &&
                        ConnectedDevices.TotaldeviceConnected[i].sensor_slaveAddress == deviceInfo.sensor_slaveAddress)
                    {
                        ConnectedDevices.ModelAndNameDevice temp = ConnectedDevices.TotaldeviceConnected[i];
                        if (ComunicationStatus == ConnectedDevices.StatusConnectionDevis.Connected)
                        {
                            temp.sensor_statusComunication = ComunicationStatus;
                            temp.sensor_statusConnection = ComunicationStatus;
                        }
                        else
                        {
                            temp.sensor_statusComunication = ComunicationStatus;
                        }
                        temp.sensor_statusComunication = ComunicationStatus;
                        ConnectedDevices.TotaldeviceConnected[i] = temp;
                    }
                }
            }
        }

        internal static void SetConnectionStatus(string ConnectionType, string Address, ConnectedDevices.StatusConnectionDevis ConnectionStatus)
        {
            lock (ConnectedDevices.TotaldeviceConnected)
            {
                for (int i = 0; i < ConnectedDevices.TotaldeviceConnected.Count; i++)
                {
                    if (ConnectedDevices.TotaldeviceConnected[i].sensor_address == Address &&
                        ConnectedDevices.TotaldeviceConnected[i].sensor_connectionType == ConnectionType)
                    {
                        ConnectedDevices.ModelAndNameDevice temp = ConnectedDevices.TotaldeviceConnected[i];
                        if (ConnectionStatus == ConnectedDevices.StatusConnectionDevis.NotConnected)
                        {
                            temp.sensor_statusConnection = ConnectionStatus;
                            temp.sensor_statusComunication = ConnectionStatus;
                        }
                        else
                        {
                            temp.sensor_statusConnection = ConnectionStatus;
                        }
                        temp.sensor_statusConnection = ConnectionStatus;
                        ConnectedDevices.TotaldeviceConnected[i] = temp;
                    }
                }
            }
        }
        #endregion Comunication & Connection Status
    }
}
