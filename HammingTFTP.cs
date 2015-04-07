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
    class HammingTFTP {

        private static readonly String USAGE = "Usage: [mono] HammingTFTP.exe [error | noerror] tftp−host file";

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


            bool error = false;
            if (args[0] == "error" || args[0] == "noerror") {
                error = (args[0] == "error" ? true : false);
            } else {
                Console.Out.WriteLine(USAGE);
                System.Environment.Exit(1);
            }
            
            String host = args[1];
            String file = args[2];
            var program = new HammingTFTP(file);

            TftpClient client = new TftpClient(program.log);

            client.requestFile(host, 7000, file, error, File.Create(Directory.GetCurrentDirectory() + '/' + file, 512, FileOptions.None));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="file"></param>
        public HammingTFTP(String file) {
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
            } catch (IOException) { }
            System.Environment.Exit(1);
        }
    }
}
