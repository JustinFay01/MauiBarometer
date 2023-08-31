using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Barometer.Services {
    class SerialPortController {
        //Serial Port Fields
        SerialPort _serialPort;

        //Fields for Serial Port Connection
        String PortName;
        int BaudRate;
        Parity Parity;
        int DataBits;
        StopBits StopBits;
        Handshake Handshake;

        //Field to change whether we are writing with or without a new line
        bool newLine;

        public SerialPortController(String portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake = Handshake.None) {

            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            Parity = parity;
            Handshake = handshake;

            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.Handshake = handshake;
            _serialPort.ReadTimeout = 500;//Milli
            _serialPort.WriteTimeout = 500;//Milli

            newLine = true;
        }

        public async Task<string> ReadAndWrite(string line, int DelayInSeconds) {

            if(newLine)
                _serialPort.WriteLine(line);
            else
                _serialPort.Write(line);

            await Task.Delay(DelayInSeconds * 1000);
            return _serialPort.ReadLine();
            //return _out is null ? throw new Exception("Read value is null") : _out;
        }

        public string ReadLine() {
            return _serialPort.ReadLine();
        }

        public void WriteLine(string line) {
            _serialPort.WriteLine(line);
        }

        public void Write(string line) {
            _serialPort.Write(line);
        }

        public void OpenPort() {
            _serialPort.Open();
        }

        public void ClosePort() {
            _serialPort.Close();
        }

        public bool GetStatus() {
            return _serialPort.IsOpen;
        }

        public bool GetNewLine() {
            return newLine;
        }
        public void SetNewLine(bool val) {
            newLine = val;
        }

    }
}
