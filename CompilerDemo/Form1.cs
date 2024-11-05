using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

namespace CompilerDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {


       }
       //源代码字符串
        private string sourceParagram;
        //全局指针
        private int pointer;
        //当前的二元式
        private token currentToken;
        private identifierTable idenTable;
        private tempVariableTable tempVarTable;
        //中间代码表
        private midCodeTable tacTable;
        private bool isDebug = false;
        //语法树
        private node ast;
        //输出
        private void print(string str) { listBox1.Items.Add(str); }
        private void debug(string str) { listBox2.Items.Add(str); }
        //二元式
        private class token
        {
            public string type { set; get; }

            public string value { set; get; }

            public token(string type, string value)
            {
                this.type = type;
                this.value = value;
            }

            public override string ToString()
            {

                return $"({this.type},{this.value})";
            }

        }
        //三元式
        private class triple
        {
            public string name { set; get; }
            public string type { set; get; }

            public string value { set; get; }

            public triple(string name)
            {
                this.name = name;
                this.type = "";
                this.value = "";
            }

            public int getValueAsINT32()
            {
                if (this.type != "int") return -1;
                if (this.value == "") return -1;
                return Convert.ToInt32(this.value);
            }
            public bool getValueAsBOOL()
            {
                if (this.type != "bool") return false;
                if (this.value == "") return false;
                if (this.value == "true") return true;

                return false;
            }

         
            public override string ToString()
            {

                return $" {this.name}  |  {this.type}  |  {this.value} ";
            }

        }
        private class identifierTable
        {
            public List<triple> table = new List<triple>();

            public identifierTable()
            {
                this.table.Clear();

            }

            public triple getIdentifierByName(string name)
            {
                foreach (triple t in table)
                {
                    if (t.name == name) return t;
                }

                return null;
            }

            public bool Add(string name)
            {

                if (getIdentifierByName(name) != null)
                {
                    return false;
                }

                table.Add(new triple(name));
                return true;

            }

            public bool UpdateTypeByName(string name, string type)
            {


                triple temp = getIdentifierByName(name);
                if (temp == null) return false;
                if (temp.type != "") { return false; }
                temp.type = type;


                return true;
            }

            public bool UpdateValueByName(string name, string value)
            {
                triple temp = getIdentifierByName(name);
                if (temp == null) return false;
                temp.value = value;

                return true;
            }

            public void dump(ListBox lb)
            {
                lb.Items.Add($" name  |  type  | value ");
                foreach (triple t in table)
                {
                    lb.Items.Add($"{t.ToString()}");
                }
            }
        }
        private class tempVariableTable
        {

            public List<triple> table = new List<triple>();

            public tempVariableTable()
            {
                this.table.Clear();
            }

            public triple createNewVariable()
            {
                //T0,T1,T2

                triple temp = new triple($"T{table.Count}");
                table.Add(temp);
                return temp;
            }


        }
        //四元式
        private class TAC
        {
            public string op { set; get; }

            public string oprand1 { set; get; }
            public string oprand2 { set; get; }
            public string result { set; get; }



            public TAC(string op, string oprand1, string oprand2, string result)
            {
                this.op = op;
                this.oprand1 = oprand1;
                this.oprand2 = oprand2;
                this.result = result;
            }


            public override string ToString()
            {

                return $"({this.op},{this.oprand1},{this.oprand2},{this.result})";
            }

        }
        //中间代码表
        private class midCodeTable
        {
            public List<TAC> table = new List<TAC>();

            public int NXQ { get { return table.Count; } }

            public midCodeTable()
            {
                this.table.Clear();
            }

            public void generate(string op, string oprand1, string oprand2, string result)
            {
                this.table.Add(new TAC(op, oprand1, oprand2, result));

            }

            //回填
            public void backpatch(int index, string result)
            {
                //table ;[0] [1] [2]
                if (index >= table.Count) return;
                this.table[index].result = result;
            }
            //dump到listBox
            public void dump(ListBox lb)
            {
                for (int i = 0; i < table.Count; i++)
                {
                    lb.Items.Add($"({i}) {table[i].ToString()}");
                }
            }
            //保存为TXT文件
            public void saveToTXT(string path)
            {
                StreamWriter sw = new StreamWriter(path);
                for (int i = 0; i < table.Count; i++)
                {
                    sw.WriteLine($"({i}) {table[i].ToString()}");
                }

                sw.Close();
            }


        }
        //语法树节点
        private class node
        {
            public string root;
            public List<node> children = new List<node>();
            public node(string root)
            {
                this.children.Clear();
                this.root = root;
            }
            public void addChild(node child)
            {
                this.children.Add(child);
            }
            public void addNodeAsFirstChild(node child)
            {
                children.Insert(0, child);
            }
            public node getFirstLeftNonLeafChild()
            {
                if (children.Count == 0) return null;
                node pointer = children[0];
                node lastpointer = null;
                while (pointer.children.Count != 0)
                {
                    lastpointer = pointer;
                    pointer = pointer.children[0];
                }
                return lastpointer;
            }
            public override string ToString()
            {
                //根-左-右
                string str = $"({this.root})";
                if (children.Count == 0) return str;
                str += "(";
                foreach (node child in children)
                {
                    if (child == null)
                    {
                        str += $"出错";
                    }
                    else
                    {
                        str += $"{child.ToString()}";
                    }
                }
                str += ")";
                return str;
            }
            public void saveToTXT(string path)
            {
                StreamWriter sw = new StreamWriter(path);
                sw.WriteLine(this.ToString());
                sw.Close();
            }
        }

        #region 词法分析
        private void button1_Click(object sender, EventArgs e)
        {
            isDebug = false;
            listBox1.Items.Clear();
            listBox2.Items.Clear();

            sourceParagram = textBox1.Text + "#";
            pointer = 0;

            //主程序入口
            currentToken = nextInput();
            print(currentToken.ToString());

            while (currentToken.value != "#" && currentToken.type != "error")
            {
                currentToken = nextInput();
                print(currentToken.ToString());
            }
            print("词法分析结束.");


        }
        //词法分析程序
        private token nextInput()
        {
            string tmpWord = "";
            int state = 0;

            while (sourceParagram[pointer] != '#')
            {
                char symbol = sourceParagram[pointer];

                if (symbol == ' ') { pointer++; continue; }

                if (state == 0)
                {
                    if (symbol == '$')
                    {
                        state = 100;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol >= '0' && symbol <= '9')
                    {
                        state = 200;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == ';')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("分号", tmpWord);
                    }
                    else if (symbol == 'i')
                    {
                        state = 400;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == 'b')
                    {
                        state = 500;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == ',')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("逗号", tmpWord);
                    }
                    else if (symbol == ':')
                    {
                        state = 700;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == 'o')
                    {
                        state = 800;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == 'n')
                    {
                        state = 900;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == 't')
                    {
                        state = 1000;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == 'f')
                    {
                        state = 1100;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == '(')
                    {

                        tmpWord += symbol;
                        pointer++;
                        return new token("左括号", tmpWord);
                    }
                    else if (symbol == ')')
                    {

                        tmpWord += symbol;
                        pointer++;
                        return new token("右括号", tmpWord);
                    }
                    else if (symbol == '<')
                    {
                        state = 1400;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == '>')
                    {
                        state = 1410;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == '=')
                    {
                        state = 1420;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == '*' || symbol == '/' || symbol == '%')
                    {

                        tmpWord += symbol;
                        pointer++;
                        return new token("乘法运算符", tmpWord);

                    }
                    else if (symbol == 'e')
                    {
                        state = 1700;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == '{')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("begin", tmpWord);
                    }
                    else if (symbol == '}')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("end", tmpWord);
                    }
                    else if (symbol == '+' || symbol == '-')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("加法运算符", tmpWord);

                    }
                    else if (symbol == 's')
                    {
                        state = 2100;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == 'w')
                    {
                        state = 2200;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == '"')
                    {
                        state = 2300;
                        tmpWord += symbol;
                        pointer++;
                        continue;
                    }
                    else if (symbol == 'a')
                    {
                        state = 2400;
                        tmpWord += symbol;
                        pointer++;
                        continue;
                    }
                    else
                    {
                         pointer++;
                        return new token("error", $"识别到{symbol} , 状态{state}无法识别");
                       
                    }
                }

                if (state == 100)
                {
                    if (symbol >= 'a' && symbol <= 'z')
                    {
                        state = 101;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }//$符号后的字母

                if (state == 101)
                {
                    if ((symbol >= '0' && symbol <= '9') || (symbol >= 'a' && symbol <= 'z'))
                    {
                        state = 101;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else if (symbol == '$')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("标识符", tmpWord);
                    }
                }//字母符号后的字母或数字

                if (state == 200)
                {
                    if (symbol >= '0' && symbol <= '9')
                    {
                        state = 200;
                        tmpWord += symbol;
                        pointer++;
                        if(sourceParagram[pointer] == '#')
                        {
                            return new token("整数", tmpWord);
                        }
                        continue;
                    }
                    else
                    {

                        return new token("整数", tmpWord);
                    }
                }//数字

                if (state == 400)
                {
                    if (symbol == 'n')
                    {
                        state = 401;
                        tmpWord += symbol;
                        pointer++;
                        continue;
                    }
                    else
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("if", tmpWord);
                    }

                }//int的n 或者if的f

                if (state == 401)
                {
                    if (symbol == 't')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("变量说明", tmpWord);
                    }
                }//int的t；

                //bool
                if (state == 500)
                {
                    if (symbol == 'o')
                    {
                        state = 501;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 501)
                {
                    if (symbol == 'o')
                    {
                        state = 502;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 502)
                {
                    if (symbol == 'l')
                    {

                        tmpWord += symbol;
                        pointer++;

                        return new token("变量说明", tmpWord);
                    }
                }

                //赋值号或冒号
                if (state == 700)
                {
                    if (symbol == '=')
                    {
                        tmpWord += symbol;
                        pointer++;

                        return new token("赋值号", tmpWord);
                    }
                    else
                    {

                        return new token(":", tmpWord);
                    }
                }

                //or
                if (state == 800)
                {
                    if (symbol == 'r')
                    {
                        tmpWord += symbol;
                        pointer++;

                        return new token("or", tmpWord);
                    }
                }

                //not
                if (state == 900)
                {
                    if (symbol == 'o')
                    {
                        state = 901;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }

                }
                if (state == 901)
                {
                    if (symbol == 't')
                    {
                        tmpWord += symbol;
                        pointer++;

                        return new token("not", tmpWord);
                    }
                }

                //true
                if (state == 1000)
                {
                    if (symbol == 'r')
                    {
                        state = 1001;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 1001)
                {
                    if (symbol == 'u')
                    {
                        state = 1002;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 1002)
                {
                    if (symbol == 'e')
                    {

                        tmpWord += symbol;
                        pointer++;

                        return new token("true", tmpWord);
                    }
                }

                //false
                if (state == 1100)
                {
                    if (symbol == 'a')
                    {
                        state = 1101;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 1101)
                {
                    if (symbol == 'l')
                    {
                        state = 1102;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 1102)
                {
                    if (symbol == 's')
                    {
                        state = 1103;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 1103)
                {
                    if (symbol == 'e')
                    {

                        tmpWord += symbol;
                        pointer++;

                        return new token("false", tmpWord);
                    }
                }

                //不等于及小于等于和小于
                if (state == 1400)
                {
                    if (symbol == '>')
                    {

                        tmpWord += symbol;
                        pointer++;

                        return new token("关系运算符", tmpWord);
                    }
                    else if (symbol == '=')
                    {

                        tmpWord += symbol;
                        pointer++;

                        return new token("关系运算符", tmpWord);
                    }
                    else
                    {

                        return new token("关系运算符", tmpWord);

                    }

                }

                //大于及大于等于
                if (state == 1410)
                {
                    if (symbol == '=')
                    {
                        tmpWord += symbol;
                        pointer++;

                        return new token("关系运算符", tmpWord);
                    }
                    else
                    {

                        return new token("关系运算符", tmpWord);
                    }
                }

                //等于
                if (state == 1420)
                {
                    if (symbol == '=')
                    {
                        tmpWord += symbol;
                        pointer++;

                        return new token("关系运算符", tmpWord);
                    }
                }

                //else
                if (state == 1700)
                {
                    if (symbol == 'l')
                    {
                        state = 1701;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 1701)
                {
                    if (symbol == 's')
                    {
                        state = 1702;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 1702)
                {
                    if (symbol == 'e')
                    {

                        tmpWord += symbol;
                        pointer++;

                        return new token("else", tmpWord);
                    }
                }

                //string
                if (state == 2100)
                {
                    if (symbol == 't')
                    {
                        state = 2101;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2101)
                {
                    if (symbol == 'r')
                    {
                        state = 2102;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2102)
                {
                    if (symbol == 'i')
                    {
                        state = 2103;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2103)
                {
                    if (symbol == 'n')
                    {
                        state = 2104;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2104)
                {
                    if (symbol == 'g')
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("变量说明", tmpWord);
                    }
                }

                //while
                if (state == 2200)
                {
                    if (symbol == 'h')
                    {
                        state = 2201;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2201)
                {
                    if (symbol == 'i')
                    {
                        state = 2202;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2202)
                {
                    if (symbol == 'l')
                    {
                        state = 2203;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2203)
                {
                    if (symbol == 'e')
                    {
                        tmpWord += symbol;
                        pointer++;

                        return new token("while", tmpWord);
                    }
                }
                //字符串
                if (state == 2300)
                {
                    if (symbol != '"')
                    {
                        state = 2300;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                    else
                    {
                        tmpWord += symbol;
                        pointer++;
                        return new token("字符串", tmpWord);
                    }
                }

                //and
                if (state == 2400)
                {
                    if (symbol == 'n')
                    {
                        state = 2401;
                        tmpWord += symbol;
                        pointer++;

                        continue;
                    }
                }
                if (state == 2401)
                {
                    if (symbol == 'd')
                    {
                        tmpWord += symbol;
                        pointer++;

                        return new token("and", tmpWord);
                    }
                }

                pointer++;
                debug("skipping " + symbol);
            }

            pointer++;
            return new token("#", $"#");
        }


  
        #endregion

        #region 语法分析
        private void button2_Click(object sender, EventArgs e)
        {
            isDebug = false;
            listBox1.Items.Clear();
            listBox2.Items.Clear();

            sourceParagram = textBox1.Text + "#";
            pointer = 0;

            currentToken = nextInput();
            debug(currentToken.ToString());

            parseProgram();//主程序入口

            print("语法分析结束");
        }

        private bool Match(string expectedTokenType)
        {
            //match 失败
            if (!isDebug)
            {
                if (currentToken.type != expectedTokenType)
                {

                    print($"匹配失败 : expect {expectedTokenType}.got {currentToken.type}");
                    return false;
                }
                print($"匹配成功 : expect {expectedTokenType}");
            }
            else
            {
                if (currentToken.type != expectedTokenType)
                {

                    debug($"匹配失败 : expect {expectedTokenType}.got {currentToken.type}");
                    return false;
                }
                debug($"匹配成功 : expect {expectedTokenType}");
            }
            currentToken = nextInput();
            debug(currentToken.ToString());
            return true;

        }

        private void parseProgram()
        {
            print("推导： <程序> → <变量说明部分> <语句部分>");
            parseDeclareSection();
            parseStatementSection();
        }
        private void parseDeclareSection()
        {
            print("推导： <变量说明部分> → <变量说明语句> 分号 A");

            parseDeclareStatement();
            if (!Match("分号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseA();

        }
        private void parseA()
        {
            print("选择产生式: A → <declareState> 分号 A | ε");
            if (currentToken.type == "变量说明")
            {
                print("推导: A → <declareState> 分号 A");
                parseDeclareStatement();
                if (!Match("分号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
                parseA();
            }
            else if (currentToken.type == "标识符" || currentToken.type == "if")
            {
                print("推导: A → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseDeclareStatement()
        {
            print("推导： <变量说明语句> → 变量说明 <标识符列表>");
            if (!Match("变量说明")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }

            parseVarList();

        }
        private void parseVarList()
        {
            print("推导： <标识符列表> → 标识符 B");
            if (!Match("标识符")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseB();
        }
        private void parseB()
        {
            print("选择产生式： B → 逗号 标识符 B | ε");
            if (currentToken.type == "逗号")
            {
                print("推导： B → 逗号 标识符 B");
                if (!Match("逗号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
                if (!Match("标识符")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
                parseB();
            }
            else if (currentToken.type == "分号")
            {
                print("推导： B → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseStatementSection()
        {
            print("推导： <语句部分> → <语句> C");
            parseStatement();
            parseC();
        }
        private void parseC()
        {
            print("选择产生式： C → 分号 <state> C | ε");
            if (currentToken.type == "分号")
            {
                print("推导： C → 分号 <state> C");
                Match("分号");
                parseStatement();
                parseC();
            }
            else if (currentToken.type == "#" || currentToken.type == "end")
            {
                print("推导： C → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseStatement()
        {
            print("选择产生式：<语句> → <赋值语句>|<条件语句>|<循环语句>");
            if (currentToken.type == "标识符")
            {
                print("推导： <语句> → <赋值语句>");
                parseAssignStatement();
            }
            else if (currentToken.type == "if")
            {
                print("推导： <语句> → <条件语句>");
                parseConditionStatement();
            }
            else if (currentToken.type == "while")
            {
                print("推导： <语句> → <循环语句>");
                parseLoopStatement();
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseAssignStatement()
        {
            print("推导： <赋值语句> → 标识符 赋值号 <表达式>");
            if (!Match("标识符")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match("赋值号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseExp();
        }
        private void parseExp()
        {
            print("推导： <表达式> → <disjunction>");
            parseDisjunction();

        }
        private void parseDisjunction()
        {
            print("推导： <disjunction> → <conjunction> D");
            parseConjunction();
            parseD();
        }
        private void parseD()
        {
            print("选择产生式：D → or < conjunction > D | ε");
            if (currentToken.type == "or")
            {
                print("推导: D → or < conjunction > D");
                Match("or");
                parseConjunction();
                parseD();
            }
            else if (currentToken.type == "end" || currentToken.type == "#"
                || currentToken.type == "分号" || currentToken.type == "右括号")
            {
                print("推导: D → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }

        }
        private void parseConjunction()
        {
            print("推导： <conjunction > → <inversion> E");
            parseInversion();
            parseE();
        }
        private void parseE()
        {
            print("选择产生式：E  → and < inversion > E | ε");
            if (currentToken.type == "and")
            {
                print("推导: E  → and < inversion > E ");
                Match("and");
                parseInversion();
                parseE();

            }
            else if (currentToken.type == "or" || currentToken.type == "#"
                || currentToken.type == "end" || currentToken.type == "分号"
                 || currentToken.type == "右括号")
            {
                print("推导: E → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseInversion()
        {
            print("选择产生式：< inversion > → not < inversion > | < 关系表达式 >");
            if (currentToken.type == "not")
            {
                print("推导: < inversion > → not < inversion >");
                Match("not");
                parseInversion();

            }
            else if (currentToken.type == "标识符" || currentToken.type == "true"
                || currentToken.type == "false" || currentToken.type == "字符串"
                || currentToken.type == "整数" || currentToken.type == "左括号")
            {
                print("推导: < inversion > → < 关系表达式 >");
                parseRel();
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseRel()
        {
            print("推导: < 关系表达式 > → <算术表达式> F");
            parseMath();
            parseF();
        }
        private void parseF()
        {
            print("选择产生式：F → 关系运算符 <算术表达式> | ε");
            if (currentToken.type == "关系运算符")
            {
                print("推导: F → 关系运算符 <算术表达式>");
                Match("关系运算符");
                parseMath();
            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号")
            {
                print("推导: F → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseMath()
        {
            print("推导: <算术表达式> → <term> G");
            parseTerm();
            parseG();
        }
        private void parseG()
        {
            print("选择产生式：G → 加法运算符 <term> G | ε");
            if (currentToken.type == "加法运算符")
            {
                print("推导: G → 加法运算符 <term> G");
                Match("加法运算符");
                parseTerm();
                parseG();
            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号" || currentToken.type == "关系运算符"
                )
            {
                print("推导: G → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseTerm()
        {
            print("推导: <term> → <factor> H");
            parseFactor();
            parseH();
        }
        private void parseH()
        {
            print("选择产生式：H → 乘法运算符 <factor> H | ε");
            if (currentToken.type == "乘法运算符")
            {
                print("推导: H → 乘法运算符 <factor> H");
                Match("乘法运算符");
                parseFactor();
                parseH();
            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号"
                || currentToken.type == "加法运算符" || currentToken.type == "关系运算符")
            {
                print("推导: H → ε");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseFactor()
        {
            if (currentToken.type == "左括号")
            {
                print("推导: <factor> → 左括号 <表达式> 右括号");
                Match("左括号");
                parseExp();
                if (!Match("右括号"))
                {
                    print($"匹配失败,不该出现 {currentToken.type}"); return;
                }
            }
            else if (currentToken.type == "标识符" || currentToken.type == "true"
                || currentToken.type == "false" || currentToken.type == "字符串"
                || currentToken.type == "整数")
            {
                string ty = currentToken.type;
                Match(currentToken.type);
                print($"推导: <factor> → {ty}");
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void parseConditionStatement()
        {
            print("推导: <条件语句> → if 左括号 <表达式> 右括号 <嵌套语句> else <嵌套语句>");
            if (!Match("if")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match("左括号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseExp();
            if (!Match("右括号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseNestStatement();
            if (!Match("else")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseNestStatement();

        }
        private void parseLoopStatement()
        {
            print("推导: <循环语句> → while 左括号<表达式>右括号: <嵌套语句>");
            if (!Match("while")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match("左括号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseExp();
            if (!Match("右括号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match(":")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseNestStatement();

        }
        private void parseNestStatement()
        {
            print("选择产生式：<嵌套语句> → <语句> 分号 | <复合语句>");
            if (currentToken.type == "if" || currentToken.type == "标识符" ||
                currentToken.type == "while")
            {
                print("推导: <嵌套语句> → <语句> 分号");

                parseStatement();
                if (!Match("分号")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            }
            else if (currentToken.type == "begin")
            {
                print("推导: <嵌套语句> → <复合语句>");
                parseCompState();
            }
            else
            {
                print($"匹配失败,不该出现 {currentToken.type}"); return;
            }

        }
        private void parseCompState()
        {
            debug("推导: <复合语句> → begin <语句部分> end");
            if (!Match("begin")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
            parseStatementSection();
            if (!Match("end")) { print($"匹配失败,不该出现 {currentToken.type}"); return; }
        }
        #endregion

        #region 语义分析
        private void button3_Click(object sender, EventArgs e)
        {
            isDebug = true;
            listBox1.Items.Clear();
            listBox2.Items.Clear();



            idenTable = new identifierTable();
            tempVarTable = new tempVariableTable();
            tacTable = new midCodeTable();

            sourceParagram = textBox1.Text + "#";
            pointer = 0;

            currentToken = nextInput();
            debug(currentToken.ToString());

            translateTACProgram();

            tacTable.dump(listBox1);


            //@是在用作字符串时无视符号的功能性作用 写路径时使用
            string path = Application.StartupPath + @"\outputTAC.txt";
            tacTable.saveToTXT(path);
            print("save to: " + path);

        }
        private void translateTACProgram()
        {
            debug("推导： <程序> → <变量说明部分> <语句部分>");
            translateTACDeclareSection();
            translateTACStatementSection();
        }
        private void translateTACDeclareSection()
        {
            debug("推导： <变量说明部分> → <变量说明语句> 分号 A");

            translateTACDeclareStatement();
            if (!Match("分号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            translateTACA();

        }
        private void translateTACA()
        {
            debug("选择产生式: A → <declareState> 分号 A | ε");
            if (currentToken.type == "变量说明")
            {
                debug("推导: A → <declareState> 分号 A");
                translateTACDeclareStatement();
                if (!Match("分号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
                translateTACA();
            }
            else if (currentToken.type == "标识符" || currentToken.type == "if")
            {
                debug("推导: A → ε");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void translateTACDeclareStatement()
        {
            debug("推导： <变量说明语句> → 变量说明 <标识符列表>");
            string type = currentToken.value;
            if (!Match("变量说明")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            translateTACVarList(type);
        }
        private void translateTACVarList(string type)
        {
            debug("推导： <标识符列表> → 标识符 B");
            string name = currentToken.value;
            if (!Match("标识符")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }

            //语义检查
            if (type != "string" && type != "int" && type != "bool")
            {
                debug($"不存在变量名: {type}");
                return;
            }
            idenTable.Add(name);
            idenTable.UpdateTypeByName(name, type);

            tacTable.generate("delcare", type, "null", $"{name}.type");
            translateTACB(type);
        }
        private void translateTACB(string type)
        {
            //type 是类似与string的类型名
            debug("选择产生式： B → 逗号 标识符 B | ε");
            if (currentToken.type == "逗号")
            {
                debug("推导： B → 逗号 标识符 B");
                if (!Match("逗号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }

                string name = currentToken.value;
                if (!Match("标识符")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }


                if (type != "string" && type != "int" && type != "bool")
                {
                    debug($"不存在变量名: {type}");
                    return;
                }
                //name是变量名 比如 $a$
                idenTable.Add(name);
                idenTable.UpdateTypeByName(name, type);

                tacTable.generate("delcare", type, "null", $"{name}.type");


                translateTACB(type);
            }
            else if (currentToken.type == "分号")
            {
                debug("推导： B → ε");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void translateTACStatementSection()
        {
            debug("推导： <语句部分> → <语句> C");
            translateTACStatement();
            translateTACC();
        }
        private void translateTACC()
        {
            debug("选择产生式： C → 分号 <state> C | ε");
            if (currentToken.type == "分号")
            {
                debug("推导： C → 分号 <state> C");
                Match("分号");
                translateTACStatement();
                translateTACC();
            }
            else if (currentToken.type == "#" || currentToken.type == "end")
            {
                debug("推导： C → ε");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void translateTACStatement()
        {
            debug("选择产生式：<语句> → <赋值语句>|<条件语句>|<循环语句>");
            if (currentToken.type == "标识符")
            {
                debug("推导： <语句> → <赋值语句>");
                translateTACAssignStatement();
            }
            else if (currentToken.type == "if")
            {
                debug("推导： <语句> → <条件语句>");
                translateTACConditionStatement();
            }
            else if (currentToken.type == "while")
            {
                debug("推导： <语句> → <循环语句>");
                translateTACLoopStatement();
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return;
            }
        }
        private void translateTACAssignStatement()
        {
            debug("推导： <赋值语句> → 标识符 赋值号 <表达式>");
            string name = currentToken.value;

            if (!Match("标识符")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match("赋值号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }

            triple Exp = translateTACExp();
            if (Exp == null)
            {
                debug("translateTACAssignStatement 中 <translateTACExp> 返回值为空");
                return;
            }

            //更新标识符列表，只有动态编译才能获得正确结果

            idenTable.UpdateValueByName(name, Exp.value);

            tacTable.generate("assign", Exp.name, "null", name);
        }
        private triple translateTACExp()
        {
            debug("推导： <表达式> → <disjunction>");

            triple E = translateTACDisjunction();
            if (E == null)
            {
                debug("translateTACExp 中 <translateTACDisjunction> 为空");
                return null;
            }
            return E;
        }
        private triple translateTACDisjunction()
        {
            debug("推导： <disjunction> → <conjunction> D");
            triple E1 = translateTACConjunction();
            if (E1 == null)
            {
                debug("translateTACDisjunction 中 <translateTACConjunction> 返回值为空");
                return null;
            }
            triple E = translateTACD(E1);
            if (E == null)
            {
                debug("translateTACDisjunction 中 <translateTACD> 返回值为空");
                return null;
            }
            return E;
        }
        private triple translateTACD(triple E1)
        {
            debug("选择产生式：D → or < conjunction > D | ε");
            if (currentToken.type == "or")
            {
                debug("推导: D → or < conjunction > D");
                Match("or");

                triple E2 = translateTACConjunction();
                if (E2 == null)
                {
                    debug("translateTACD 中 <translateTACConjunction> 返回值为空");
                    return null;
                }



                //根据E1 和 E2 进行or运算 获得T的正确值
                if (E1.type != "bool" || E2.type != "bool")
                {
                    debug($" or 的两操作数 {E1.name} 或 {E2.name} 不为bool");
                    return null;
                }

                triple T = tempVarTable.createNewVariable();
                T.type = "bool";
                if (E1.value == "true" || E2.value == "true")
                    T.value = "true";
                else T.value = "false";

                tacTable.generate("or", E1.name, E2.name, T.name);
                return translateTACD(T);
            }
            else if (currentToken.type == "end" || currentToken.type == "#"
                || currentToken.type == "分号" || currentToken.type == "右括号")
            {
                debug("推导: D → ε");
                return E1;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }
        }
        private triple translateTACConjunction()
        {
            debug("推导： <conjunction > → <inversion> E");
            triple E1 = translateTACInversion();

            if (E1 == null)
            {
                debug("translateTACConjunction 中 <translateTACInversion> 返回值为空");
                return null;
            }

            triple E = translateTACE(E1);
            if (E == null)
            {
                debug("translateTACConjunction 中 <translateTACE> 返回值为空");
                return null;
            }
            return E;

        }
        private triple translateTACE(triple E1)
        {
            debug("选择产生式：E  → and < inversion > E | ε");
            if (currentToken.type == "and")
            {
                debug("推导: E  → and < inversion > E ");
                Match("and");
                triple E2 = translateTACInversion();

                if (E2 == null)
                {
                    debug("translateTACE 中 <translateTACInversion> 返回值为空");
                    return null;
                }

                //根据E1 和 E2 进行 and 运算 获得T的正确值
                if (E1.type != "bool" || E2.type != "bool")
                {
                    debug($"两操作数不能进行and运算 ");
                    return null;
                }
               
                triple T = tempVarTable.createNewVariable();
                T.type = "bool";
                if (E1.value == "true" && E2.value == "true")
                    T.value = "true";
                else T.value = "false";

                tacTable.generate("and", E1.name, E2.name, T.name);
                triple E = translateTACE(T);
                if (E == null)
                {
                    debug("translateTACE 中 <translateTACE> 返回值为空");
                    return null;
                }

                return E;

            }
            else if (currentToken.type == "or" || currentToken.type == "#"
                || currentToken.type == "end" || currentToken.type == "分号"
                 || currentToken.type == "右括号")
            {
                debug("推导: E → ε");
                return E1;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private triple translateTACInversion()
        {
            debug("选择产生式：< inversion > → not < inversion > | < 关系表达式 >");

            if (currentToken.type == "not")
            {
                debug("推导: < inversion > → not < inversion >");
                Match("not");


                triple E1 = translateTACInversion();
                if (E1 == null)
                {
                    debug("translateTACInversion 中 <translateTACInversion> 返回值为空");
                    return null;
                }

                if (E1.type != "bool")
                {
                    debug($"非bool型变量 {E1.type}  {E1.value}不能取反");
                    return null;
                }
                triple T = tempVarTable.createNewVariable();
                T.type = "bool";
                //根据E1进行not运算 获得T的正确值
                T.value = (E1.value == "true") ? "false" : "true";
                tacTable.generate("not", E1.name, "null", T.name);
                return T;
            }

            else if (currentToken.type == "标识符" || currentToken.type == "true"
                || currentToken.type == "false" || currentToken.type == "字符串"
                || currentToken.type == "整数" || currentToken.type == "左括号")
            {
                debug("推导: < inversion > → < 关系表达式 >");
                triple E = translateTACRel();
                if (E == null)
                {
                    debug("translateTACInversion 中 <translateTACRel> 返回值为空");
                    return null;
                }
                return E;
            }

            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private triple translateTACRel()
        {
            debug("推导: < 关系表达式 > → <算术表达式> F");
            triple E1 = translateTACMath();

            if (E1 == null)
            {
                debug("translateTACRel 中 <translateTACMath> 返回值为空");
                return null;
            }

            triple E = translateTACF(E1);
            if (E == null)
            {
                debug("translateTACRel 中 <translateTACF> 返回值为空");
                return null;
            }
            return E;
        }
        private triple translateTACF(triple E1)
        {
            debug("选择产生式：F → 关系运算符 <算术表达式> | ε");
            if (currentToken.type == "关系运算符")
            {
                debug("推导: F → 关系运算符 <算术表达式>");
                string op = currentToken.value;

                Match("关系运算符");
                triple E2 = translateTACMath();

                if (E2 == null)
                {
                    debug("translateTACF 中 <translateTACMath> 返回值为空");
                    return null;
                }


                //根据E1进行关系运算 获得T的正确值
                
                int e1, e2;
                if(E1.type == "string")
                {
                    e1 = E1.value.Length;
                }
                else if(E1.type == "bool")
                {
                    e1 = (E1.value == "true") ? 1 : 0;
                }
                else if(E1.type == "int")
                {
                    e1 = E1.getValueAsINT32();
                    if(e1 == -1)
                    {
                        debug($"{E1.name} 赋值错误");
                        return null;
                    }
                }
                else
                {
                    debug($"{E1.type} 无法进行关系运算");
                    return null;
                }

                if (E2.type == "string")
                {
                    e2 = E2.value.Length;
                }
                else if (E2.type == "bool")
                {
                    e2 = (E2.value == "true") ? 1 : 0;
                }
                else if (E2.type == "int")
                {
                    e2 = E2.getValueAsINT32();
                    if (e2 == -1)
                    {
                        debug($"{E2.name} 赋值错误");
                        return null;
                    }
                }
                else
                {
                    debug($"{E2.type} 无法进行关系运算");
                    return null;
                }


                triple T = tempVarTable.createNewVariable();
                T.type = "bool";


                if (op == "<")
                {
                    op = "lt";
                    T.value = (e1 < e2) ? "true" : "false";
                 
                }
                else if (op == ">")
                {
                    op = "gt";
                    T.value = (e1 > e2) ? "true" : "false";
                }
                else if (op == "<>")
                {
                    op = "noteq";
                    T.value = (e1 != e2) ? "true" : "false";
                }
                else if (op == "<=")
                {
                    op = "lte";
                    T.value = (e1 <= e2) ? "true" : "false";
                }
                else if (op == ">=")
                {
                    op = "gte";
                    T.value = (e1 >= e2) ? "true" : "false";

                }
                else if (op == "==")
                {
                    op = "eq";
                    T.value = (e1 == e2) ? "true" : "false";
                }
                else
                {
                    debug($"不存在关系运算符: {op}");
                    return null;
                }

                tacTable.generate(op, E1.name, E2.name, T.name);
                return T;

            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号" )
            {
                debug("推导: F → ε");
                return E1;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private triple translateTACMath()
        {
            debug("推导: <算术表达式> → <term> G");
            triple E1 = translateTACTerm();
            if (E1 == null)
            {
                debug("translateTACMath 中 <translateTACTerm> 返回值为空");
                return null;
            }
            triple E = translateTACG(E1);
            if (E == null)
            {
                debug("translateTACMath 中 <translateTACG> 返回值为空");
                return null;
            }
            return E;
        }
        private triple translateTACG(triple E1)
        {
            debug("选择产生式：G → 加法运算符 <term> G | ε");
            if (currentToken.type == "加法运算符")
            {
                debug("推导: G → 加法运算符 <term> G");
                String op = currentToken.value;
                Match("加法运算符");
                triple E2 = translateTACTerm();
                if (E2 == null)
                {
                    debug("translateTACG 中 <translateTACTerm> 返回值为空");
                    return null;
                }


                int kind;
                if (E1.type == "string" && E2.type == "string")
                {
                    kind = 0;
                }
                else if ((E1.type == "int" || E1.type == "bool") && (E2.type == "int" || E2.type == "bool"))
                {
                    kind = 1;
                }
                else
                {
                    debug($"{E1.type}  {E1.value} 不能和 {E2.type}  {E2.value} 做加减法");
                    return null;
                }

                //根据E1和E2进行加法运算,获得T的正确的值

                //创建临时变量
                triple T = tempVarTable.createNewVariable();
                if (op == "+")
                {
                    op = "add";
                    if (kind == 0)
                    {
                        T.type = "string";
                        T.value = E1.value + E2.value;
                    }
                    else if (kind == 1)
                    {
                        T.type = "int";
                        int e1, e2;
                        if(E1.type == "bool")
                        {
                            if (E1.value == "true") e1 = 1; else e1 = 0;
                        }
                        else
                        {
                            e1 = E1.getValueAsINT32();
                            if(e1 == -1)
                            {
                                debug($"{E1.value}  赋值错误");
                                return null;
                            }
                        }
                        if (E2.type == "bool")
                        {
                            if (E2.value == "true") e2 = 1; else e2 = 0;
                        }
                        else
                        {
                            e2 = E2.getValueAsINT32();
                            if (e2 == -1)
                            {
                                debug($"{E2.value}  赋值错误");
                                return null;
                            }
                        }

                        T.value = (e1 + e2).ToString();

                    }

                }
                else if (op == "-")
                {
                    op = "sub";


                     if (kind == 1)
                    {
                        T.type = "int";
                        int e1, e2;
                        if (E1.type == "bool")
                        {
                            if (E1.value == "true") e1 = 1; else e1 = 0;
                        }
                        else
                        {
                            e1 = E1.getValueAsINT32();
                            if (e1 == -1)
                            {
                                debug($"{E1.value}  赋值错误");
                                return null;
                            }
                        }
                        if (E2.type == "bool")
                        {
                            if (E2.value == "true") e2 = 1; else e2 = 0;
                        }
                        else
                        {
                            e2 = E2.getValueAsINT32();
                            if (e2 == -1)
                            {
                                debug($"{E2.value}  赋值错误");
                                return null;
                            }
                        }
                        int res = e1 - e2;
                        T.value = (res>=0) ? res.ToString() : 0.ToString();
                    }
                    else
                    {
                        debug($"{E1.type}  {E1.value} 不能和 {E2.type}  {E2.value} 做减法");
                        return null;
                    }
                }

                tacTable.generate(op, E1.name, E2.name, T.name);

                triple E = translateTACG(T);
                if (E == null)
                {
                    debug("translateTACG 中 <translateTACG> 返回值为空");
                    return null;
                }
                return E;
            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号" || currentToken.type == "关系运算符"
                )
            {
                debug("推导: G → ε");
                return E1;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }
        }
        private triple translateTACTerm()
        {
            debug("推导: <term> → <factor> H");
            triple E1 = translateTACFactor();
            if (E1 == null)
            {
                debug("translateTACTerm 中 <translateTACFactor> 返回值为空");
                return null;
            }
            triple E = translateTACH(E1);
            if (E == null)
            {
                debug("translateTACTerm 中 <translateTACH> 返回值为空");
                return null;
            }
            return E;
        }
        private triple translateTACH(triple E1)
        {
            debug("选择产生式：H → 乘法运算符 <factor> H | ε");
            if (currentToken.type == "乘法运算符")
            {
                debug("推导: H → 乘法运算符 <factor> H");
                String op = currentToken.value;
                Match(currentToken.type);
                triple E2 = translateTACFactor();
                if (E2 == null)
                {
                    debug("translateTACH 中 <translateTACFactor> 返回值为空");
                    return null;
                }

                int kind;
                if (E1.type == "string" && (E2.type == "int" || E2.type == "bool"))
                {
                    kind = 0;
                }
                else if ((E1.type == "int" || E1.type == "bool") && (E2.type == "int" || E2.type == "bool"))
                {
                    kind = 1;
                }
                else
                {
                    debug($"{E1.type}  {E1.value}不能和 {E2.type}  {E2.value} 做乘除法");
                    return null;
                }


                //创建临时变量
                triple T = tempVarTable.createNewVariable();

                if (op == "*")
                {
                    op = "mul";
                    if (kind == 0)
                    {
                        T.type = "string";
                        int e2;
                        if(E2.type == "bool")
                        {
                            e2 = (E2.value == "true") ? 1 : 0;
                        }
                        else
                        {
                            e2 = E2.getValueAsINT32();
                            if(e2 == -1)
                            {
                                
                                    debug($"{E2.value}  赋值错误");
                                    return null;   
                            }
                        }
                        if(e2 <= 2)
                            for (int i = 0; i < e2; i++)
                            {
                                T.value += E1.value;
                            }
                        else
                            for (int i = 0; i < 2; i++)
                            {
                                T.value += E1.value;
                            }

                    }
                    else 
                    {
                        T.type = "int";
                        int e1;
                        int e2;
                        if (E1.type == "bool")
                        {
                            if (E1.value == "true") e1 = 1; else e1 = 0;
                        }
                        else
                        {
                            e1 = E1.getValueAsINT32();
                            if (e1 == -1)
                            {
                                debug($"{E1.value}  赋值错误");
                                return null;
                            }
                        }
                        if (E2.type == "bool")
                        {
                            if (E2.value == "true") e2 = 1; else e2 = 0;
                        }
                        else
                        {
                            e2 = E2.getValueAsINT32();
                            if (e2 == -1)
                            {
                                debug($"{E2.value}  赋值错误");
                                return null;
                            }
                        }

                        T.value = ((e1 * e2) % 10000).ToString();

                    }
                   
                }
                else if (op == "/")
                {
                    op = "div";

                    if (kind == 1)
                    {
                        T.type = "int";
                        int e1;
                        int e2;
                        if (E1.type == "bool")
                        {
                            if (E1.value == "true") e1 = 1; else e1 = 0;
                        }
                        else
                        {
                            e1 = E1.getValueAsINT32();
                            if (e1 == -1)
                            {
                                debug($"{E1.value}  赋值错误");
                                return null;
                            }
                        }
                        if (E2.type == "bool")
                        {
                            if (E2.value == "true") e2 = 1; else e2 = 0;
                        }
                        else
                        {
                            e2 = E2.getValueAsINT32();
                            if (e2 == -1)
                            {
                                debug($"{E2.value}  赋值错误");
                                return null;
                            }
                        }
                        if(e2 == 0)
                        {
                            debug($"{E2.value} 为0不能作为除数");
                            return null;
                        }
                        T.value = (e1 / e2).ToString();
                    }
                    else
                    {
                        debug($"{E1.type}  {E1.value}不能和 {E2.type}  {E2.value} 做除法");
                        return null;
                    }

                }
                else if (op == "%")
                {
                    op = "mod";
                    if (kind == 1)
                    {
                        T.type = "int";
                        int e1;
                        int e2;
                        if (E1.type == "bool")
                        {
                            if (E1.value == "true") e1 = 1; else e1 = 0;
                        }
                        else
                        {
                            e1 = E1.getValueAsINT32();
                            if (e1 == -1)
                            {
                                debug($"{E1.value}  赋值错误");
                                return null;
                            }
                        }
                        if (E2.type == "bool")
                        {
                            if (E2.value == "true") e2 = 1; else e2 = 0;
                        }
                        else
                        {

                            e2 = E2.getValueAsINT32();
                            if (e2 == -1)
                            {
                                debug($"{E2.value}  赋值错误");
                                return null;
                            }
                        }
                        if (e2 == 0)
                        {
                            debug($"{E2.value} 为0不能作为取模数");
                            return null;
                        }
                        T.value = (e1 % e2).ToString();
                    }
                    else
                    {
                        debug($"{E1.type}  {E1.value}不能和 {E2.type}  {E2.value} 做除法");
                        return null;
                    }

                }
               

                tacTable.generate(op, E1.name, E2.name, T.name);

                triple E = translateTACH(T);
                if (E == null)
                {
                    debug("translateTACH 中 <translateTACH> 返回值为空");
                    return null;
                }
                return E;
            }

            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号"
                 || currentToken.type == "加法运算符" ||currentToken.type == "关系运算符")
            {
                debug("推导: H → ε");
                return E1;
            }

            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private triple translateTACFactor()
        {

            if (currentToken.type == "左括号")
            {
                debug("推导: <factor> → 左括号 <表达式> 右括号");
                Match("左括号");
                triple Exp = translateTACExp();

                if (!Match("右括号"))
                {
                    debug($"匹配失败,不该出现 {currentToken.type}"); return null;
                }
                if (Exp == null)
                {
                    debug("translateTACFactor 中 <translateTACExp> 返回值为空");
                    return null;
                }
                return Exp;
            }

            else if (currentToken.type == "标识符" || currentToken.type == "true"
                || currentToken.type == "false" || currentToken.type == "字符串"
                || currentToken.type == "整数")
            {
                string ty = currentToken.type;
                string value = currentToken.value;
                triple t;
                if (ty == "标识符")
                {
                    debug($"推导: <factor> → {ty}");
                    Match(ty);

                    return idenTable.getIdentifierByName(value);
                }
                else
                {
                    debug($"推导: <factor> → {ty}");
                    Match(ty);
                }

                if (ty == "字符串")
                {
                     t = new triple(value);
                    t.type = "string";
                    t.value = value;
                   
                }

                else if (ty == "整数")
                {
                     t = new triple(value);
                    t.type = "int";
                    t.value = value;
                 
                }

                else if (ty == "true")
                {
                     t = new triple("true");
                    t.type = "bool";
                    t.value = "true";
           
                }

                else
                {
                     t = new triple("false");
                    t.type = "bool";
                    t.value = "false";
                  
                }
                return t;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }
        }
        private void translateTACConditionStatement()
        {
            debug("推导: <条件语句> → if 左括号 <表达式> 右括号 <嵌套语句> else <嵌套语句>");
            if (!Match("if")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match("左括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            triple T =  translateTACExp();
            if(T == null)
            {
                debug("translateTACConditionStatement 中 <translateTACExp> 返回值为空");
                return;
            }

            tacTable.generate("jnz",T.name,"null",(tacTable.NXQ + 2).ToString());
            int falseIndex  = tacTable.NXQ;
            tacTable.generate("jump", "null", "null","0");


            if (!Match("右括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            translateTACNestStatement();

            int exitIndex = tacTable.NXQ;
           
            tacTable.generate("jump", "null", "null","0");
            tacTable.backpatch(falseIndex, (tacTable.NXQ ).ToString());


            if (!Match("else")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }

          
            translateTACNestStatement();
            tacTable.backpatch(exitIndex, (tacTable.NXQ ).ToString());

        }
        private void translateTACLoopStatement()
        {
            debug("推导: <循环语句> → while 左括号<表达式>右括号: <嵌套语句>");
            if (!Match("while")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match("左括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }

            triple E = translateTACExp();
            if (E == null)
            {
                debug("translateTACLoopStatement 中 <translateTACExp> 返回值为空");
                return;
            }

       
            tacTable.generate("jnz", E.name, "null", (tacTable.NXQ + 2).ToString());
            int falseIndex = tacTable.NXQ;
            tacTable.generate("jump", "null", "null", "0");

          
          
            if (!Match("右括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            if (!Match(":")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            translateTACNestStatement();

            tacTable.generate("jump", "null", "null", "0");
            tacTable.backpatch(falseIndex,(tacTable.NXQ).ToString());



        }
        private void translateTACNestStatement()
        {
            debug("选择产生式：<嵌套语句> → <语句> 分号 | <复合语句>");
            if (currentToken.type == "if" || currentToken.type == "标识符" ||
                currentToken.type == "while")
            {
                debug("推导: <嵌套语句> → <语句> 分号");

                translateTACStatement();
                if (!Match("分号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            }
            else if (currentToken.type == "begin")
            {
                debug("推导: <嵌套语句> → <复合语句>");
                translateTACCompState();
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return;
            }

        }
        private void translateTACCompState()
        {
            debug("推导: <复合语句> → begin <语句部分> end");
            if (!Match("begin")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
            translateTACStatementSection();
            if (!Match("end")) { debug($"匹配失败,不该出现 {currentToken.type}"); return; }
        }
        #endregion

        #region AST语法树
        private void button4_Click(object sender, EventArgs e)
        {

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            sourceParagram = textBox1.Text + "#";
            pointer = 0;

            idenTable = new identifierTable();
            tempVarTable = new tempVariableTable();

            currentToken = nextInput();
            debug(currentToken.ToString());

            ast = translateASTProgram();
            print(ast.ToString());


            string path = Application.StartupPath + @"\outputAST.txt";
            ast.saveToTXT(path);
            print("Save to" + path);
        }
        private node translateASTProgram()
        {
            debug("推导： <程序> → <变量说明部分> <语句部分>");
            node root = new node("topLevel");
            node c1 = translateASTDeclareSection();
            node c2 = translateASTStatementSection();

            root.addChild(c1);
            root.addChild(c2);
            debug("AST语义分析结束");
            return root;
        }
        private node translateASTDeclareSection()
        {
            debug("推导： <变量说明部分> → <变量说明语句> 分号 A");
            node root = new node("declareList");
            node c1 = translateASTDeclareStatement();
            Match("分号");
            node c2 = translateASTA();

            root.addChild(c1);
            root.addChild(c2);
            return root;
        }
        private node translateASTA()
        {
            debug("选择产生式: A → <declareState> 分号 A | ε");

            if (currentToken.type == "变量说明")
            {
                debug("推导: A → <declareState> 分号 A");
                node root = new node("linkdeclareList");
                node c1 = translateASTDeclareStatement();
                Match("分号");
                node c2 = translateASTA();

                root.addChild(c1);
                root.addChild(c2);
                return root;
            }
            else if (currentToken.type == "标识符" || currentToken.type == "if")
            {
                debug("推导: A → ε");
                node root = new node("emptydeclareList");
                return root;
            }
            return null;
        }
        private node translateASTDeclareStatement()
        {
            debug("推导： <变量说明语句> → 变量说明 <标识符列表>");
            node root = new node("declear");
            Match("变量说明");
            node c1 = new node(currentToken.value);
            node c2 = translateASTVarList();

            root.addChild(c1);
            root.addChild(c2);
            return root;

        }
        private node translateASTVarList()
        {
            debug("推导： <标识符列表> → 标识符 B");
            node root = new node("varList");
            node c1 = new node(currentToken.value);
            Match("标识符");
            node c2 = translateASTB();
            root.addChild(c1);
            root.addChild(c2);
            return root;
        }
        private node translateASTB()
        {
            debug("选择产生式： B → 逗号 标识符 B | ε");
            if (currentToken.type == "逗号")
            {
                debug("推导： B → 逗号 标识符 B");
                node root = new node("linkvarList");
                node c1 = new node(currentToken.value);
                Match("逗号");
                Match("标识符");
                node c2 = translateASTB();
                root.addChild(c1);
                root.addChild(c2);
                return root;
            }
            else if (currentToken.type == "分号")
            {
                debug("推导： B → ε");
                node root = new node("emptyvarList");
                return root;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}");
                return null;
            }

        }
        private node translateASTStatementSection()
        {
            debug("推导： <语句部分> → <语句> C");
            node root = new node("seqList");
            node c1 = translateASTStatement();
            node c2 = translateASTC();

            root.addChild(c1);
            root.addChild(c2);
            return root;
        }
        private node translateASTC()
        {
            debug("选择产生式： C → 分号 <state> C | ε");
            if (currentToken.type == "分号")
            {
                debug("推导： C → 分号 <state> C");
                node root = new node("linkSeqList");
                Match("分号");
                node c1 = translateASTStatement();
                node c2 = translateASTC();
                root.addChild(c1);
                root.addChild(c2);
                return root;
            }
            else if (currentToken.type == "#" || currentToken.type == "end")
            {
                debug("推导： C → ε");
                return new node("emptySeqList");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTStatement()
        {
            debug("选择产生式：<语句> → <赋值语句>|<条件语句>|<循环语句>");
            if (currentToken.type == "标识符")
            {
                debug("推导： <语句> → <赋值语句>");
                return translateASTAssignStatement();
            }
            else if (currentToken.type == "if")
            {
                debug("推导： <语句> → <条件语句>");
                return translateASTConditionStatement();
            }
            else if (currentToken.type == "while")
            {
                debug("推导： <语句> → <循环语句>");
                return translateASTLoopStatement();
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTAssignStatement()
        {
            debug("推导： <赋值语句> → 标识符 赋值号 <表达式>");
            node root = new node("assign");
            if (!Match("标识符")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            if (!Match("赋值号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            node c1 = new node(currentToken.value);
            node c2 = translateASTExp();

            root.addChild(c1);
            root.addChild(c2);
            return root;
        }
        private node translateASTExp()
        {
            debug("推导： <表达式> → <disjunction>");
            node c1 = translateASTDisjunction();

            return c1;
        }
        private node translateASTDisjunction()
        {
            debug("推导： <disjunction> → <conjunction> D");
            node c1 = translateASTConjunction();
            node c2 = translateASTD();
            if (c2 == null) return null;
            if (c2.root == "empty")
            {
                return c1;
            }
            else
            {
                node insertpoint = c2.getFirstLeftNonLeafChild();
                if (insertpoint == null)
                {
                    c2.addNodeAsFirstChild(c1);
                    return c2;
                }
                else
                {
                    insertpoint.addNodeAsFirstChild(c1);
                    return c2;
                }
            }
        }
        private node translateASTD()
        {
            debug("选择产生式：D → or < conjunction > D | ε");
            if (currentToken.type == "or")
            {
                debug("推导: D → or < conjunction > D");
                string op = currentToken.value;
                Match("or");
                node c1 = translateASTConjunction();
                node c2 = translateASTD();

                if (c2 == null) return null;
                node root;
                if (c2.root == "empty")
                {
                    root = new node(op);
                    root.addChild(c1);
                    return root;
                }
                else
                {
                    node firstchild = new node(op);
                    firstchild.addChild(c1);
                    node insertpoint = c2.getFirstLeftNonLeafChild();

                    if (insertpoint == null)
                    {
                        c2.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                    else
                    {
                        insertpoint.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                }
            }
            else if (currentToken.type == "end" || currentToken.type == "#"
                || currentToken.type == "分号" || currentToken.type == "右括号")
            {
                debug("推导: D → ε");
                return new node("empty");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }


        }
        private node translateASTConjunction()
        {
            debug("推导： <conjunction > → <inversion> E");
            node c1 = translateASTInversion();
            node c2 = translateASTE();
            if (c2 == null) return null;
            if (c2.root == "empty")
            {
                return c1;
            }
            else
            {
                node insertpoint = c2.getFirstLeftNonLeafChild();
                if (insertpoint == null)
                {
                    c2.addNodeAsFirstChild(c1);
                    return c2;
                }
                else
                {
                    insertpoint.addNodeAsFirstChild(c1);
                    return c2;
                };
            }
        }
        private node translateASTE()
        {
            debug("选择产生式：E  → and < inversion > E | ε");
            if (currentToken.type == "and")
            {
                debug("推导: E  → and < inversion > E ");
                string op = currentToken.value;
                Match("and");
                node c1 = translateASTInversion();
                node c2 = translateASTE();


                if (c2 == null) return null;
                node root;
                if (c2.root == "empty")
                {
                    root = new node(op);
                    root.addChild(c1);
                    return root;
                }
                else
                {
                    node firstchild = new node(op);
                    firstchild.addChild(c1);
                    node insertpoint = c2.getFirstLeftNonLeafChild();

                    if (insertpoint == null)
                    {
                        c2.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                    else
                    {
                        insertpoint.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                }

            }
            else if (currentToken.type == "or" || currentToken.type == "#"
                || currentToken.type == "end" || currentToken.type == "分号"
                 || currentToken.type == "右括号")
            {
                debug("推导: E → ε");
                return new node("empty");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTInversion()
        {
            debug("选择产生式：< inversion > → not < inversion > | < 关系表达式 >");
            if (currentToken.type == "not")
            {
                debug("推导: < inversion > → not < inversion >");
                node root = new node("not");
                Match("not");
                node c1 = translateASTInversion();
                root.addChild(c1);
                return root;
            }
            else if (currentToken.type == "标识符" || currentToken.type == "true"
                || currentToken.type == "false" || currentToken.type == "字符串"
                || currentToken.type == "整数" || currentToken.type == "左括号")
            {
                debug("推导: < inversion > → < 关系表达式 >");
                return translateASTRel();
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTRel()
        {
            debug("推导: < 关系表达式 > → <算术表达式> F");
            node c1 = translateASTMath();
            node c2 = translateASTF();
            if (c2 == null) return null;
            if (c2.root == "empty")
            {
                return c1;
            }
            else
            {
                c2.addNodeAsFirstChild(c1);
                return c2;
            }
        }
        private node translateASTF()
        {
            debug("选择产生式：F → 关系运算符 <算术表达式> | ε");
            if (currentToken.type == "关系运算符")
            {
                debug("推导: F → 关系运算符 <算术表达式>");
                string op = currentToken.value;
                if (op == "<") { op = "lt"; }
                else if (op == ">") { op = "gt"; }
                else if (op == "<>") { op = "noteq"; }
                else if (op == "==") { op = "eq"; }
                else if (op == "<=") { op = "lte"; }
                else if (op == ">=") { op = "gte"; }
                Match("关系运算符");
                node c1 = translateASTMath();
                node c2 = translateASTF();
                if (c2.root == "empty")
                {
                    node root = new node(op);
                    root.addChild(c1);
                    return root;
                }
                else
                {
                    node newFirstChild = new node(op);
                    newFirstChild.addChild(c1);
                    node InsertPoint = c2.getFirstLeftNonLeafChild();
                    if (InsertPoint == null)
                    {
                        c2.addNodeAsFirstChild(c1);
                    }
                    else
                    {
                        InsertPoint.addNodeAsFirstChild(c1);
                    }
                    return c2;
                }
            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号")
            {
                debug("推导: F → ε");
                return new node("empty");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTMath()
        {
            debug("推导: <算术表达式> → <term> G");
            node c1 = translateASTTerm();
            node c2 = translateASTG();

            if (c2 == null) return null;
            if (c2.root == "empty")
            {
                return c1;
            }
            else
            {
                node insertpoint = c2.getFirstLeftNonLeafChild();
                if (insertpoint == null)
                {
                    c2.addNodeAsFirstChild(c1);
                    return c2;
                }
                else
                {
                    insertpoint.addNodeAsFirstChild(c1);
                    return c2;
                }
            }

        }
        private node translateASTG()
        {
            debug("选择产生式：G → 加法运算符 <term> G | ε");
            if (currentToken.type == "加法运算符")
            {
                debug("推导: G → 加法运算符 <term> G");
                string op = currentToken.value;
                if (op == "+") { op = "sum"; }
                else if (op == "-") { op = "sub"; }
                Match("加法运算符");
                node c1 = translateASTTerm();
                node c2 = translateASTG();
                if (c2 == null) return null;
                if (c2.root == "empty")
                {
                    node root = new node(op);
                    root = new node(op);
                    root.addChild(c1);
                    return root;
                }
                else
                {

                    node firstchild = new node(op);
                    firstchild.addChild(c1);
                    node insertpoint = c2.getFirstLeftNonLeafChild();

                    if (insertpoint == null)
                    {
                        c2.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                    else
                    {
                        insertpoint.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                }
            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号"
                || currentToken.type == "关系运算符")
            {
                debug("推导: G → ε");
                return new node("empty");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }


        }
        private node translateASTTerm()
        {
            debug("推导: <term> → <factor> H");
            node c1 = translateASTFactor();
            node c2 = translateASTH();

            if (c2 == null) return null;
            if (c2.root == "empty")
            {
                return c1;
            }
            else
            {
                node insertpoint = c2.getFirstLeftNonLeafChild();
                if (insertpoint == null)
                {
                    c2.addNodeAsFirstChild(c1);
                    return c2;
                }
                else
                {
                    insertpoint.addNodeAsFirstChild(c1);
                    return c2;
                }
            }
        }
        private node translateASTH()
        {
            debug("选择产生式：H → 乘法运算符 <factor> H | ε");
            if (currentToken.type == "乘法运算符")
            {
                debug("推导: H → 乘法运算符 <factor> H");
                string op = currentToken.value;
                if (op == "*") { op = "mul"; }
                else if (op == "/") { op = "div"; }
                else if (op == "%") { op = "mod"; }
                Match("乘法运算符");
                node c1 = translateASTFactor();
                node c2 = translateASTH();

                if (c2 == null) return null;
                if (c2.root == "empty")
                {
                    node root = new node(op);
                    root.addChild(c1);
                    return root;
                }
                else
                {
                    node firstchild = new node(op);
                    firstchild.addChild(c1);
                    node insertpoint = c2.getFirstLeftNonLeafChild();

                    if (insertpoint == null)
                    {
                        c2.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                    else
                    {
                        insertpoint.addNodeAsFirstChild(firstchild);
                        return c2;
                    }
                }
            }
            else if (currentToken.type == "and" || currentToken.type == "or"
                || currentToken.type == "#" || currentToken.type == "分号"
                || currentToken.type == "end" || currentToken.type == "右括号"
                || currentToken.type == "关系运算符" || currentToken.type == "加法运算符")
            {
                debug("推导: H → ε");
                return new node("empty");
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTFactor()
        {
            debug("选择产生式：<factor> → 标识符 | true | false | 字符串 | 整数| 左括号 <表达式> 右括号");
            if (currentToken.type == "标识符")
            {
                debug("推导：<factor> → 标识符");
                node leave = new node(currentToken.value);
                Match("标识符");
                return leave;
            }
            else if (currentToken.type == "true")
            {
                debug("推导：<factor> →true");
                Match("true");
                return new node("true");
            }
            else if (currentToken.type == "false")
            {
                debug("推导：<factor> →false");
                Match("false");
                return new node("false");
            }
            else if (currentToken.type == "字符串")
            {
                debug("推导：<factor> →字符串");
                node leave = new node(currentToken.value);
                Match("字符串");
                return leave;
            }
            else if (currentToken.type == "整数")
            {
                debug("推导：<factor> →整数");
                node leave = new node(currentToken.value);
                Match("整数");
                return leave;
            }
            if (currentToken.type == "左括号")
            {
                debug("推导: <factor> → 左括号 <表达式> 右括号");
                Match("左括号");
                node c1 = translateASTExp();
                if (!Match("右括号"))
                {
                    debug($"匹配失败,不该出现 {currentToken.type}"); return null;
                }
                return c1;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTConditionStatement()
        {
            debug("推导: <条件语句> → if 左括号 <表达式> 右括号 <嵌套语句> else <嵌套语句>");
            node root = new node("ifThenElse");
            if (!Match("if")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            if (!Match("左括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            node c1 = translateASTExp();
            if (!Match("右括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            node c2 = translateASTNestStatement();
            if (!Match("else")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            node c3 = translateASTNestStatement();

            root.addChild(c1);
            root.addChild(c2);
            root.addChild(c3);
            return root;
        }
        private node translateASTLoopStatement()
        {
            debug("推导: <循环语句> → while 左括号<表达式>右括号: <嵌套语句>");
            node root = new node("while");
            if (!Match("while")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            if (!Match("左括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            node c1 = translateASTExp();
            if (!Match("右括号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            if (!Match(":")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            node c2 = translateASTNestStatement();
            root.addChild(c1);
            root.addChild(c2);
            return root;
        }
        private node translateASTNestStatement()
        {
            debug("选择产生式：<嵌套语句> → <语句> 分号 | <复合语句>");
            if (currentToken.type == "if" || currentToken.type == "标识符" ||
                currentToken.type == "while")
            {
                debug("推导: <嵌套语句> → <语句> 分号");

                node c1 = translateASTStatement();
                if (!Match("分号")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
                return c1;
            }
            else if (currentToken.type == "begin")
            {
                debug("推导: <嵌套语句> → <复合语句>");
                node c1 = translateASTCompState();
                return c1;
            }
            else
            {
                debug($"匹配失败,不该出现 {currentToken.type}"); return null;
            }

        }
        private node translateASTCompState()
        {
            debug("推导：<复合语句> → begin <语句部分> end");
            if (!Match("begin")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            node c1 = translateASTStatementSection();
            if (!Match("end")) { debug($"匹配失败,不该出现 {currentToken.type}"); return null; }
            return c1;
        }
    }
    #endregion
}

