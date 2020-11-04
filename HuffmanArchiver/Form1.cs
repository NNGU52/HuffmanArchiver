using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Collections;
using System.IO;

namespace HuffmanArchiver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();  // метод, который инициализирует все копоненты, расположенные на форме
        }

        private void Form1_Load(object sender, EventArgs e)   // событие (отправитель объекта, базовый класс для классов, которые содержат данные о событиях)  
        {

        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)  // класс
        {
            Close();
        }

        private void button_change_Click(object sender, EventArgs e)
        {
            if (button_do.Text == "Архивировать")
            {
                groupBox1.Text = "Имя архива";
                groupBox2.Text = "Имя выходного файла";
                button_do.Text = "Разархивироать";

                String tmpStr = textBox1.Text;
                textBox1.Text = textBox2.Text;
                textBox2.Text = tmpStr;
            }
            else
            {
                groupBox1.Text = "Файл для архивации";
                groupBox2.Text = "Имя выходного архива";
                button_do.Text = "Архивировать";

                String tmpStr = textBox1.Text;
                textBox1.Text = textBox2.Text;
                textBox2.Text = tmpStr;
            }
        }

        private void button_open1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            String filename1 = openFileDialog1.FileName;
            textBox1.Text = filename1;
        }

        private void button_open2_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            String filename2 = saveFileDialog1.FileName;
            textBox2.Text = filename2;
        }

        private void button_do_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "")
            {
                toolStripStatusLabel1.Text = "Заполнены не все поля!";
                return;
            }

            if (!File.Exists(textBox1.Text))
            {
                toolStripStatusLabel1.Text = "Входной файл не существует!";
                return;
            }

            if (button_do.Text == "Архивировать")
            {
                String textFromFile;                                       // для хранения текста из файла

                // Чтение из файла
                using (FileStream fstream = File.OpenRead(textBox1.Text))
                {
                    byte[] byteArr = new byte[fstream.Length];
                    fstream.Read(byteArr, 0, byteArr.Length);               // чтение байт из файла
                    textFromFile = Encoding.Default.GetString(byteArr);     // преобразование байт в текст (строку)
                }

                HuffmanOperator HOperator = new HuffmanOperator(new HuffmanTree(textFromFile));

                // Запись в файл
                using (FileStream fstream = new FileStream(textBox2.Text, FileMode.OpenOrCreate))
                {
                    byte[] byteArr = HOperator.getBytedMsg();
                    fstream.Write(byteArr, 0, byteArr.Length);
                }
                using (FileStream fstream = new FileStream(textBox2.Text + ".codetable", FileMode.OpenOrCreate))
                {
                    byte[] byteArr = Encoding.Default.GetBytes(HOperator.getEncodingTable());
                    fstream.Write(byteArr, 0, byteArr.Length);
                }

                toolStripStatusLabel1.Text = "Готово! Коэффициент: " + Convert.ToString(Math.Round(HOperator.getCompressionRatio() * 100) / 100F);
            }
            else
            {
                byte[] bytesFromFile;                                       // для хранения текста из файла
                String codeTable;                                           // для хранения кодовой таблиц
                String zeroOneFromFile = "";                                // для хранения бит 1 и 0 в виде строки

                // Чтение из файла
                using (FileStream fstream = File.OpenRead(textBox1.Text))
                {
                    bytesFromFile = new byte[fstream.Length];
                    fstream.Read(bytesFromFile, 0, bytesFromFile.Length);   // чтение байт из файла
                }
                using (FileStream fstream = File.OpenRead(textBox1.Text + ".codetable"))
                {
                    byte[] byteArr = new byte[fstream.Length];
                    fstream.Read(byteArr, 0, byteArr.Length);               // чтение байт из файла
                    codeTable = Encoding.Default.GetString(byteArr);        // преобразование байт в текст (строку)
                }

                HuffmanOperator HOperator = new HuffmanOperator();

                String[] tempCodeTableArr = codeTable.Split('\n');
                String[] codeTableArrRaw = new String[tempCodeTableArr.Length - 1];
                Array.ConstrainedCopy(tempCodeTableArr, 0, codeTableArrRaw, 0, tempCodeTableArr.Length - 1);   // убираем последний элемент, так как послке Split он окажется пустым
                // Придётся так сделать, так как Split произведён по \n, которое может присутствовать непосредственно в кодовой таблице
                for (int i = 0; i < codeTableArrRaw.Length; i++)
                {
                    if (codeTableArrRaw[i].Equals(""))
                    {
                        codeTableArrRaw[i + 1] = '\n' + codeTableArrRaw[i + 1];
                        
                    }
                }
                String[] codeTableArr = codeTableArrRaw.Where(x => !String.IsNullOrEmpty(x)).ToArray();

                BitArray bitArr = new BitArray(bytesFromFile);

                // Преобразование бит в String 1 и 0
                for (int i = 0; i < bitArr.Length; i++)
                {
                    if (bitArr[i])
                    {
                        zeroOneFromFile += "1";
                    }
                    else
                    {
                        zeroOneFromFile += "0";
                    }
                }

                String extractedText = HOperator.extract(zeroOneFromFile, codeTableArr);

                // Запись в файл
                using (FileStream fstream = new FileStream(textBox2.Text, FileMode.OpenOrCreate))
                {
                    byte[] byteArr = Encoding.Default.GetBytes(extractedText);
                    fstream.Write(byteArr, 0, byteArr.Length);
                }

                toolStripStatusLabel1.Text = "Готово!";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
            timer1.Enabled = false;
        }

        private void toolStripStatusLabel1_TextChanged(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }
    }

    public class Node
    {
        private int frequence;          // частота
        private char letter;            // буква
        private Node leftChild;         // левый потомок
        private Node rightChild;        // правый потомок

        public Node(char letter, int frequence)
        {
            this.letter = letter;
            this.frequence = frequence;
        }

        public Node() { }               // перегрузка конструтора для безымянных узлов(см. выше в разделе о построении дерева Хаффмана)

        public void addChild(Node newNode)          // добавить потомка
        {
            if (leftChild == null)                  // если левый пустой=> правый тоже=> добавляем в левый
                leftChild = newNode;
            else
            {
                if (leftChild.getFrequence() <= newNode.getFrequence()) // в общем, левым потомком
                    rightChild = newNode;                               // станет тот, у кого меньше частота
                else
                {
                    rightChild = leftChild;
                    leftChild = newNode;
                }
            }

            frequence += newNode.getFrequence();                        // итоговая частота
        }

        public Node getLeftChild()
        {
            return leftChild;
        }

        public Node getRightChild()
        {
            return rightChild;
        }

        public int getFrequence()
        {
            return frequence;
        }

        public char getLetter()
        {
            return letter;
        }

        public bool isLeaf()
        {
            return leftChild == null && rightChild == null;
        }
    }

    class BinaryTree
    {
        private Node root;

        public BinaryTree()      // создаем объект дерева для каждого из узлов Node
        {
            root = new Node();
        }

        public BinaryTree(Node root)
        {
            this.root = root;
        }

        public int getFrequence()
        {
            return root.getFrequence();
        }

        public Node getRoot()
        {
            return root;
        }
    }

    class PriorityQueue
    {
        private List<BinaryTree> data;      // список очереди
        private int nElems;                 // кол-во элементов в очереди

        public PriorityQueue()
        {
            data = new List<BinaryTree>();
            nElems = 0;
        }

        public void insert(BinaryTree newTree)
        {
            if (nElems == 0)                // вставка
                data.Add(newTree);
            else
            {
                for (int i = 0; i < nElems; i++)
                {
                    if (data[i].getFrequence() > newTree.getFrequence())    // если частота вставляемого дерева меньше 
                    {
                        data.Insert(i, newTree);                            // чем част. текущего, то cдвигаем все деревья на позициях справа на 1 ячейку                   
                        break;                                              // затем ставим новое дерево на позицию текущего
                    }
                    if (i == nElems - 1)
                        data.Add(newTree);
                }
            }
            nElems++;                                                       // увеличиваем кол-во элементов на 1
        }

        public BinaryTree remove()                                          // удаление из очереди
        {
            BinaryTree tmp = data[0];                                       // копируем удаляемый элемент
            data.RemoveAt(0);                                               // собственно, удаляем
            nElems--;                                                       // уменьшаем кол-во элементов на 1
            return tmp;                                                     // возвращаем удаленный элемент(элемент с наименьшей частотой)
        }
    }

    public class HuffmanTree
    {
        private int ENCODING_TABLE_SIZE = 65535;         // длина кодировочной таблицы (127 - для латиницы, 1104 - для кирилицы, 10999 - +спецсимволы, 65535 - для всех)
        private String myString;                        // сообщение
        private BinaryTree huffmanTree;                 // дерево Хаффмана
        private int[] freqArray;                        // частотная таблица
        private String[] encodingArray;                 // кодировочная таблица

        //----------------constructor----------------------
        public HuffmanTree(String newString)
        {
            myString = newString;

            freqArray = new int[ENCODING_TABLE_SIZE];
            fillFrequenceArray();

            huffmanTree = getHuffmanTree();

            encodingArray = new String[ENCODING_TABLE_SIZE];
            fillEncodingArray(huffmanTree.getRoot(), "", "");
        }

        //--------------------frequence array------------------------
        private void fillFrequenceArray()
        {
            for (int i = 0; i < myString.Length; i++)
            {
                freqArray[(int)myString[i]]++;
            }
        }

        public int[] getFrequenceArray()
        {
            return freqArray;
        }

        //------------------------huffman tree creation------------------
        private BinaryTree getHuffmanTree()
        {
            PriorityQueue pq = new PriorityQueue();
            //алгоритм описан выше
            for (int i = 0; i < ENCODING_TABLE_SIZE; i++)
            {
                if (freqArray[i] != 0)
                {                                //если символ существует в строке
                    Node newNode = new Node((char)i, freqArray[i]);    //то создать для него Node
                    BinaryTree newTree = new BinaryTree(newNode);       //а для Node создать BinaryTree
                    pq.insert(newTree);                                 //вставить в очередь
                }
            }

            while (true)
            {
                BinaryTree tree1 = pq.remove();                         //извлечь из очереди первое дерево.

                try
                {
                    BinaryTree tree2 = pq.remove();                     //извлечь из очереди второе дерево

                    Node newNode = new Node();                          //создать новый Node
                    newNode.addChild(tree1.getRoot());                  //сделать его потомками два извлеченных дерева
                    newNode.addChild(tree2.getRoot());

                    pq.insert(new BinaryTree(newNode));
                }
                catch (ArgumentOutOfRangeException)
                {                 //осталось одно дерево в очереди
                    return tree1;
                }
            }
        }

        private BinaryTree getTree()
        {
            return huffmanTree;
        }

        //-------------------encoding array------------------
        void fillEncodingArray(Node node, String codeBefore, String direction)
        {    //заполнить кодировочную таблицу
            if (node.isLeaf())
            {
                encodingArray[(int)node.getLetter()] = codeBefore + direction;
            }
            else
            {
                fillEncodingArray(node.getLeftChild(), codeBefore + direction, "0");
                fillEncodingArray(node.getRightChild(), codeBefore + direction, "1");
            }
        }

        public String[] getEncodingArray()
        {
            return encodingArray;
        }

        public void displayEncodingArray()
        {                                        //для отладки
            fillEncodingArray(huffmanTree.getRoot(), "", "");

            Console.WriteLine("======================Encoding table====================");
            for (int i = 0; i < ENCODING_TABLE_SIZE; i++)
            {
                if (freqArray[i] != 0)
                {
                    Console.Write((char)i + " ");
                    Console.WriteLine(encodingArray[i]);
                }
            }
            Console.WriteLine("========================================================");
        }
        //-----------------------------------------------------
        public String getOriginalString()
        {
            return myString;
        }
    }

    public class HuffmanOperator
    {
        private byte ENCODING_TABLE_SIZE = 127;         //длина таблицы
        private HuffmanTree mainHuffmanTree;            //дерево Хаффмана (используется только для сжатия)
        private String myString;                        //исходное сообщение
        private int[] freqArray;                        //частотаная таблица
        private String[] encodingArray;                 //кодировочная таблица
        private double ratio;                           //коэффициент сжатия 


        public HuffmanOperator(HuffmanTree MainHuffmanTree)
        {       //for compress
            this.mainHuffmanTree = MainHuffmanTree;

            myString = mainHuffmanTree.getOriginalString();

            encodingArray = mainHuffmanTree.getEncodingArray();

            freqArray = mainHuffmanTree.getFrequenceArray();
        }

        public HuffmanOperator() { }                                 //for extract;

        //---------------------------------------compression-----------------------------------------------------------
        private String getCompressedString()
        {
            String compressed = "";
            String intermidiate = "";                               //промежуточная строка(без добавочных нулей)
            //System.out.println("=============================Compression=======================");
            //displayEncodingArray();
            for (int i = 0; i < myString.Length; i++)
            {
                intermidiate += encodingArray[myString[i]];
            }
            //Мы не можем писать бит в файл. Поэтому нужно сделать длину сообщения кратной 8=>
            //нужно добавить нули в конец(можно 1, нет разницы)
            byte counter = 0;//количество добавленных в конец нулей (байта вполне хватит: 0<=counter<8<127)
            for (int length = intermidiate.Length, delta = 8 - length % 8; counter < delta; counter++)
            {//delta - количество добавленных нулей
                intermidiate += "0";
            }

            //склеить кол-во добавочных нулей в бинарном предаствлении и промежуточную строку
            String zeroOne = "";
            byte[] b = BitConverter.GetBytes(counter);
            BitArray bitArr = new BitArray(b);
            // Преобразование бит в String 1 и 0
            for (int i = 7; i >= 0; i--)     // i-- чтобы сохранить правильный порядок бит
            {
                if (bitArr[i])
                {
                    zeroOne += "1";
                }
                else
                {
                    zeroOne += "0";
                }
            }
            compressed = zeroOne + intermidiate;

            //идеализированный коэффициент
            setCompressionRatio();
            //System.out.println("===============================================================");
            return compressed;
        }

        private void setCompressionRatio()
        {                    //посчитать идеализированный коэффициент 
            double sumA = 0, sumB = 0;                          //A-the original sum
            for (int i = 0; i < ENCODING_TABLE_SIZE; i++)
            {
                if (freqArray[i] != 0)
                {
                    sumA += 8 * freqArray[i];
                    sumB += encodingArray[i].Length * freqArray[i];
                }
            }
            ratio = sumA / sumB;
        }

        public byte[] getBytedMsg()
        {                           //final compression
            StringBuilder compressedString = new StringBuilder(getCompressedString());
            byte[] compressedBytes = new byte[compressedString.Length / 8];
            for (int i = 0; i < compressedBytes.Length; i++)
            {
                String bitStr = compressedString.ToString(i * 8, 8);
                BitArray bitArr = new BitArray(bitStr.Length);

                for (int j = 0; j < bitArr.Length; j++)
                {
                    bitArr.Set(j, Convert.ToString(bitStr[j]).Equals("1"));
                }

                byte[] bt = new byte[1];
                bitArr.CopyTo(bt, 0);
                compressedBytes[i] = bt[0];
            }
            return compressedBytes;
        }
        //---------------------------------------end of compression----------------------------------------------------------------
        //------------------------------------------------------------extract-----------------------------------------------------
        public String extract(String compressed, String[] newEncodingArray)
        {
            String decompressed = "";
            String current = "";
            String delta = "";
            encodingArray = newEncodingArray;

            //displayEncodingArray();
            //получить кол-во вставленных нулей
            for (int i = 0; i < 8; i++)
                delta += compressed[i];
            int ADDED_ZEROES = Convert.ToInt32(delta, 2);

            for (int i = 8, l = compressed.Length - ADDED_ZEROES; i < l; i++)
            {
                //i = 8, т.к. первым байтом у нас идет кол-во вставленных нулей
                current += compressed[i];
                for (int j = 0; j < encodingArray.Length; j++)
                {
                    String subStr = Convert.ToString(encodingArray[j]);         // чтобы можно было обрезать первый символ строки (букву), оставив только её код
                    if (current.Equals(subStr.Substring(1)))
                    {             //если совпало
                        decompressed += subStr.Substring(0, 1);                        //то добавляем элемент
                        current = "";                                   //и обнуляем текущую строку
                    }
                }
            }

            return decompressed;
        }

        public String getEncodingTable()
        {
            String enc = "";
            for (int i = 0; i < encodingArray.Length; i++)
            {
                if (freqArray[i] != 0)
                    enc += (char)i + encodingArray[i] + '\n';
            }
            return enc;
        }

        public double getCompressionRatio()
        {
            return ratio;
        }


        public void displayEncodingArray()
        {    //для отладки
            Console.WriteLine("======================Encoding table====================");
            for (int i = 0; i < ENCODING_TABLE_SIZE; i++)
            {
                //if (freqArray[i] != 0) {
                Console.Write((char)i + " ");
                Console.WriteLine(encodingArray[i]);
                //}
            }
            Console.WriteLine("========================================================");
        }
    }
}