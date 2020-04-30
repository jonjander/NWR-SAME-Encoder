using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SAMEEncoder
{

    public class tools
    {
        public static byte[] ConvertToByteArray(string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }
        public static List<string> ToBinary(Byte[] data)
        {
            return data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')).ToList();
        }

        public static List<string> ToBinaryList(List<string> byteList)
        {
            return byteList.SelectMany(s => s.ToCharArray().ToList())
                .Select(s=>s.ToString()).ToList();
        }

        public static List<string> CreateDataByte(string byteString)
        {
            var zeroOnes = byteString.ToCharArray().ToList();
            zeroOnes[0] = '0';
            zeroOnes.Reverse();
            var dataOut = zeroOnes;
            return dataOut.Select(s => s.ToString()).ToList();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Message >");
            var Data = Console.ReadLine();
            var PreambleCode = "11010101";
            var PreambleStartBody = new List<string>();
            for (int i = 0; i < 16; i++)
            {
                PreambleStartBody.Add(PreambleCode);
            }
            var PrembelDataBits = tools.ToBinaryList(PreambleStartBody);
            var dataBits = PrembelDataBits;
            var byteArrayMessage = tools.ConvertToByteArray(Data, Encoding.ASCII);
            var binary = tools.ToBinary(byteArrayMessage);

            var x = 0;
            foreach (var item in binary)
            {
                if (x == 254)
                {
                    x = 0;
                    dataBits.AddRange(PrembelDataBits);
                } else
                {
                    x++;
                }
                dataBits.AddRange(tools.CreateDataByte(item));
            }
            var wGen = new WaveGenerator();
            wGen.addData(WaveExampleType.broadcastCombined);
            wGen.addData(WaveExampleType.zero);
            for (int i = 0; i < 3; i++)
            {
                foreach (var item in dataBits)
                {
                    if (item == "0") 
                    {
                        //Space
                        wGen.addData(WaveExampleType.Low);
                    }
                    else 
                    {
                        //Mark
                        wGen.addData(WaveExampleType.High);
                    }
                }
                wGen.addData(WaveExampleType.zero);
            }
            wGen.addData(WaveExampleType.broadcastCombined);
            wGen.addData(WaveExampleType.zero);
            wGen.addData(WaveExampleType.zero);
            wGen.Save(".\\Message.wav");
        }
    }


    public class WaveHeader
    {
        public string sGroupID; // RIFF
        public uint dwFileLength; // total file length minus 8, which is taken up by RIFF
        public string sRiffType; // always WAVE

        /// <summary>
        /// Initializes a WaveHeader object with the default values.
        /// </summary>
        public WaveHeader()
        {
            dwFileLength = 0;
            sGroupID = "RIFF";
            sRiffType = "WAVE";
        }
    }

    public class WaveFormatChunk
    {
        public string sChunkID;         // Four bytes: "fmt "
        public uint dwChunkSize;        // Length of header in bytes
        public ushort wFormatTag;       // 1 (MS PCM)
        public ushort wChannels;        // Number of channels
        public uint dwSamplesPerSec;    // Frequency of the audio in Hz... 44100
        public uint dwAvgBytesPerSec;   // for estimating RAM allocation
        public ushort wBlockAlign;      // sample frame size, in bytes
        public ushort wBitsPerSample;    // bits per sample

        /// <summary>
        /// Initializes a format chunk with the following properties:
        /// Sample rate: 44100 Hz
        /// Channels: Stereo
        /// Bit depth: 16-bit
        /// </summary>
        public WaveFormatChunk()
        {
            sChunkID = "fmt ";
            dwChunkSize = 16;
            wFormatTag = 1;
            wChannels = 2;
            dwSamplesPerSec = 44100;
            wBitsPerSample = 16;
            wBlockAlign = (ushort)(wChannels * (wBitsPerSample / 8));
            dwAvgBytesPerSec = dwSamplesPerSec * wBlockAlign;
        }
    }

    public class WaveDataChunk
    {
        public string sChunkID;     // "data"
        public uint dwChunkSize;    // Length of header in bytes
        public List<short> shortArray;  // 8-bit audio

        /// <summary>
        /// Initializes a new data chunk with default values.
        /// </summary>
        public WaveDataChunk()
        {
            shortArray = new List<short>();
            dwChunkSize = 0;
            sChunkID = "data";
        }
    }

    public enum WaveExampleType
    {
        ExampleSineWave = 0,
        High = 1,
        Low = 2,
        zero = 3,
        broadcastCombined = 4
    }

    public class WaveGenerator
    {
        // Header, Format, Data chunks
        WaveHeader header;
        WaveFormatChunk format;
        WaveDataChunk data;
        uint offset = 0;

        /// <snip>
        /// 
        public WaveGenerator()
        {
            // Init chunks
            header = new WaveHeader();
            format = new WaveFormatChunk();
            data = new WaveDataChunk();
        }

        public void addData(WaveExampleType type)
        {
            // Fill the data array with sample data

            uint numSamples;
            int amplitude;
            double freq, freq1, freq2;
            double t, t1, t2;
            double fCorr = -30f;
            switch (type)
            {
                case WaveExampleType.Low:
                    numSamples = format.dwSamplesPerSec * format.wChannels / (int)(520.8333 * 2);
                    amplitude = 32760;  // Max amplitude for 16-bit audio
                    freq = (1562.5f) * 2; 
                    freq -= fCorr;
                    t = (Math.PI * 2 * freq) / (format.dwSamplesPerSec * format.wChannels);
                    for (uint i = offset; i < numSamples - 1 + offset; i++)
                    {
                        for (int channel = 0; channel < format.wChannels; channel++)
                        {
                            data.shortArray.Add(Convert.ToInt16(amplitude * Math.Sin(t * i)));
                        }
                    }
                    data.dwChunkSize = (uint)(data.shortArray.Count * (format.wBitsPerSample / 8));
                    break;
                case WaveExampleType.High:
                    numSamples = format.dwSamplesPerSec * format.wChannels / (int)(520.8333 * 2);
                    amplitude = 32760;  // Max amplitude for 16-bit audio
                    freq = (2083f + (1 / 3)) * 2;
                    freq -= fCorr;
                    t = (Math.PI * 2 * freq) / (format.dwSamplesPerSec * format.wChannels);
                   
                    for (uint i = offset; i < numSamples - 1 + offset; i++) 
                    {
                        // Fill with a simple sine wave at max amplitude
                        for (int channel = 0; channel < format.wChannels; channel++)
                        {
                            data.shortArray.Add(Convert.ToInt16(amplitude * Math.Sin(t * i)));
                        }
                    }
                    // Calculate data chunk size in bytes
                    data.dwChunkSize = (uint)(data.shortArray.Count * (format.wBitsPerSample / 8));
                    break;
                case WaveExampleType.zero:
                    numSamples = format.dwSamplesPerSec * format.wChannels;
                    for (uint i = offset; i < numSamples - 1 + offset; i++)
                    {
                        // Fill with a simple sine wave at max amplitude
                        for (int channel = 0; channel < format.wChannels; channel++)
                        {
                            data.shortArray.Add(0);
                        }
                    }
                    data.dwChunkSize = (uint)(data.shortArray.Count * (format.wBitsPerSample / 8));
                    break;
                case WaveExampleType.broadcastCombined:
                    numSamples = format.dwSamplesPerSec * format.wChannels;
                    amplitude = 32760 / 2;  // Max amplitude for 16-bit audio

                    freq1 = (853f + (1 / 3)) * 2; 
                    freq1 -= fCorr;

                    freq2 = (960f + (1 / 3)) * 2; 
                    freq2 -= fCorr;

                    t1 = (Math.PI * 2 * freq1) / (format.dwSamplesPerSec * format.wChannels);
                    t2 = (Math.PI * 2 * freq2) / (format.dwSamplesPerSec * format.wChannels);
                    for (uint i = offset; i < numSamples - 1 + offset; i++)
                    {
                        // Fill with a simple sine wave at max amplitude
                        for (int channel = 0; channel < format.wChannels; channel++)
                        {
                            data.shortArray.Add(Convert.ToInt16((amplitude * Math.Sin(t1 * i)) + (amplitude * Math.Sin(t2 * i))));
                        }
                    }
                    // Calculate data chunk size in bytes
                    data.dwChunkSize = (uint)(data.shortArray.Count * (format.wBitsPerSample / 8));
                    
                    break;
            }
        }

        public void Save(string filePath)
        {
            // Create a file (it always overwrites)
            FileStream fileStream = new FileStream(filePath, FileMode.Create);

            // Use BinaryWriter to write the bytes to the file
            BinaryWriter writer = new BinaryWriter(fileStream);
   
            // Write the header
            writer.Write(header.sGroupID.ToCharArray());
            writer.Write(header.dwFileLength);
            writer.Write(header.sRiffType.ToCharArray());

            // Write the format chunk
            writer.Write(format.sChunkID.ToCharArray());
            writer.Write(format.dwChunkSize);
            writer.Write(format.wFormatTag);
            writer.Write(format.wChannels);
            writer.Write(format.dwSamplesPerSec);
            writer.Write(format.dwAvgBytesPerSec);
            writer.Write(format.wBlockAlign);
            writer.Write(format.wBitsPerSample);

            // Write the data chunk
            writer.Write(data.sChunkID.ToCharArray());
            writer.Write(data.dwChunkSize);
            foreach (short dataPoint in data.shortArray)
            {
                writer.Write(dataPoint);
            }

            writer.Seek(4, SeekOrigin.Begin);
            uint filesize = (uint)writer.BaseStream.Length;
            writer.Write(filesize - 8);

            // Clean up
            writer.Close();
            fileStream.Close();
        }
    }
    
}
