using System;
using System.ServiceProcess;
using NLog;
using System.Threading;
using System.Timers;

namespace Sensor_Service
{
    public partial class Sensor : ServiceBase
    {
        #region VARIABILI
        public static Logger logger = LogManager.GetCurrentClassLogger();

        public static ConnectedDevices connectedDevices;

        TickData varTickData;

        public static DB_MYSQL DataBase;

        System.Timers.Timer timerConnectionStatus;

        #endregion VARIABILI

        public Sensor()
        {
            InitializeComponent();
            logger.Info("Avvio Servizio Sensor");
        }

        protected override void OnStart(string[] args)
        {
            //avvio il servizio del Database 'MySQL57'
            MySQL_StartService();
            connectedDevices = new ConnectedDevices();
            //creo il database e la tabella se non esiste
            if(ConnectedDevices.TotaldeviceConnected.Count > 0)
            {
                DataBase = new DB_MYSQL(connectedDevices);

                connectedDevices.OpenConnectionDevice();
                connectedDevices.StartTimerPeriodicMessage();
                //timer per vedere lo stato della comunicazione dei vari dispositivi
                SetAndStartTimerConnectionStatus();

                varTickData = new TickData();
            }            
        }

        protected override void OnStop()
        {
            if (connectedDevices != null)
            {
                if (varTickData != null)
                {
                    varTickData.StopThreds();
                }
                ConnectionSetAndOpen.CloseComunication(ref ConnectedDevices.TotaldeviceConnected);                
            }

            MySQL_StopService();
            logger.Info("Servizio Sensor Stoppato");
        }

        #region Connection & Comunication Status
        private void SetAndStartTimerConnectionStatus()
        {
            timerConnectionStatus = new System.Timers.Timer();
            timerConnectionStatus.Interval = 3000;
            timerConnectionStatus.Elapsed += OnTimerEventConnectionStatus;
            timerConnectionStatus.AutoReset = true;
            timerConnectionStatus.Start();
        }

        private void OnTimerEventConnectionStatus(object sender, ElapsedEventArgs e)
        {
            if (DataBase != null)
            {
                lock (ConnectedDevices.TotaldeviceConnected)
                {
                    foreach (ConnectedDevices.ModelAndNameDevice device in ConnectedDevices.TotaldeviceConnected)
                    {
                        DataBase.SendMessageStatus(device);
                    }
                }                
            }
        }

        #endregion Connection & Comunication Status

        #region Servizio MySQL
        private void MySQL_StartService()
        {
            ServiceController[] scServices;
            scServices = ServiceController.GetServices();

            foreach (ServiceController scTemp in scServices)
            {

                if (scTemp.ServiceName == "MySQL57")
                {
                    // Display properties for the Simple Service sample
                    // from the ServiceBase example.
                    ServiceController sc = new ServiceController("MySQL57");

                    try
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            sc.Start();
                            while (sc.Status == ServiceControllerStatus.Stopped)
                            {
                                Thread.Sleep(1000);
                                sc.Refresh();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }

        private void MySQL_StopService()
        {             
            ServiceController[] scServices = ServiceController.GetServices();
            
            foreach (ServiceController scTemp in scServices)
            {

                if (scTemp.ServiceName == "MySQL57")
                {
                    // Display properties for the Simple Service sample
                    // from the ServiceBase example.
                    ServiceController sc = new ServiceController("MySQL57");
                    try
                    {
                        if (sc.Status != ServiceControllerStatus.Stopped)
                        {
                            sc.Stop();
                            while (sc.Status != ServiceControllerStatus.Stopped)
                            {
                                Thread.Sleep(1000);
                                sc.Refresh();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
        }
        #endregion Servizio MySQL
    }
}
