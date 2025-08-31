
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using WORD = System.UInt16;

namespace Hack;

public partial class VMTranslator
{
    public class Parser
    {
        static readonly Regex regCommand = new(@"(\S+)(\s+(\S+)){0,2}\s*(//.*)?");

        readonly StreamReader reader;
        string buffer = string.Empty;
        ECommandType commandType;
        public int CurrentLine { get; protected set; }
        public WORD CurrentCommand { get; protected set; }
        public string Buffer { get => buffer; }
        string arg1 = string.Empty;
        string arg2 = string.Empty;
        int tokenIndex = 0;


        public Parser(StreamReader reader)
        {
            this.reader = reader;
            CurrentLine = 0;
            CurrentCommand = 0;
        }

        ~Parser()
        {
        }

        public void Reset()
        {
            reader.BaseStream.Position = 0;
            CurrentLine = 0;
            CurrentCommand = 0;
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
            buffer = reader.ReadLine() ?? string.Empty;
            Debug.Assert(buffer != null);

            buffer = __trim(buffer);
            __resetToken();

            string? cmd = __token();
            if (string.IsNullOrEmpty(cmd) || !VMTranslator.commandMap.TryGetValue(cmd, out commandType))
            {
                throw new Exception(string.Format("Advandce unsupported cmd: {0}", buffer));
            }

            arg1 = __token();
            arg2 = __token();

            CurrentCommand++;
            CurrentLine++;
        }

        string __trim(string str)
        {
            StringBuilder stringBuilder = new(str);

            int idx = str.IndexOf("//");
            if (idx >= 0)
            {
                stringBuilder.Remove(idx, stringBuilder.Length - idx);
            }

            // for (int i = stringBuilder.Length - 1; i >= 0; i--)
            // {
            //     char c = stringBuilder[i];
            //     if (char.IsWhiteSpace(c))
            //     {
            //         stringBuilder.Remove(i, 1);
            //     }
            // }

            return stringBuilder.ToString();
        }

        void __resetToken()
        {
            tokenIndex = 0;
        }

        string __token()
        {
            int lastWordIndex = -1;
            for (int i = tokenIndex; i < buffer.Length; i++)
            {
                tokenIndex++;

                char c = buffer[i];
                if (char.IsWhiteSpace(c))
                {
                    if (lastWordIndex < 0)
                    {
                        continue;
                    }
                    break;
                }

                if (lastWordIndex >= 0)
                {
                    continue;
                }
                lastWordIndex = i;
            }

            if (lastWordIndex < 0)
            {
                return string.Empty;
            }

            return buffer[lastWordIndex..(tokenIndex - 1)];
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
        /// 返回当前指令的第一个操作数
        /// </summary>
        /// <returns></returns>
        public string Arg1()
        {
            return arg1;
        }

        /// <summary>
        /// 返回当前指令的第二个操作数
        /// </summary>
        /// <returns></returns>
        public string Arg2()
        {
            return arg2;
        }

    }
}