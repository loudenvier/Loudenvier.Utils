using System;
using System.Text;

namespace Loudenvier.Utils
{
    /// <summary>
    /// Specific CRC16 industry standard methods
    /// </summary>
    public enum Crc16Method {
        /// <summary>
        /// Defaults to <see cref="Crc16Method.Modbus"/>
        /// </summary>
        Default, 
        Modbus, 
        CCITTxFFFF, 
        Kermit }

    /// <summary>
    /// Helper class to calculate CRC16 checksums using various industry standard methods (ModBus, CCITT, Kermit)
    /// </summary>
    public static class Crc16
    {
        /// <summary>
        /// Calculates the CRC-16 from the bytes in <paramref name="buffer"/> up to the specified <paramref name="length"/>
        /// with a specific industry standard <paramref name="method"/>.
        /// </summary>
        /// <param name="method">The CRC-16 industry standard to employ for calculation</param>
        /// <param name="buffer">An array of 8-bit unsigned integers</param>
        /// <param name="length">The length of data in the <paramref name="buffer"/> to include in the calculation</param>
        /// <returns>The computed CRC-16</returns>
        public static ushort Calculate(Crc16Method method, byte[] buffer, int length) => method switch {
            Crc16Method.Modbus => CalculateModbus(buffer, length),
            Crc16Method.CCITTxFFFF => CalculateCCITT(buffer, length),
            Crc16Method.Kermit => CalculateKermit(buffer, length),
            _ => CalculateModbus(buffer, length),
        };

        static readonly Lazy<Encoding> DefaultEncoding = new(() => Encoding.ASCII);

        /// <summary>
        /// Calculates the CRC-16 of the provided <paramref name="text"/>, up to the specified <paramref name="length"/>  
        /// by using the specified <paramref name="encoding"/> to convert from <see cref="char"/> to <see cref="byte"/>,
        /// employing a specific CRC-16 industry standard <paramref name="method"/>.
        /// </summary>  
        /// <remarks>The <paramref name="length"/> is relative to the characters in the <paramref name="text"/> string
        /// and do not take into account the encoding.</remarks>
        /// <param name="method">The CRC-16 industry standard to employ for calculation</param>
        /// <param name="text">The text to use for calculation</param>
        /// <param name="length">The length of data in the <paramref name="text"/> to include in the calculation</param>
        /// <param name="encoding">The character encoding to use to convert the string into bytes</param>
        /// <returns>The computed CRC-16</returns>
        public static ushort Calculate(Crc16Method method, string text, int length, Encoding? encoding = null) {
            if (string.IsNullOrEmpty(text) || length == 0)
                return 0;
            encoding ??= DefaultEncoding.Value;
            var buffer = encoding.GetBytes(length < text.Length ? text[..length] : text);
            return Calculate(method, buffer, buffer.Length);
        }

        /// <summary>
        /// Calculates the MODBUS CRC-16 of the provided <paramref name="text"/>, up to the specified <paramref name="length"/>  
        /// by using the specified <paramref name="encoding"/> to convert from <see cref="char"/> to <see cref="byte"/>.
        /// </summary>  
        /// <remarks>The <paramref name="length"/> is relative to the characters in the <paramref name="text"/> string
        /// and do not take into account the encoding.</remarks>
        /// <param name="text">The text to use for calculation</param>
        /// <param name="length">The length of data in the <paramref name="text"/> to include in the calculation</param>
        /// <param name="encoding">The character encoding to use to convert the string into bytes</param>
        /// <returns>The computed MODBUS CRC-16</returns>
        public static ushort CalculateModbus(string text, int length, Encoding? encoding = null)
            => Calculate(Crc16Method.Modbus, text, length, encoding);
        /// <summary>
        /// Calculates the CCITTxFFFF CRC-16 of the provided <paramref name="text"/>, up to the specified <paramref name="length"/>  
        /// by using the specified <paramref name="encoding"/> to convert from <see cref="char"/> to <see cref="byte"/>.
        /// </summary>  
        /// <remarks>The <paramref name="length"/> is relative to the characters in the <paramref name="text"/> string
        /// and do not take into account the encoding.</remarks>
        /// <param name="text">The text to use for calculation</param>
        /// <param name="length">The length of data in the <paramref name="text"/> to include in the calculation</param>
        /// <param name="encoding">The character encoding to use to convert the string into bytes</param>
        /// <returns>The computed CCITTxFFFF CRC-16</returns>
        public static ushort CalculateCCITTxFFFF(string text, int length, Encoding? encoding = null)
            => Calculate(Crc16Method.CCITTxFFFF, text, length, encoding);
        /// <summary>
        /// Calculates the KERMIT/CCITT CRC-16 of the provided <paramref name="text"/>, up to the specified <paramref name="length"/>  
        /// by using the specified <paramref name="encoding"/> to convert from <see cref="char"/> to <see cref="byte"/>.
        /// </summary>  
        /// <remarks>The <paramref name="length"/> is relative to the characters in the <paramref name="text"/> string
        /// and do not take into account the encoding.</remarks>
        /// <param name="text">The text to use for calculation</param>
        /// <param name="length">The length of data in the <paramref name="text"/> to include in the calculation</param>
        /// <param name="encoding">The character encoding to use to convert the string into bytes</param>
        /// <returns>The computed KERMIT/CCITT CRC-16</returns>
        public static ushort CalculateKermit(string text, int length, Encoding? encoding = null)
            => Calculate(Crc16Method.Kermit, text, length, encoding);


