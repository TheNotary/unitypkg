﻿using System.Collections;
using System.Security.Cryptography;
using System.IO;
using System;
using System.Linq;

namespace SimpleWebsocketServer
{
    public class WebsocketSerializer
    {
        private WebsocketFrame frame;
        MemoryStream memoryStream = new MemoryStream();

        public WebsocketSerializer(WebsocketFrame frame)
        {
            this.frame = frame;
        }

        public byte[] ToBytes()
        {
            var memoryStream = new MemoryStream();

            // Handle 1st byte: FIN, RSV1-3 (f), and opcode
            byte firstByte = SerializeFirstHeaderByte();
            memoryStream.WriteByte(firstByte);

            // Handle 2nd byte: Mask and payload length
            byte secondByte = SerializeSecondHeaderByte();
            memoryStream.WriteByte(secondByte);

            // Handle extended payload length bytes also if present
            byte[] extendedPayloadLengthBytes = SerializeExtendedPayloadLengthBytes();
            memoryStream.Write(extendedPayloadLengthBytes, 0, extendedPayloadLengthBytes.Length);

            if (frame.isMasked)
            {
                frame.mask = frame.mask == null ? GenerateMaskingKey() : frame.mask;
                memoryStream.Write(frame.mask, 0, frame.mask.Length);
            }

            byte[] payloadData = SerializePayloadData();
            memoryStream.Write(payloadData, 0, payloadData.Length);

            return memoryStream.ToArray();
        }

        internal byte[] GenerateMaskingKey()
        {
            byte[] key = new byte[4];
            RandomNumberGenerator.Create().GetBytes(key);
            return key;
        }

        internal MemoryStream xorMessage()
        {
            MemoryStream xorStream = new MemoryStream();

            for (ulong i = 0; i < (ulong)frame.cleartextPayload.Length; i++)
            {
                byte maskI = (byte)frame.mask[i % 4];
                byte rawByte = (byte)frame.cleartextPayload[i];
                byte decodedByte = (byte)(rawByte ^ maskI);
                xorStream.WriteByte(decodedByte);
            }
            return xorStream;
        }

        internal byte[] SerializePayloadData()
        {
            if (frame.isMasked)
                return xorMessage().ToArray();
            return frame.cleartextPayload;
        }

        internal byte SerializeFirstHeaderByte()
        {
            int header1Left = BitsToBytes(new BitArray(new bool[4] {
               false, false, false, frame.fin })
            )[0];
            header1Left = header1Left << 4;
            int joinedHeader1 = header1Left + frame.opcode;
            byte firstByte = BitConverter.GetBytes(joinedHeader1)[0];
            return firstByte;
        }

        internal byte[] BitsToBytes(BitArray bits)
        {
            byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }

        internal byte SerializeSecondHeaderByte()
        {
            int header2Left = Convert.ToInt32(frame.isMasked) << 7;
            int joinedHeader2 = header2Left + (int) frame.payloadLength;
            return BitConverter.GetBytes(joinedHeader2)[0];
        }

        internal byte[] SerializeExtendedPayloadLengthBytes()
        {
            if (frame.payloadLength < 126)
                return new byte[0];  // no extended payload length for numbers less than 126
            if (frame.payloadLength <= 65535)
                return SubArray(BitConverter.GetBytes(frame.payloadLength), 0, 2).Reverse().ToArray();
            return BitConverter.GetBytes(frame.payloadLength).Reverse().ToArray(); // the next 8 bytes are payload length
        }

        private T[] SubArray<T>(T[] array, int offset, int length)
        {
            T[] result = new T[length];
            Array.Copy(array, offset, result, 0, length);
            return result;
        }

    }
}
