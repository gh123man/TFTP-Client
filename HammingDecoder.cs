using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

///
/// Brian Floersch (bpf4935@rit.edu)
/// 
/// This class decodes hamming encoded data
namespace TFTP {
    /// <summary>
    /// Decodes hamming encoded data
    /// </summary>
    class HammingDecoder {

        /// <summary>
        /// Holds leftovers between blocks
        /// </summary>
        private bool[] instanceLeftovers = null;

        /// <summary>
        /// Trys to decode hamming encoded data
        /// </summary>
        /// <param name="data">data to deocde</param>
        /// <returns>true if success, false if bad</returns>
        public bool tryDecode(ref byte[] data) {
            bool[] leftovers = instanceLeftovers;
            var blocks = breakIntoBlocks(data);
            List<byte> finalBytes = new List<byte>();

            foreach (byte[] bits in blocks) {

                byte[] bitsFromBlock = bits;

                if (!tryCorrectHamming(ref bitsFromBlock)) {
                    return false;
                }

                bool[] dataNoHamming = new bool[26];

                BitArray bArray = new BitArray(bitsFromBlock);

                int backCount = 25;

                for (var i = 0; i < bArray.Length; ++i) {
                    if (i != 0 && i != 1 && i != 3 && i != 7 && i != 15 && i != 31) {
                        dataNoHamming[backCount] = bArray.Get(i);
                        backCount--;
                    } 
                }


                if (leftovers != null) {
                    dataNoHamming = concatBoolArrays(leftovers, dataNoHamming);
                    leftovers = null;
                }

                bool[][] reversedBytes = null;
                if (dataNoHamming.Length == 32) {
                    reversedBytes = new bool[][] {
                        dataNoHamming.Skip(0).Take(8).ToArray(),
                        dataNoHamming.Skip(8).Take(8).ToArray(),
                        dataNoHamming.Skip(16).Take(8).ToArray(),
                        dataNoHamming.Skip(24).Take(8).ToArray()
                    };
                } else {
                    reversedBytes = new bool[][] {
                        dataNoHamming.Skip(0).Take(8).ToArray(),
                        dataNoHamming.Skip(8).Take(8).ToArray(),
                        dataNoHamming.Skip(16).Take(8).ToArray()
                    };

                    leftovers = dataNoHamming.Skip(24).Take(dataNoHamming.Length - 24).ToArray();
                }

                foreach (bool[] revByte in reversedBytes) {
                    byte finalByte = boolArrayTobyte(revByte);
                    finalBytes.Add(finalByte);
                }

            }
            instanceLeftovers = leftovers;
            data = finalBytes.ToArray();
            return true;
            
        }

        /// <summary>
        /// breaks data into 32bit blocks
        /// </summary>
        /// <param name="bytes">data</param>
        /// <returns>array of blocks</returns>
        private static List<byte[]> breakIntoBlocks(byte[] bytes) {
            var offset = 0;
            var result = new List<byte[]>();

            do {
                result.Add(bytes.Skip(offset).Take(4).ToArray());
                offset += 4;
            } while (offset != bytes.Length);

            return result;
        }

        /// <summary>
        /// reverses and converts an array of bools into valid bytes
        /// </summary>
        /// <param name="byteAsBools">data</param>
        /// <returns>byte</returns>
        private static byte boolArrayTobyte(bool[] byteAsBools) {
            byte result = 0;
            
            for (int i = 0; i < byteAsBools.Length; i++) {
                if (byteAsBools[i]) {
                    result |= (byte)(1 << i);
                }
            }
            return result;
        }

        /// <summary>
        /// Concatinates two or more bool arrays
        /// </summary>
        /// <param name="inBoolArrays">bool arrays</param>
        /// <returns>one bool array</returns>
        private static bool[] concatBoolArrays(params bool[][] inBoolArrays) {
            bool[] finalBoolArray = new bool[0];
            foreach (bool[] arr in inBoolArrays) {
                bool[] tmpBoolArray = new bool[finalBoolArray.Length + arr.Length];
                Buffer.BlockCopy(finalBoolArray, 0, tmpBoolArray, 0, finalBoolArray.Length);
                Buffer.BlockCopy(arr, 0, tmpBoolArray, finalBoolArray.Length, arr.Length);
                finalBoolArray = tmpBoolArray;
            }
            return finalBoolArray;
        }

        /// <summary>
        /// Trys to correct hamming data
        /// </summary>
        /// <param name="bits">data to correct</param>
        /// <returns>true if success, false if > 2 errors</returns>
        private static bool tryCorrectHamming(ref byte[] bits) {
            
            BitArray bArray = new BitArray(bits);

            int check1 = 0,
                check2 = 0,
                check4 = 0,
                check8 = 0,
                check16 = 0,
                check32 = 0;

            //Take one skip one
            for (var i = 0; i < bArray.Length; i++) {
                if (i % 2 == 0) {
                    if (bArray[i]) {
                        check1++;
                    }
                }
            }

            //Take two skip two
            bool take = false;
            int count = 0;
            for (var i = 1; i < bArray.Length; i++) {
                if (count % 2 == 0) {
                    take = !take;
                }
                if (take) {
                    if (bArray[i]) {
                        check2++;
                    }
                }
                count++;
            }

            //Take 4 skip 4
            take = false;
            count = 0;
            for (var i = 3; i < bArray.Length; i++) {
                if (count % 4 == 0) {
                    take = !take;
                }
                if (take) {
                    if (bArray[i]) {
                        check4++;
                    }
                }
                count++;
            }

            //Take 8 skip 8
            take = false;
            count = 0;
            for (var i = 7; i < bArray.Length; i++) {
                if (count % 8 == 0) {
                    take = !take;
                }
                if (take) {
                    if (bArray[i]) {
                        check8++;
                    }
                }
                count++;
            }

            //Take 16 skip 16
            take = false;
            count = 0;
            for (var i = 15; i < bArray.Length; i++) {
                if (count % 16 == 0) {
                    take = !take;
                }
                if (take) {
                    if (bArray[i]) {
                        check16++;
                    }
                }
                count++;
            }

            //Check
            int bitToCorrect = 0;
            if (check1 % 2 != 0) {
                bitToCorrect += 1;
            }
            if (check2 % 2 != 0) {
                bitToCorrect += 2;
            }
            if (check4 % 2 != 0) {
                bitToCorrect += 4;
            }
            if (check8 % 2 != 0) {
                bitToCorrect += 8;
            }
            if (check16 % 2 != 0) {
                bitToCorrect += 16;
            }

            //Correct
            if (bitToCorrect != 0) {
                bitToCorrect--;
                bArray.Set(bitToCorrect, !bArray.Get(bitToCorrect));
            }

            //check 32
            for (var i = 0; i < bArray.Length; i++) {
                if (bArray[i]) {
                    check32++;
                }
            }

            //More than two bit errors?
            if (check32 % 2 != 0) {
                return false;
            }

            //Nope
            bArray.CopyTo(bits, 0);

            return true;
        }

    }
}
