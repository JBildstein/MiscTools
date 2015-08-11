using System;
using System.IO;
using System.Drawing;

namespace MiscTools
{
    /// <summary>
    /// Provides methods to get a thumbnail from a CR2 file.
    /// <para>Only parts of the file header are read, so it's memory efficient.</para>
    /// </summary>
    public static class CR2ThumbReader
    {
        /// <summary>
        /// Reads the thumbnail from a CR2 file
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <returns>A Bitmap that represents the thumbnail</returns>
        public static Bitmap GetThumb(string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return GetThumb(stream);
            }
        }

        /// <summary>
        /// Reads the thumbnail from a CR2 stream
        /// </summary>
        /// <param name="stream">The stream of the CR2 data</param>
        /// <returns>A Bitmap that represents the thumbnail</returns>
        public static Bitmap GetThumb(Stream stream)
        {
            MemoryStream mstream = new MemoryStream(GetThumbData(stream));
            return new Bitmap(mstream);
        }

        /// <summary>
        /// Reads the thumbnail data from a CR2 file
        /// </summary>
        /// <param name="file">Path to the file</param>
        /// <returns>A byte array with the jpeg data</returns>
        public static byte[] GetThumbData(string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return GetThumbData(stream);
            }
        }

        /// <summary>
        /// Reads the thumbnail data from a CR2 stream
        /// </summary>
        /// <param name="stream">The stream of the CR2 data</param>
        /// <returns>A byte array with the jpeg data</returns>
        public static byte[] GetThumbData(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                //Data endianness
                ushort order = BitConverter.ToUInt16(reader.ReadBytes(2), 0);
                bool IsLittleEndian;
                if (order == 0x4949) IsLittleEndian = true;
                else if (order == 0x4d4d) IsLittleEndian = false;
                else throw new FormatException("Invalid byte order marker");
                bool doReverse = BitConverter.IsLittleEndian != IsLittleEndian;

                //Magic words and first offset
                if (ReadUInt16(reader.ReadBytes(2), doReverse) != 0x002a)                   //tiff magic word
                    throw new FormatException("Invalid magic word for Tiff");
                uint offset0 = ReadUInt32(reader.ReadBytes(4), doReverse);                  //offset to first IFD (should be 16)
                if (ReadUInt16(reader.ReadBytes(2), doReverse) != 0x5243)                   //CR2 magic word
                    throw new FormatException("Invalid magic word for CR2");


                //IFD #0 small jpg, Exif, Makernotes
                stream.Position = offset0;                                                  //set stream to offset
                ushort IFD0_entrynr = ReadUInt16(reader.ReadBytes(2), doReverse);           //number of entries
                stream.Position += 12 * IFD0_entrynr;                                       //jump over all entries (+ unnecessary data)
                uint IFD0_offset = ReadUInt32(reader.ReadBytes(4), doReverse);              //offset to next UFD


                //IFD #1 thumb (160x120)
                stream.Position = IFD0_offset + 10;                                         //set stream to offset (+ jump over unnecessary data)
                uint thumbPos = ReadUInt32(reader.ReadBytes(4), doReverse);                 //offset to thumb data
                stream.Position += 8;                                                       //jump over unnecessary data
                uint dataLength = ReadUInt32(reader.ReadBytes(4), doReverse);               //thumb length in bytes

                //Thumbnail:
                stream.Position = thumbPos;                                                 //set stream to thumb position
                return reader.ReadBytes((int)dataLength);
            }
        }

        private static uint ReadUInt32(byte[] data, bool reverse)
        {
            unchecked
            {
                if (reverse) return (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
                else return (uint)(data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));
            }
        }

        private static ushort ReadUInt16(byte[] data, bool reverse)
        {
            unchecked
            {
                if (reverse) return (ushort)((data[0] << 8) | data[1]);
                else return (ushort)(data[0] | (data[1] << 8));
            }
        }
    }
}
