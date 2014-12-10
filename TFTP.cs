using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

///
/// Brian Floersch (bpf4935@rit.edu)
/// 
/// This class is the main command line interface for the tftp client
namespace TFTP {

    /// <summary>
    /// CMD line app for a TFTP Client
    /// </summary>
    class TFTP {

        private static readonly String USAGE =  "\nUsage : [ mono ] TFTPreader.exe [ netascii | octet ] tftp −hostfile";

        private String mFile;

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args) {

            if (args.Length < 3) {
                Console.Out.WriteLine(USAGE);
                System.Environment.Exit(1);
            }

            String mode = args[0];
            String host = args[1];
            String file = args[2];

            var program = new TFTP(file);

            TftpClient client = new TftpClient(program.log);

            client.requestFile(host, 69, file, mode, File.Create(Directory.GetCurrentDirectory() + '/' + file, 512, FileOptions.None));

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="file"></param>
        public TFTP(String file) {
            mFile = file;
        }

        /// <summary>
        /// handles error logging
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public void log(int code, String message) {
            Console.Out.WriteLine((code == -1 ? "" : ("Error Code " + code + " ")) + message);
            try {
                File.Delete(Directory.GetCurrentDirectory() + '/' + mFile);
            } catch (IOException e) { }
            System.Environment.Exit(1);
        }
    }
}
