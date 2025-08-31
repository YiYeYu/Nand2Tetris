
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WORD = System.UInt16;

namespace Hack;

public partial class Assembler
{
    public class Parser
    {
        const char A_COMMAND_PREFIX = '@';
        const char L_COMMAND_PREFIX = '(';
        const char L_COMMAND_SUBFIX = ')';
        static readonly Regex regASymbol = new(@"[\w\d_.$:]*");
        static readonly Regex regSymbol = new(@"[\w_.$:][\w\d_.$:]*");
        static readonly Regex regConstanst = new(@"\d+");
        static readonly Regex regCCommand = new(@"(A?M?D?)=?(.+);?(\W+)");

        readonly StreamReader reader;
        string buffer = string.Empty;
        ECommandType commandType;
        string symbol = string.Empty;
        string dest = string.Empty;
        string comp = string.Empty;
        string jump = string.Empty;
        public int CurrentLine { get; protected set; }
        public int CurrentCommand { get; protected set; }
        public string Buffer { get => buffer; }

        public Parser(StreamReader reader)
        {
            this.reader = reader;
            CurrentLine = 0;
            CurrentCommand = 0;
        }

        ~Parser()
        {
        }

        /// <summary>
        /// 是否还有更多指令
        /// </summary>
        /// <returns></returns>
        public bool HasMoreCommands()
        {
            __eat();
            return !reader.EndOfStream;
        }

        void __eat()
        {
            if (reader.EndOfStream)
            {
                return;
            }

            __eatWhite();
            while (__eatComment())
            {
                __eatWhite();
            }
        }

        void __eatWhite()
        {
            for (int c = reader.Peek(); !reader.EndOfStream && char.IsWhiteSpace((char)c); c = reader.Peek())
            {
                // do nothing
                if (c == '\n')
                {
                    CurrentLine++;
                }
                reader.Read();
            }
        }

        bool __eatComment()
        {
            // 偷懒，单斜杆注释
            if (reader.Peek() != '/')
            {
                return false;
            }

            reader.ReadLine();
            CurrentLine++;
            return true;
        }

        /// <summary>
        /// 步进读取下一条指令
        /// 仅当HasMoreCommands为真时才能调用
        /// </summary>
        public void Advandce()
        {
            CurrentCommand++;
            CurrentLine++;

            buffer = reader.ReadLine() ?? string.Empty;
            Debug.Assert(buffer != null);

            buffer = __trim(buffer);

            char firstChar = buffer[0];

            commandType = firstChar switch
            {
                A_COMMAND_PREFIX => ECommandType.A_COMMAND,
                L_COMMAND_PREFIX => ECommandType.L_COMMAND,
                _ => ECommandType.C_COMMAND,
            };

            switch (commandType)
            {
                case ECommandType.A_COMMAND:
                    __tryParseA();
                    break;
                case ECommandType.C_COMMAND:
                    __tryParseC();
                    break;
                case ECommandType.L_COMMAND:
                    __tryParseL();
                    break;
                default:
                    throw new Exception(string.Format("Advandce unsupported cmd: {0}", commandType));
            }
        }

        bool __tryParseA()
        {
            var c0 = Utils.TryGetStringCharAt(buffer, 0);
            if (c0 != A_COMMAND_PREFIX)
            {
                return false;
            }

            symbol = buffer[1..];

            return regASymbol.IsMatch(symbol);
        }

        bool __tryParseC()
        {
            int equalIndex = buffer.IndexOf('=');
            int commonIndex = buffer.IndexOf(';');
            if (commonIndex < 0)
            {
                commonIndex = buffer.Length;
            }

            if (commonIndex < equalIndex)
            {
                return false;
            }

            if (equalIndex < 1)
            {
                dest = string.Empty;
            }
            else
            {
                dest = buffer[..equalIndex];
            }
            if (!Assembler.DestMap.ContainsKey(dest))
            {
                return false;
            }

            if (commonIndex == buffer.Length)
            {
                jump = string.Empty;
            }
            else
            {
                jump = buffer[(commonIndex + 1)..];
            }
            if (!Assembler.JumpMap.ContainsKey(jump))
            {
                return false;
            }

            comp = buffer[(equalIndex + 1)..commonIndex];
            if (!Assembler.CompMap.ContainsKey(comp))
            {
                return false;
            }

            return true;
        }

        bool __tryParseL()
        {
            var c0 = Utils.TryGetStringCharAt(buffer, 0);
            if (c0 != L_COMMAND_PREFIX)
            {
                return false;
            }

            var c_1 = Utils.TryGetStringCharAt(buffer, buffer.Length - 1);
            if (c_1 != L_COMMAND_SUBFIX)
            {
                return false;
            }

            symbol = buffer[1..-1];

            return regSymbol.IsMatch(symbol);
        }

        string __trim(string str)
        {
            StringBuilder stringBuilder = new(str);

            int idx = str.IndexOf("//");
            if (idx >= 0)
            {
                stringBuilder.Remove(idx, stringBuilder.Length - idx);
            }

            for (int i = stringBuilder.Length - 1; i >= 0; i--)
            {
                char c = stringBuilder[i];
                if (char.IsWhiteSpace(c))
                {
                    stringBuilder.Remove(i, 1);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 当前命令类型
        /// A指令：@value 数值 or 符号
        /// C指令：dest=comp;jump 0x111accccccdddjjj；dest为空，“=”省略；jump为空，“；”省略
        /// </summary>
        /// <returns></returns>
        public ECommandType CommandType()
        {
            return commandType;
        }

        /// <summary>
        /// 返回当前指令的符号或十进制值，当前为A指令或L指令时才能调用
        /// </summary>
        /// <returns></returns>
        public string Symbol()
        {
            return commandType switch
            {
                ECommandType.A_COMMAND or ECommandType.L_COMMAND => symbol,
                _ => throw new Exception(string.Format("get symbol from unsupported cmd: {0}", commandType)),
            };
        }

        /// <summary>
        /// 返回当前C指令的dest助记符（8种）
        /// </summary>
        /// <returns></returns>
        public string Dest()
        {
            return dest;
        }

        /// <summary>
        /// 返回当前C指令的cmop助记符（28种）
        /// </summary>
        /// <returns></returns>
        public string Comp()
        {
            return comp;
        }

        /// <summary>
        /// 返回当前C指令的jump助记符（8种）
        /// </summary>
        /// <returns></returns>
        public string Jump()
        {
            return jump;
        }
    }
}