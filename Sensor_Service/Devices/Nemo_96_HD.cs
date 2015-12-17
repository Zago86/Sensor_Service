using System;
using System.IO.Ports;


namespace Sensor_Service
{
    
    public class Nemo_96_HD : ParametersMonitored
    {
        #region Variabili
        //private struct Tdata
        //{
        //    public UInt32 V1, V2, V3;      //Tensione fase neutro mV
        //    public UInt32 I1, I2, I3;      //Correnti mA
        //    public UInt32 P, Q, S;         //P: Potenza Attiva Q: Potenza reattiva S: Potenza apparente
        //    public UInt32 ET;              //Energia attiva positiva
        //    public UInt32 U1, U2, U3;      //Tensione fase fase
        //    public UInt16 FR;              //Frequenza
        //    public Int16 PF;              //Fattore di potenza
        //    public UInt16 SPF;             //Settore fattore potenza
        //    public UInt32 ETR;             //Energia reattiva positiva
        //    public UInt16 PSIGN;           //Segno della potenza attiva
        //    public UInt16 QSIGN;           //Segno della potenza reattiva
        //    public UInt32 PM;              //Potenza media
        //    public UInt32 PMAX;            //Potenza massima picco

        //}                
        private TimeSpan tot_time;
        private UInt32 tot_ET;
        private DateTime last_time;
        private UInt32 last_ET;
        TimeSpan delta_time;
        UInt32 delta_ET;
        Double avg_pow;

        private enum State
        {
            Init, Disabled, Start, WaitTx, WaitTx1, WaitRx, WaitRx1
        }

        //private Tdata data;
        private State state;
        byte[] tx_buffer;
        byte[] rx_buffer;
        private int ticks;
        public bool click, hold;
        private bool first_scan;

        public string comunicationType;
        public string ComunicationType
        {
            get
            {
                return comunicationType;
            }
            set
            {
                comunicationType = value;
            }
        }
        #endregion Variabili

        public Nemo_96_HD()
        {
            hold = true;
            state = State.Init;
        }

        private void Convert()
        {
            int IdxConvert = 3; // parto dalla 3 posizione perchè alla pos 0° ho il numero di slave, alla 1° il num di funzione
                                       //2° ho il numero di byte letti; 
            v1 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            v2 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            v3 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            i1 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            i2 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            i3 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            p = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            q = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4; 
            s = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            et = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            u1 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            u2 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            u3 = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            //+4
            //ERRORE NELLA DOCUMENTAZIONE
            IdxConvert = IdxConvert + 10;
            fr = BitConverter.ToUInt16(rx_buffer, IdxConvert); //61
            //+2
            IdxConvert = IdxConvert + 4;
            pf = BitConverter.ToInt16(rx_buffer, IdxConvert); //65
            if (pf < 0) pf = (short)(0 - pf);

            IdxConvert = IdxConvert + 2;
            spf = BitConverter.ToUInt16(rx_buffer, IdxConvert);//67
            //+4
            IdxConvert = IdxConvert + 4;
            etr = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            psign = BitConverter.ToUInt16(rx_buffer, IdxConvert);
            //+4
            IdxConvert = IdxConvert + 6;
            qsign = BitConverter.ToUInt16(rx_buffer, IdxConvert); //81
            //+6
            IdxConvert = IdxConvert + 4;
            pm = BitConverter.ToUInt32(rx_buffer, IdxConvert);
            IdxConvert = IdxConvert + 4;
            pmax = BitConverter.ToUInt32(rx_buffer, IdxConvert); //89
            
            //accomulators
            if (first_scan)
            {
                last_time = DateTime.Now;
                last_ET = et;
                first_scan = false;
            }

            delta_time = tot_time + (DateTime.Now - last_time);
            delta_ET = tot_ET + (et - last_ET);

            if (!hold)
                avg_pow = pm / 100000.0;
        }

