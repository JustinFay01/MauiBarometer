using System;
using System.IO.Ports;
using System.Threading;

namespace Barometer.Services {
    //Example of Connection with C#
    //https://www.c-sharpcorner.com/uploadfile/eclipsed4utoo/communicating-with-serial-port-in-C-Sharp/
    //Link to docs
    //https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport?view=netframework-4.8
    public class SerialPortController {

        bool _continue;
        SerialPort _serialPort;

        //Fields for Serial Port Connection
        String PortName;
        int BaudRate;
        Parity Parity;
        int DataBits;
        StopBits StopBits;
        Handshake Handshake;

            //Example used for C++
           /* dcbParams.BaudRate = CBR_9600;
            dcbParams.ByteSize = 8;
            dcbParams.StopBits = ONESTOPBIT;
            dcbParams.Parity = NOPARITY;
            dcbParams.fDtrControl = DTR_CONTROL_ENABLE;*/

        public SerialPortController(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake=Handshake.None) {
            
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            Parity = parity;
            Handshake = handshake;

            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.Handshake = handshake;

        }

        public bool OpenConnection() {

            if(_serialPort.IsOpen) {
                return true;
            }

            _serialPort?.Open();
            return _serialPort.IsOpen;
        }

        public bool CloseConnection() {
            if (!_serialPort.IsOpen) {
                return false;
            }
            _serialPort.Close();
            return _serialPort.IsOpen;
        }

        public bool GetStatus() {
            return _serialPort.IsOpen;
        }
    }
}

