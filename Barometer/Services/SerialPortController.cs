using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Barometer.Services {
    class SerialPortController {
        //Serial Port Fields
        bool _continue;
        SerialPort _serialPort;

        //Fields for Serial Port Connection
        String PortName;
        int BaudRate;
        Parity Parity;
        int DataBits;
        StopBits StopBits;
        Handshake Handshake;

        public SerialPortController(String portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Handshake handshake = Handshake.None) {

            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            Parity = parity;
            Handshake = handshake;

            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            _serialPort.Handshake = handshake;

        }

        public async Task<string> ReadAndWrite(string line, CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {

                _serialPort.WriteLine(line);

                await Task.Delay(2000);

                return _serialPort.ReadLine();
            }
            return "";
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

        public void SetContinue(bool val) {
            _continue = val;
        }

    }
}