        /// <summary>
        /// Calculates the MODBUS CRC-16 of the array of <paramref name="bytes"/> up to the specified <paramref name="length"/>
        /// </summary>
        /// <param name="bytes">An array of 8-bit unsigned integers</param>
        /// <param name="length">The length of data in the array of <paramref name="bytes"/> to include in the calculation</param>
        /// <returns>The computed MODBUS CRC-16</returns>
        public static ushort CalculateModbus(byte[] bytes, int length) {
            ushort Crc = 65535;
            ushort x;
            for (ushort i = 0; i < length; i++) {
                x = (ushort)(Crc ^ bytes[i]);
                Crc = (ushort)((Crc >> 8) ^ crc_table_modbus[x & 0x00FF]);
            }
            return Crc;
        }

        /// <summary>
        /// Calculates the CCITT CRC-16 of the array of <paramref name="bytes"/> up to the specified <paramref name="length"/>
        /// </summary>
        /// <param name="bytes">An array of 8-bit unsigned integers</param>
        /// <param name="length">The length of data in the array of <paramref name="bytes"/> to include in the calculation</param>
        /// <returns>The computed CCITT CRC-16</returns>
        public static ushort CalculateCCITT(byte[] buffer, int length) {
            var table = tableCCITT.Value;
            ushort crc = 0xffff;
            for (int i = 0; i < length; ++i) {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & buffer[i]))]);
            }
            return crc;
        }

        /// <summary>
        /// Calculates the KERMIT CRC-16 of the array of <paramref name="bytes"/> up to the specified <paramref name="length"/>
        /// </summary>
        /// <param name="bytes">An array of 8-bit unsigned integers</param>
        /// <param name="length">The length of data in the array of <paramref name="bytes"/> to include in the calculation</param>
        /// <returns>The computed KERMIT CRC-16</returns>
        public static ushort CalculateKermit(byte[] bytes, int length) {
            var table = tableKermit.Value;
            ushort crc = 0;
            for (int i = 0; i < length; ++i) {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            return crc;
        }

        private static readonly ushort[] crc_table_modbus = [
0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,
0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,
0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,
0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,
0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,
0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,
0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,
0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,
0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,
0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,
0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,
0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,
0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040];

        static readonly Lazy<ushort[]> tableCCITT = new(GetTableCCITT);

        static ushort[] GetTableCCITT() {
            const ushort poly = 4129;
            ushort value, temp;
            ushort[] table = new ushort[256];
            for (int i = 0; i < table.Length; ++i) {
                value = 0;
                temp = (ushort)(i << 8);
                for (int j = 0; j < 8; ++j) {
                    if (((value ^ temp) & 0x8000) != 0) {
                        value = (ushort)((value << 1) ^ poly);
                    } else {
                        value <<= 1;
                    }
                    temp <<= 1;
                }
                table[i] = value;
            }
            return table;
        }

        static readonly Lazy<ushort[]> tableKermit = new(GetTableKermit);

        static ushort[] GetTableKermit() {
            ushort polynomial = 0x8408;
            ushort value;
            ushort temp;
            ushort[] table = new ushort[256];
            for (ushort i = 0; i < table.Length; ++i) {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j) {
                    if (((value ^ temp) & 0x0001) != 0) {
                        value = (ushort)((value >> 1) ^ polynomial);
                    } else {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
            return table;
        }
    }
}
