﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace letterCompression
{
    class Combo
    {
        public string combo;
        public int occ;
    }
    class Program
    {
        public static int blockSize = 9; //9 bits. Don't change - might break (never tested)

        public static List<char> inputAlpha = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', '.', ',', '\'','"', '!', '?', '\n'}.ToList();

        public static List<Combo> dataset = JsonConvert.DeserializeObject<List<Combo>>(File.ReadAllText("datasets/English.json"));

        static void Main(string[] args)
        {
            //prepare the dataset
            dataset = dataset.Take((int)Math.Pow(2, blockSize) - inputAlpha.Count).ToList();
            Console.WriteLine("Hello. Press 1 to compress and 2 to decompress.");
            Console.WriteLine("===============================================");
            Console.Write(">");
            var z = Console.ReadKey();
            Console.WriteLine();
            if (z.Key == ConsoleKey.D1)
            {
                Console.Write("[Text to compress (a-Z 0-9 .,'\"!?)]=");
                string text = Console.ReadLine().Replace("\r", "").Replace("\n", " ");
                byte[] compressed = Compress(text, dataset);
                Console.WriteLine($"[Result (in base64)]={Convert.ToBase64String(compressed)}");
            } else
            {
                Console.Write("[Text to decompress (in base64)]=");
                string text = Console.ReadLine();
                string decomp = Decompress(Convert.FromBase64String(text), dataset);
                Console.WriteLine($"[Result]={decomp}");
            }
        }

        private static byte[] Compress(string toCompress, List<Combo> dataset)
        {
            List<short> compressed = new List<short>();
            for (int i = 0; i<toCompress.Length; i++)
            {
                string combo3 = "";
                string combo2 = "";
                if (i + 1 < toCompress.Length)
                {
                    combo2 = toCompress[i].ToString() + toCompress[i + 1].ToString();
                    if (i + 2 < toCompress.Length)
                    {
                        combo3 = toCompress[i].ToString() + toCompress[i + 1].ToString() + toCompress[i + 2].ToString();
                    }
                }
                string combo1 = toCompress[i].ToString();

                short c = (short)dataset.FindIndex(z => z.combo == combo3);
                if (c != -1) { compressed.Add((short)(c + inputAlpha.Count)); i += 2; continue; }
                c = (short)dataset.FindIndex(z => z.combo == combo2);
                if (c != -1) { compressed.Add((short)(c + inputAlpha.Count)); i += 1; continue; }

                c = (short)inputAlpha.FindIndex(z => z == combo1[0]);
                if (c != -1) { compressed.Add(c); } else { throw new Exception($"Unexpected character at {i}"); }
            }

            byte[] final = new byte[(int) Math.Ceiling((double)compressed.Count * blockSize / 8)];
            int bitPointer = 0;
            int bytePointer = 0;
            for (int i = 0; i < compressed.Count; i++)
            {
                short res = compressed[i];
                final[bytePointer] |= (byte) (res >> (blockSize-8+bitPointer));
                bytePointer++;
                final[bytePointer] = (byte) ((res & (short)(((1 << bitPointer+1)-1))) << (16-blockSize-bitPointer));
                bitPointer += blockSize - 8;
                if (bitPointer >= 8)
                {
                    bitPointer = 0;
                    bytePointer++;
                }
            }
            return final; 
        }

        public static string Decompress(byte[] toDecomp, List<Combo> dataset)
        {
            int bitPointer = 0;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < toDecomp.Length-1; i++)
            {
                short block = (short) ((toDecomp[i] << (bitPointer+(blockSize-8))) & ((short) Math.Pow(2, blockSize)-1));
                block |= (short)((toDecomp[i+1] & (byte)(256 - (1 << 7-bitPointer))) >> (7-bitPointer));
                stringBuilder.Append(block < inputAlpha.Count ? inputAlpha[block] : dataset[block - inputAlpha.Count].combo);

                bitPointer += blockSize - 8;
                if (bitPointer >= 8)
                {
                    bitPointer = 0;
                    i++;
                }
            }
            return stringBuilder.ToString();
        }
    }
}
