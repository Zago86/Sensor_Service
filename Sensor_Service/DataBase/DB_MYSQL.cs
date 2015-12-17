using System;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;

namespace Sensor_Service
{
    public class DB_MYSQL
    {              
        string ConnectionStr = "server=localhost;user=root;port=3306;password=mysql;";
        string CommandCreateDataBase = "CREATE DATABASE IF NOT EXISTS `Data`;";
        static public string formatDateTime = "yyyy-MM-dd HH:mm:ss";

        public System.Data.ConnectionState DB_ConnectionState = System.Data.ConnectionState.Closed;
        
        public DB_MYSQL() { }

        public DB_MYSQL(ConnectedDevices connectedDevices)
        {
            try
            {
                lock(ConnectedDevices.TotaldeviceConnected)
                {
                    using (MySqlConnection DB_Connection = new MySqlConnection(ConnectionStr))
                    {
                        DB_ConnectionState = DB_Connection.State;
                        if (DB_ConnectionState == System.Data.ConnectionState.Closed)
                        {
                            DB_Connection.Open();
                            DB_ConnectionState = DB_Connection.State;
                        }
                        DB_Connection.CreateCommand();
                        using (MySqlCommand DB_Command = DB_Connection.CreateCommand())
                        {
                            DB_Command.CommandText = CommandCreateDataBase;                            
                            DB_Command.ExecuteNonQuery();
                            Sensor.logger.Info("DB Creato");

                            CreateAllTableOnDB();                            
                        }
                    }
                }                                 
            }
            catch (Exception ex)
            {
                Sensor.logger.Error(ex.ToString);
            }
        }

        public void CreateAllTableOnDB()
        {
            using (MySqlConnection DB_Connection = new MySqlConnection(ConnectionStr))
            {
                DB_ConnectionState = DB_Connection.State;
                if (DB_ConnectionState == System.Data.ConnectionState.Closed)
                {
                    DB_Connection.Open();
                    DB_ConnectionState = DB_Connection.State;
                }               
                using (MySqlCommand DB_Command = DB_Connection.CreateCommand())
                {
                    lock(ConnectedDevices.TotaldeviceConnected)
                    { 
                        foreach (ConnectedDevices.ModelAndNameDevice device in ConnectedDevices.TotaldeviceConnected)
                        {
                            DB_Command.CommandText = GetStringCreateTableData(device);
                            DB_Command.ExecuteNonQuery();
                            //creo le tabelle di status
                            DB_Command.CommandText = GetStringCreateTableStatusInstallation(device);
                            DB_Command.ExecuteNonQuery();
                        }
                    }
                }
            }               
        }

        internal void SendToDB(TickData.DataTosend dataReadyToSendToDB)
        {
            try
            {
                using (MySqlConnection DB_Connection = new MySqlConnection(ConnectionStr))
                {
                    DB_ConnectionState = DB_Connection.State;
                    if (DB_ConnectionState == System.Data.ConnectionState.Closed)
                    {
                        DB_Connection.Open();
                        DB_ConnectionState = DB_Connection.State;
                    }
                    using (MySqlCommand DB_Command = DB_Connection.CreateCommand())
                    {
                        string szCommand = ComposeMessage(dataReadyToSendToDB);
                        DB_Command.CommandText = szCommand;
                        DB_Command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Sensor.logger.Error(ex.ToString);
            }

        }        

        private string ComposeMessage(TickData.DataTosend dataReadyToSendToDB)
        {
            string command = string.Format(@"insert into Data.{0}_{1}_{2}(DateTime, V1, V2, V3, I1, I2, I3, P, Q, S, ET,
                        U1, U2, U3, FR, PF, SPF, ETR, PSIGN, QSIGN, PM, PMAX)",
                        dataReadyToSendToDB.deviceInfo.installation_name, dataReadyToSendToDB.deviceInfo.connection_name,
                        dataReadyToSendToDB.deviceInfo.sensor_name);

            string values = string.Format(@" values('{0}','{1}', '{2}', '{3}', '{4}','{5}', '{6}', '{7}', '{8}', '{9}',
                                            '{10}', '{11}', '{12}','{13}', '{14}', '{15}', '{16}', '{17}', '{18}', 
                                            '{19}', '{20}', '{21}');",
                                           dataReadyToSendToDB.data.DatetimeReadData,
                                           dataReadyToSendToDB.data.V1, dataReadyToSendToDB.data.V2, dataReadyToSendToDB.data.V3,
                                           dataReadyToSendToDB.data.I1, dataReadyToSendToDB.data.I2, dataReadyToSendToDB.data.I3,
                                           dataReadyToSendToDB.data.P, dataReadyToSendToDB.data.Q, dataReadyToSendToDB.data.S,
                                           dataReadyToSendToDB.data.ET, dataReadyToSendToDB.data.U1, dataReadyToSendToDB.data.U2,
                                           dataReadyToSendToDB.data.U3, dataReadyToSendToDB.data.FR, dataReadyToSendToDB.data.PF,
                                           dataReadyToSendToDB.data.SPF, dataReadyToSendToDB.data.ETR, dataReadyToSendToDB.data.PSIGN,
                                           dataReadyToSendToDB.data.QSIGN, dataReadyToSendToDB.data.PM, dataReadyToSendToDB.data.PMAX);

            command += values;

            return command;
        }

