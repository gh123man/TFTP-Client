using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Collections;

///
/// Brian Floersch (bpf4935@rit.edu)
///
/// This class connects to a TFTP server and transfers files.
namespace TFTP {

    /// <summary>
    /// Class for connecting and downloading files from a tftp server.
    /// </summary>
    class TftpClient {

        // Response codes
        private static readonly byte[] REQUEST = new byte[] { 0x0, 0x01 };
        private static readonly byte[] DATA = new byte[] { 0x0, 0x03 };
        private static readonly byte[] ACK = new byte[] { 0x0, 0x04 };
        private static readonly byte[] ERROR = new byte[] { 0x0, 0x05 };
        private static readonly byte[] ASCII_ZERO = new byte[] { 0x0 };

        private static readonly int SECONDS_30 = 30000;

        /// <summary>
        /// Delegate for error handling
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public delegate void ErrorHandler(int code, String message);

        private IPAddress mServerAddress;
        private UdpClient mUdpClient;
        private IPEndPoint mEndpoint;
        private FileStream mFileStream;
        private BinaryWriter mBinWriter;
        private ErrorHandler mErrorHandler;
        private Timer mTimer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handler"></param>
        public TftpClient(ErrorHandler handler) {
            mUdpClient = new UdpClient();
            mErrorHandler = handler;
        }

        /// <summary>
        /// Begins the file download process.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="file"></param>
        /// <param name="transferMode"></param>
        /// <param name="fileOut"></param>
        public void requestFile(String host, int port, String file, string transferMode, FileStream fileOut) {
            mFileStream = fileOut;
            mBinWriter = new BinaryWriter(mFileStream);
            try {
                mServerAddress = System.Net.Dns.GetHostAddresses(host)[0];
            } catch (SocketException e) {
                mErrorHandler(-1, "Host not found!");
            }
            mEndpoint = new IPEndPoint(mServerAddress, port);
            tftpSend(formatRequest(file, transferMode));
            recieveResponse();
        }

        /// <summary>
        /// waits and handles the inital server response until the file is downloaded.
        /// </summary>
        private void recieveResponse() {
            Boolean done = false;
            int block = 0;

            while (!done) {
                var newEndpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = mUdpClient.Receive(ref newEndpoint);
                mEndpoint = newEndpoint;

                var type = getResponseType(response);

                if (type.SequenceEqual(DATA)) {

                    int currentBlock = getblockNumber(response);

                    if (currentBlock == (block + 1)) {

                        if (mTimer != null) {
                            mTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            mTimer.Dispose();
                        }

                        block = currentBlock;
                        var data = getDataBlock(response);
                        mBinWriter.Write(data);

                        if (data.Length < 512) {
                            done = true;
                        }

                        tftpSend(formatACK(getDatablockNumberAsByte(response)));

                        setAckTimer(response);
                    }

                } else if (type.SequenceEqual(ERROR)) {
                    mBinWriter.Flush();
                    mFileStream.Close();
                    mFileStream.Dispose();
                    mErrorHandler(getblockNumber(response), Encoding.UTF8.GetString(getDataBlock(response)));
                }
            }

            mBinWriter.Flush();
            mFileStream.Close();
        }

        /// <summary>
        /// Sets the ACK command timer on a new thread
        /// </summary>
        /// <param name="response"></param>
        public void setAckTimer(object response) {
            mTimer = new Timer(new TimerCallback(resendAck), response, SECONDS_30, 0);
        }

        /// <summary>
        /// used as a callback to re-send an ack.
        /// </summary>
        /// <param name="response"></param>
        public void resendAck(Object response) {
            tftpSend(formatACK(getDatablockNumberAsByte((byte[])response)));
            setAckTimer(response);
        }

        /// <summary>
        /// sends a TFTP UDP message
        /// </summary>
        /// <param name="msg"></param>
        private void tftpSend(byte[] msg) {
            mUdpClient.Send(msg, msg.Length, mEndpoint);
        }

        /// <summary>
        /// gets the response opcode
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] getResponseType(byte[] data) {
            return new byte[] { data[0], data[1] };
        }

        /// <summary>
        /// gets the block number
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static int getblockNumber(byte[] data) {
            return data[2] * 256 + data[3];
        }

        /// <summary>
        /// gets the block number as bytes
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] getDatablockNumberAsByte(byte[] data) {
            return new byte[] { data[2], data[3] };
        }

        /// <summary>
        /// gets the block data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] getDataBlock(byte[] data) {
            return data.Skip(4).Take(data.Length - 4).ToArray();
        }

        /// <summary>
        /// formats an ACK response
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private static byte[] formatACK(byte[] block) {
            return concatByteArrays(ACK, block);
        }

        /// <summary>
        /// formats a request
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="transferMode"></param>
        /// <returns></returns>
        private static byte[] formatRequest(String fileName, String transferMode) {
            return concatByteArrays(REQUEST, Encoding.ASCII.GetBytes(fileName), ASCII_ZERO, Encoding.ASCII.GetBytes(transferMode), ASCII_ZERO);
        }

        /// <summary>
        /// Concats byte arrays into one byte array.
        /// </summary>
        /// <param name="inByteArrays"></param>
        /// <returns></returns>
        private static byte[] concatByteArrays(params byte[][] inByteArrays) {
            byte[] finalByteArray = new byte[0];
            foreach (byte[] arr in inByteArrays) {
                byte[] tmpByteArray = new byte[finalByteArray.Length + arr.Length];
                Buffer.BlockCopy(finalByteArray, 0, tmpByteArray, 0, finalByteArray.Length);
                Buffer.BlockCopy(arr, 0, tmpByteArray, finalByteArray.Length, arr.Length);
                finalByteArray = tmpByteArray;
            }
            return finalByteArray;
        }

    }
}
