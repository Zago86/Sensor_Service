using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Sensor_Service
{
    static class ModBus
    {
        static public int iNumByteMessage = 8;
        static ModBus() { }

        enum Function { Invalid_0,
            ReadCoilStatus,
            ReadInputStatus,
            ReadHoldingRegisters,
            ReadInputRegisters,
            ForceSingleCoil,
            PresetSingleRegister,
            ReadExceptionStatus,
            Invalid_1,
            Invalid_2,
            Invalid_3,
            FetchCommEventCounter,
            FetchCommEventLog,
            Invalid_4,
            Invalid_5,
            ForceMultipleCoils,
            PresetMultipleRegisters,
            ReportSlaveID,
            Invalid_6,
            Invalid_7,
            ReadGeneralReference,
            WriteGeneralReference,
            MaskWrite4XRegister,
            ReadWrite4XRegister,
            ReadFIFOQueue
        }


        static public void BuildMessage(int SlaveAddress, int numberfunction, int startAddress, int numberRegistersToRead, out byte[] message)
        {
            message = new byte[8];
            //Array to receive CRC bytes:
            byte[] CRC = new byte[2];

            message[0] = (byte)SlaveAddress;
            message[1] = (byte)numberfunction;
            message[2] = (byte)(startAddress >> 8);
            message[3] = (byte)startAddress;
            message[4] = (byte)(numberRegistersToRead >> 8);
            message[5] = (byte)numberRegistersToRead;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }

        static private void BuildMessage(int SlaveAddress, int numberfunction, out byte[] message)
        {
            message = new byte[4];
            //Array to receive CRC bytes:
            byte[] CRC = new byte[2];

            message[0] = (byte)SlaveAddress;
            message[1] = (byte)numberfunction;            

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }

        static private void BuildMessage(int SlaveAddress, int numberfunction, int startAddress, int numElement, int byteCount, int forceData, out byte[] message)
        {
            //Array to receive CRC bytes:
            message = new byte[11];
            byte[] CRC = new byte[2];

            message[0] = (byte)SlaveAddress;
            message[1] = (byte)numberfunction;
            message[2] = (byte)(startAddress >> 8);
            message[3] = (byte)startAddress;
            message[4] = (byte)(numElement >> 8);
            message[5] = (byte)numElement;
            message[6] = (byte)byteCount;
            message[7] = (byte)(forceData >> 8);
            message[8] = (byte)forceData;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }

        static private void GetCRC(byte[] message, ref byte[] CRC)
        {
            //Function expects a modbus message of any length as well as a 2 byte CRC array in which to 
            //return the CRC values:

            ushort CRCFull = 0xFFFF;
            byte CRCHigh = 0xFF, CRCLow = 0xFF;
            char CRCLSB;

            for (int i = 0; i < (message.Length) - 2; i++)
            {
                CRCFull = (ushort)(CRCFull ^ message[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[1] = CRCHigh = (byte)((CRCFull >> 8) & 0xFF);
            CRC[0] = CRCLow = (byte)(CRCFull & 0xFF);
        }

        static public void ReadCoilStatus(int SlaveAddress, int startAddress, int numElementToRead, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ReadCoilStatus, startAddress, numElementToRead, out message);
        }

        static public void ReadInputStatus(int SlaveAddress, int startAddress, int numElementToRead, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ReadInputStatus, startAddress, numElementToRead, out message);
        }

        static public void ReadHoldingRegisters(int SlaveAddress, int startAddress, int numElementToRead, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ReadHoldingRegisters, startAddress, numElementToRead, out message);
        }

        static public void ReadInputRegisters(int SlaveAddress, int startAddress, int numElementToRead, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ReadInputRegisters, startAddress, numElementToRead, out message);
        }

        static public void ForceSingleCoil(int SlaveAddress, int startAddress, int ForceData, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ForceSingleCoil, startAddress, ForceData, out message);
        }

        static public void PresetSingleRegister(int SlaveAddress, int startAddress, int PresetData, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.PresetSingleRegister, startAddress, PresetData, out message);
        }

        static public void ReadExceptionStatus(int SlaveAddress, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ReadExceptionStatus, out message);
        }

        static public void FetchCommEventCounter(int SlaveAddress, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.FetchCommEventCounter, out message);
        }

        static public void FetchCommEventLog(int SlaveAddress, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.FetchCommEventLog, out message);
        }

        static public void ForceMultipleCoils(int SlaveAddress, int startAddress, int numElement, int byteCount, int forceData, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ForceMultipleCoils, startAddress, numElement, byteCount, forceData, out message);
        }

        static public void PresetMultipleRegisters(int SlaveAddress, int startAddress, int numElement, int byteCount, int[] DataSet, out byte[] message)
        {
            message = new byte[9 + DataSet.Length * 2];

            if (numElement!= DataSet.Length /*|| message.Length != 9 + DataSet.Length*2*/)
            {
                //MessageBoxResult result = MessageBox.Show("Error!! numero elementi da settare è diverso da dataset ", "Error");
            }
            else
            {
                byte[] CRC = new byte[2];

                message[0] = (byte)SlaveAddress;
                message[1] = (byte)Function.PresetMultipleRegisters;
                message[2] = (byte)(startAddress >> 8);
                message[3] = (byte)startAddress;
                message[4] = (byte)(numElement >> 8);
                message[5] = (byte)numElement;
                message[6] = (byte)byteCount;
                int ipos = 6;
                for(int i = 0; i < DataSet.Length; i++ )
                {
                    ipos = ipos + 1;
                    message[ipos] = (byte)(DataSet[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)DataSet[i];
                    

                }               

                GetCRC(message, ref CRC);
                message[message.Length - 2] = CRC[0];
                message[message.Length - 1] = CRC[1];
            }
        }


        static public void ReportSlaveID(int SlaveAddress, out byte[] message)
        {
            BuildMessage(SlaveAddress, (int)Function.ReportSlaveID, out message);
        }

        static public void ReadGeneralReference(int SlaveAddress, int byteCount, int numberOfGroup, int[] ReferenceType, int[] FileNumber, int[] StartAddress, int[] RegisterCount, out byte[] message)
        {
            message = new byte[(numberOfGroup * 7) + 5];

            if (ReferenceType.Length != numberOfGroup || FileNumber.Length != numberOfGroup || StartAddress.Length != numberOfGroup || RegisterCount.Length != numberOfGroup )
            {
                //MessageBoxResult result = MessageBox.Show("Error!! Quantità di dati passati non corretta", "Error");
            }
            else
            {
                byte[] CRC = new byte[2];

                message[0] = (byte)SlaveAddress;
                message[1] = (byte)Function.ReadGeneralReference;
                message[2] = (byte)byteCount;
                int ipos = 2;
                for (int i = 0;  i < numberOfGroup; i++)
                {
                    ipos = ipos + 1 ;
                    message[ipos] = (byte)ReferenceType[i];
                    ipos = ipos + 1;
                    message[ipos] = (byte)(FileNumber[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)FileNumber[i];
                    ipos = ipos + 1;
                    message[ipos] = (byte)(StartAddress[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)StartAddress[i];
                    ipos = ipos + 1;
                    message[ipos] = (byte)(RegisterCount[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)RegisterCount[i];
                }                 

                GetCRC(message, ref CRC);
                message[message.Length - 2] = CRC[0];
                message[message.Length - 1] = CRC[1];
            }

        }

        static public void WriteGeneralReference(int SlaveAddress, int byteCount, int numberOfGroup, int[] ReferenceType, int[] FileNumber, int[] StartAddress, int[] RegisterCount, int[,] RegisterData, out byte[] message)
        {
            int TotalElementSubReq = (numberOfGroup * 7) + 5;
            for (int i = 0; i <numberOfGroup; i++)
            {
                TotalElementSubReq = TotalElementSubReq + RegisterCount[i] * 2;
            }
            //TotalElementSubReq = (numberOfGroup * 7) + 5;
            message = new byte[TotalElementSubReq];

            if (ReferenceType.Length != numberOfGroup || FileNumber.Length != numberOfGroup || StartAddress.Length != numberOfGroup || RegisterCount.Length != numberOfGroup)
            {
                //MessageBoxResult result = MessageBox.Show("Error!! Quantità di dati passati non corretta", "Error");
            }
            else
            {
                byte[] CRC = new byte[2];

                message[0] = (byte)SlaveAddress;
                message[1] = (byte)Function.WriteGeneralReference;
                message[2] = (byte)byteCount;
                int ipos = 2;
                for (int i = 0; i < numberOfGroup; i++)
                {
                    ipos = ipos + 1;
                    message[ipos] = (byte)ReferenceType[i];
                    ipos = ipos + 1;
                    message[ipos] = (byte)(FileNumber[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)FileNumber[i];
                    ipos = ipos + 1;
                    message[ipos] = (byte)(StartAddress[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)StartAddress[i];
                    ipos = ipos + 1;
                    message[ipos] = (byte)(RegisterCount[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)RegisterCount[i];
                                        
                    for(int j = 0; j < RegisterData.GetLength(1); j++ )
                    {
                        ipos = ipos + 1;
                        message[ipos] = (byte)(RegisterData[i,j] >> 8);
                        ipos = ipos + 1;
                        message[ipos] = (byte)RegisterData[i,j];
                    }
                }

                GetCRC(message, ref CRC);
                message[message.Length - 2] = CRC[0];
                message[message.Length - 1] = CRC[1];
            }

        }

        static public void MaskWrite4XRegister(int SlaveAddress, int ReferenceAddress, int AndMask, int OrMask, out byte[] message)
        {

            byte[] CRC = new byte[2];

            message = new byte[10];

            message[0] = (byte)SlaveAddress;
            message[1] = (byte)Function.MaskWrite4XRegister;
            message[2] = (byte)(ReferenceAddress >> 8);
            message[3] = (byte)ReferenceAddress;
            message[4] = (byte)(AndMask >> 8);
            message[5] = (byte)AndMask;
            message[6] = (byte)(OrMask >> 8);
            message[7] = (byte)OrMask;

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
            }

        static public void ReadWrite4XRegister(int SlaveAddress, int ReadReferenceAddress, int QuantityToRead, int WriteReferenceAddress, int QuantityToWrite, int ByteCount, int[] WriteData, out byte[] message)
        {
            //int TotalElementSubReq = QuantityToWrite * 2 + 13;

            message = new byte[QuantityToWrite * 2 + 13];     
           
            if (WriteData.Length != QuantityToWrite)
            {
                //MessageBoxResult result = MessageBox.Show("Error!! Quantità di dati passati non corretta", "Error");
            }
            else
            {
                byte[] CRC = new byte[2];

                message[0] = (byte)SlaveAddress;
                message[1] = (byte)Function.ReadWrite4XRegister;
                message[2] = (byte)(ReadReferenceAddress >> 8);
                message[3] = (byte)ReadReferenceAddress;
                message[4] = (byte)(QuantityToRead >> 8);
                message[5] = (byte)QuantityToRead;
                message[6] = (byte)(WriteReferenceAddress >> 8);
                message[7] = (byte)WriteReferenceAddress;
                message[8] = (byte)(QuantityToWrite >> 8);
                message[9] = (byte)QuantityToWrite;
                message[10] = (byte)ByteCount;
                int ipos = 10;
                for(int i = 0; i < QuantityToWrite; i++ )
                {
                    ipos = ipos + 1;
                    message[ipos] = (byte)(WriteData[i] >> 8);
                    ipos = ipos + 1;
                    message[ipos] = (byte)WriteData[i];
                    
                }              

                GetCRC(message, ref CRC);
                message[message.Length - 2] = CRC[0];
                message[message.Length - 1] = CRC[1];
            }

        }

        static public void ReadFIFOQueue(int SlaveAddress, int FIFOPointerAddress, out byte[] message)
        {
            byte[] CRC = new byte[2];
            message = new byte[6];
            message[0] = (byte)SlaveAddress;
            message[1] = (byte)Function.ReadFIFOQueue;
            message[2] = (byte)(FIFOPointerAddress >> 8);
            message[3] = (byte)FIFOPointerAddress;               

            GetCRC(message, ref CRC);
            message[message.Length - 2] = CRC[0];
            message[message.Length - 1] = CRC[1];
        }
    }
}