        private string GetStringCreateTableData(ConnectedDevices.ModelAndNameDevice Device)
        {
            string CommandCreateTable = string.Format(@"CREATE TABLE  IF NOT EXISTS `Data`.`{0}_{1}_{2}` 
                                     (`DateTime` DATETIME NOT NULL,`V1` DOUBLE NULL,
                                     `V2` DOUBLE NULL, `V3` DOUBLE NULL,  `I1` DOUBLE NULL,  `I2` DOUBLE NULL,
                                     `I3` DOUBLE NULL,  `P` DOUBLE NULL,  `Q` DOUBLE NULL,  `S` DOUBLE NULL,
                                     `ET` INT NULL,  `U1` DOUBLE NULL,  `U2` DOUBLE NULL, `U3` DOUBLE NULL,
                                     `FR` DOUBLE NULL,  `PF` DOUBLE NULL, `SPF` DOUBLE NULL,  `ETR` DOUBLE NULL, 
                                     `PSIGN` DOUBLE NULL, `QSIGN` DOUBLE NULL,  `PM` DOUBLE NULL, 
                                     `PMAX` DOUBLE NULL)ENGINE = MyISAM, ROW_FORMAT = FIXED ;"
                                     , Device.installation_name, Device.connection_name, Device.sensor_name);

            return CommandCreateTable;
        }

        private string GetStringCreateTableStatusInstallation(ConnectedDevices.ModelAndNameDevice device)
        {
            string CommandCreateTable = string.Format(@"CREATE TABLE  IF NOT EXISTS `Data`.`{0}_Connection_Status` 
                                     (`DateTime` DATETIME NOT NULL,`Name` VARCHAR(45),`TypeConnection` VARCHAR(45), `Address` VARCHAR(45), `ConnectionStatus` VARCHAR(45),  `ComunicationStatus` VARCHAR(45))ENGINE = MyISAM, ROW_FORMAT = FIXED ;"
                                     , device.installation_name);

            return CommandCreateTable;
        }

        internal void SendMessageStatus(ConnectedDevices.ModelAndNameDevice device)
        {
            try
            {
                if (device.installation_name != null)
                {
                    using (MySqlConnection DB_Connection = new MySqlConnection(ConnectionStr))
                    {
                        DB_ConnectionState = DB_Connection.State;
                        if (DB_ConnectionState == System.Data.ConnectionState.Closed)
                        {
                            DB_Connection.Open();
                            DB_ConnectionState = DB_Connection.State;
                        }
                        using (MySqlCommand DB_CommandTemp = DB_Connection.CreateCommand())
                        {
                            DB_CommandTemp.CommandText = string.Format(@"SELECT * from data.{0}_Connection_status WHERE name ='{1}' 
                                                    order by datetime DESC ", device.installation_name, device.sensor_name);

                            using (MySqlDataReader queryResults = DB_CommandTemp.ExecuteReader())
                            {
                                ConnectedDevices.ModelAndNameDevice deviceOld = new ConnectedDevices.ModelAndNameDevice();

                                while (queryResults.Read())
                                {
                                    Object[] numb = new Object[queryResults.FieldCount];

                                    // Get the Row with all its column values..
                                    Object[] values = new Object[queryResults.FieldCount];
                                    queryResults.GetValues(values);
                                    deviceOld.sensor_name = (string)values[1];
                                    deviceOld.sensor_connectionType = (string)values[2];
                                    deviceOld.sensor_address = (string)values[3];
                                    /////status CONNECTION
                                    if ((string)values[4] == "Connected")
                                    {
                                        deviceOld.sensor_statusConnection = ConnectedDevices.StatusConnectionDevis.Connected;

                                    }
                                    else if ((string)values[4] == "NotConnected")
                                    {
                                        deviceOld.sensor_statusConnection = ConnectedDevices.StatusConnectionDevis.NotConnected;

                                    }
                                    /////status COMUNICATION
                                    if ((string)values[5] == "Connected")
                                    {
                                        deviceOld.sensor_statusComunication = ConnectedDevices.StatusConnectionDevis.Connected;

                                    }
                                    else if ((string)values[5] == "NotConnected")
                                    {
                                        deviceOld.sensor_statusComunication = ConnectedDevices.StatusConnectionDevis.NotConnected;
                                    }
                                    break;    //mi fermo subito perchè io voglio solo l'ultimo inserito, con la query già li mettonì in ordine                
                                }

                                if (!queryResults.HasRows)
                                {
                                    queryResults.Dispose();

                                    DB_CommandTemp.CommandText = string.Format(@"insert into Data.{0}_Connection_Status(DateTime,Name
                                                                           ,TypeConnection, Address, ConnectionStatus, ComunicationStatus) values('{1}','{2}','{3}','{4}','{5}', '{6}')",
                                                                                      device.installation_name, DateTime.Now.ToString(formatDateTime),
                                                                                      device.sensor_name, device.sensor_connectionType,
                                                                                      device.sensor_address, device.sensor_statusConnection, device.sensor_statusComunication);

                                    DB_CommandTemp.ExecuteNonQuery();
                                    //deviceOld.sensor_statusConnection = device.sensor_statusConnection;
                                }
                                else
                                {
                                    if (deviceOld.sensor_statusConnection != device.sensor_statusConnection || deviceOld.sensor_statusComunication != device.sensor_statusComunication)
                                    {
                                        queryResults.Dispose();
                                        DB_CommandTemp.CommandText = string.Format(@"insert into Data.{0}_Connection_Status(DateTime,Name
                                                                       ,TypeConnection, Address, ConnectionStatus, ComunicationStatus) values('{1}','{2}','{3}','{4}','{5}','{6}')",
                                                                       device.installation_name, DateTime.Now.ToString(formatDateTime),
                                                                       device.sensor_name, device.sensor_connectionType,
                                                                       device.sensor_address, device.sensor_statusConnection, device.sensor_statusComunication);
                                        DB_CommandTemp.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Sensor.logger.Error(e.ToString);
            }
        }
    }
}
