using System;

namespace TwentySquares
{
    public enum Square
    {
        NONE = 0,
        X    = 1,
        O    = 2
    }

    public class Row
    {
        private uint index = 0;
        private Square[] row = new Square[20];

        public Row()
        {
            for (int i = 0; i < 20; ++i)
            {
                row[i] = Square.NONE;
            }
        }

        public Row(Square[] from)
        {
            for (int i = 0; i < 20; ++i)
            {
                row[i] = from[i];
            }
        }

        public Row(byte[] from)
        {
            index = 0;
            row = new Square[20];
            int fromLen = from.Length;

            uint i;
            for (i = 0; i < fromLen && i < 5 && index < 20; ++i)
            {
                row[index++] = (Square)((byte)from[i] >> 6);
                row[index++] = (Square)((byte)(from[i] & 48) >> 4);
                row[index++] = (Square)((byte)(from[i] & 12) >> 2);
                row[index++] = (Square)((byte)from[i] & 3);
            }

            for (i = 0; i < 20; ++i)
            {
                if (row[i] == Square.NONE)
                    break;

                index = i;
            }
        }

        public void SetFromBytes(byte[] from)
        {
            index = 0;
            row = new Square[20];
            int fromLen = from.Length;

            uint i;
            for (i = 0; i < fromLen && i < 5 && index < 20; ++i)
            {
                row[index++] = (Square)((byte)from[i] >> 6);
                row[index++] = (Square)((byte)(from[i] & 48) >> 4);
                row[index++] = (Square)((byte)(from[i] & 12) >> 2);
                row[index++] = (Square)((byte)from[i] & 3);
            }

            for (i = 0; i < 20; ++i)
            {
                if (row[i] == Square.NONE)
                    break;

                index = i;
            }
        }

        public static ConsoleColor GetColorX(bool isXView) => isXView ? ConsoleColor.Green : ConsoleColor.Red;
        public static ConsoleColor GetColorO(bool isXView) => isXView ? ConsoleColor.Blue : ConsoleColor.Green;

        public void ResetRow()
        {
            index = 0;
            row = new Square[20];
            for (int i = 0; i < 20; ++i)
            {
                row[i] = Square.NONE;
            }
        }
        
        public static void PrintRow(Square view, Square[] row)
        {
            int rowLen = row.Length;

            Console.Write("[ ");
            for (int i = 0; i < rowLen;  ++i)
            {
                switch (row[i])
                {
                    case Square.NONE:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(". ");
                        Console.ResetColor();
                        break;
                    case Square.X:
                        Console.ForegroundColor = view == Square.NONE ? GetColorX(false) : GetColorX(view == Square.X);
                        Console.Write("X ");
                        Console.ResetColor();
                        break;
                    case Square.O:
                        Console.ForegroundColor = view == Square.NONE ? GetColorO(true) : GetColorO(view == Square.X);
                        Console.Write("O ");
                        Console.ResetColor();
                        break;
                }
            }
            Console.WriteLine("]");
        }

        public void PrintRow(Square view)
        {
            PrintRow(view, row);
        }

        public void Add(Square s, uint times)
        {
            if (index >= 20)
                return;

            for (int i = 0; i < times && index < 20; ++i)
            {
                row[index++] = s;
            }
        }

        public Square Winner() => row[19];
        public bool IsEnd() => (index >= 20);

        public byte[] GetRowInBytes()
        {
            int rowLen = row.Length;
            byte[] result = new byte[5];
            byte tmp = 0, shift = 6, resultIndex = 0;

            for (uint i = 0; i < rowLen && resultIndex < 5; ++i)
            {
                if (shift == 0)
                {
                    tmp |= (byte)row[i];
                    shift = 6;

                    result[resultIndex++] = tmp;
                    tmp = 0;
                    continue;
                }

                tmp |= (byte)((byte) row[i] << shift);
                shift -= 2;
            }

            return result;
        }
    }
}