        static public DataMonitored Convert(byte[] messageToRead)
        {
            DataMonitored DataRead = new DataMonitored();         
                           
            int IdxConvert = 3; // parto dalla 3 posizione perchè alla pos 0° ho il numero di slave, alla 1° il num di funzione
                                //2° ho il numero di byte letti; 
            DataRead.V1 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.V2 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.V3 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.I1 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.I2 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.I3 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.P = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.Q = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.S = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.ET = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.U1 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.U2 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.U3 = BitConverter.ToUInt32(messageToRead, IdxConvert);
            //+4
            //ERRORE NELLA DOCUMENTAZIONE
            IdxConvert = IdxConvert + 10;
            DataRead.FR = BitConverter.ToUInt16(messageToRead, IdxConvert); //61
            //+2
            IdxConvert = IdxConvert + 4;
            DataRead.PF = BitConverter.ToInt16(messageToRead, IdxConvert); //65
            if (DataRead.PF < 0) DataRead.PF = (short)(0 - DataRead.PF);

            IdxConvert = IdxConvert + 2;
            DataRead.SPF = BitConverter.ToUInt16(messageToRead, IdxConvert);//67
            //+4
            IdxConvert = IdxConvert + 4;
            DataRead.ETR = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.PSIGN = BitConverter.ToUInt16(messageToRead, IdxConvert);
            //+4
            IdxConvert = IdxConvert + 6;
            DataRead.QSIGN = BitConverter.ToUInt16(messageToRead, IdxConvert); //81
            //+6
            IdxConvert = IdxConvert + 4;
            DataRead.PM = BitConverter.ToUInt32(messageToRead, IdxConvert);
            IdxConvert = IdxConvert + 4;
            DataRead.PMAX = BitConverter.ToUInt32(messageToRead, IdxConvert); //89
                             
            DataRead.DatetimeReadData = DateTime.Now.ToString(DB_MYSQL.formatDateTime);

            return DataRead;
            ////accomulators
            //if (first_scan)
            //{
            //    last_time = DateTime.Now;
            //    last_ET = et;
            //    first_scan = false;
            //}

            //delta_time = tot_time + (DateTime.Now - last_time);
            //delta_ET = tot_ET + (et - last_ET);

            //if (!hold)
            //    avg_pow = pm / 100000.0;
        }

        int iByteRead = 0;
        public bool TickData(SerialPort port)
        {
            switch (state)
            {
                case State.Init:
                    //con il messaggio sotto vado a leggere V1_max, V2_max, V3_max
                    ModBus.BuildMessage(1, 3, 964, 6, out tx_buffer);                    

                    last_ET = 0;
                    last_time = DateTime.Now;
                    ticks = 0;                    
                    state = State.Disabled;
                    break;

                case State.Disabled:
                    if (click)
                    {
                        if (port.IsOpen)
                        {
                            //START CLICK
                            state = State.Start;
                            first_scan = true;
                        }
                        click = false;
                    }
                    else
                        return true;
                    break;

                case State.Start:
                    if (click)
                    {
                        //STOP CLICK                  
                        tot_ET += et - last_ET;
                        tot_time += DateTime.Now - last_time;

                        state = State.Disabled;
                        click = false;
                    }
                    else
                    {
                        port.DiscardInBuffer();
                        state = State.WaitTx;
                    }
                    break;

                case State.WaitTx:

                    //lblTX.BackColor = Color.Yellow;
                    port.Write(tx_buffer, 0, tx_buffer.Length);
                    state = State.WaitTx1;
                    break;

                case State.WaitTx1:
                    if (port.BytesToWrite == 0)
                    {
                        //lblTX.BackColor = Color.Gray;
                        state = State.WaitRx;
                    }
                    break;

                case State.WaitRx:
                    if (port.BytesToRead > 0)
                    {
                        Sensor.logger.Debug("Byte letti {0}", port.BytesToRead);                        
                        iByteRead = port.BytesToRead;                        
                        state = State.WaitRx1;
                    }
                    else if (ticks++ >= 50)
                    {
                       // lblRX.BackColor = Color.Red;
                        state = State.Start;
                        return true;
                    }
                    break;

                case State.WaitRx1:
                    if (port.BytesToRead == iByteRead)
                    {
                        rx_buffer = new byte[iByteRead];
                       
                        port.Read(rx_buffer, 0, iByteRead);                        
                        //Convert();                       
                       // lblRX.BackColor = Color.Gray;
                        state = State.Start;
                        return true;
                    }
                    else if (ticks++ >= 5)
                    {
                       // lblRX.BackColor = Color.Red;
                        state = State.Start;
                        return true;
                    }
                    break;
            }

            return false;
        }
       
        internal override void Read(byte[] buffer)
        {
            buffer = tx_buffer;
        }

        internal override byte[] Write()
        {
            byte[] prova = null;
            return prova;
        }
    }
}
