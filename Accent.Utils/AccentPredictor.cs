using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accent.KShortestPaths.Model;
using Accent.KShortestPaths.Model.Abstracts;
using Accent.KShortestPaths.Common;
using Accent.Utils;
using System.IO;
using System.Text.RegularExpressions;
using Accent.KShortestPaths.Controller;
using System;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;
using System.Xml;
using System.Configuration;

namespace Accent.Utils
{
    public class AccentPredictor
    {
        private static Dictionary<string, int> _1Gram = new Dictionary<string, int>(); // 1-gram // khong co static
        private static Dictionary<string, int> _2Grams = new Dictionary<string, int>(); // 2-grams // khong co static
        private static Dictionary<string, int> _1Statistic = new Dictionary<string, int>(); //= new Dictionary<string, int>();
        private HashSet<string> accents;
        private int max = 18;
        private double MIN = -1000;
        private static long size1Gram = 216448;//0;
        private static long totalcount1Gram = 400508609;//0;

        private static long size2Grams = 5553699;// 0;
        private static long totalcount2Grams = 400508022;//0;
        private HashSet<string> globalPosibleChanges = new HashSet<string>();

        static bool isInitialized2Gram;
        static object initLock2Gram = new object();

        static bool isInitialized1Gram;
        static object initLock1Gram = new object();


        static bool isInitialized1Statistic;
        static object initLock1Statistic = new object();

        //public AccentPredictor(string _1GramFile, string _2GramsFile)
        //{
        //    Console.WriteLine("Loading NGrams...");
        //    loadNGram(_1GramFile, _2GramsFile, "Datasets/AccentInfo.txt");
        //    Console.WriteLine("Done!");
        //}
        //public AccentPredictor()
        //{
        //    string path1Gram = System.IO.Path.GetFullPath("news1gram_n2");
        //    string path2Gram = System.IO.Path.GetFullPath("news2grams_n2");
        //    string pathAccentInfo = System.IO.Path.GetFullPath("AccentInfo.txt");
        //    Console.WriteLine("Loading NGrams...");
        //    loadNGram(path1Gram, path2Gram, pathAccentInfo);
        //    Console.WriteLine("Done!");
        //}

        /// <summary>
        /// Khởi tạo danh sách cụm từ khi mapping
        /// </summary>
        /// <param name="path1Gram"></param>
        /// <param name="path2Gram"></param>
        //public void InitNgram(string path1Gram, string path2Gram, string path1Statistic = "")
        //{
        //    Console.WriteLine("Loading NGrams...");
        //    loadNGram(path1Gram, path2Gram, path1Statistic);
        //    Console.WriteLine("Done!");
        //}

        public void InitNgram2(string path1Gram, string path2Gram, string pathStatistic)
        {
            Console.WriteLine("Loading NGrams...");
            Console.WriteLine(DateTime.Now);
            loadNGram(path1Gram, path2Gram, pathStatistic);

            //string filePath1 = @"D:\news1gram.bin";
            //string filePath2 = @"D:\news2grams.bin";
            //WriteToFileBinary(_1Gram, filePath1);
            //WriteToFileBinary(_2Grams, filePath2);
            Console.WriteLine(DateTime.Now);
            Console.WriteLine("Done!");
        }
        static void WriteToFileBinary(Dictionary<string, int> dictionary, string file)
        {
            using (FileStream fs = File.OpenWrite(file))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Put count.
                writer.Write(dictionary.Count);
                // Write pairs.
                foreach (var pair in dictionary)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
            }
        }

        private int maxWordLength = 8;

        /// <summary>
        /// Lấy tất cả trường hợp dữ liệu từ vựng có thể thêm dấu
        /// </summary>
        /// <param name="input"></param>
        /// <param name="index"></param>
        /// <param name="posibleChanges">danh sách dấu</param>
        public virtual void getPosibleChanges(string input, int index, HashSet<string> posibleChanges)
        {
            if (input.Length > maxWordLength)
            {
                return;
            }
            if (index > input.Length)
            {
                return;
            }
            else if (index == input.Length)
            {

                if (_1Gram.ContainsKey(input))
                {
                    globalPosibleChanges.Add(input);
                }
                return;
            }
            char[] charSeq = input.ToCharArray();
            bool check = false;
            foreach (string s in posibleChanges)
            {

                if (s.IndexOf(charSeq[index]) != -1)
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        char[] tmp = input.ToCharArray();
                        tmp[index] = s[i];
                        string sTmp = "";
                        for (int j = 0; j < input.Length; j++)
                        {
                            sTmp += tmp[j] + "";
                        }

                        getPosibleChanges(sTmp, index + 1, posibleChanges);
                    }
                    check = true;
                }

            }
            if (!check)
            {
                getPosibleChanges(input, index + 1, posibleChanges);
            }
        }

        /// <summary>
        /// Load dữ liệu từ, cụm từ đã được tranning.
        /// </summary>
        /// <param name="fileIn"></param>
        /// <param name="is1Gram"></param>
        /// <returns></returns>
        public static Dictionary<string, int> getNgrams(string fileIn, bool is1Gram)//virtual
        {
            Dictionary<string, int> ngrams = new Dictionary<string, int>();

            //long size = 0, counts = 0;
            //try
            //{

            //    var file = new FileInfo(fileIn);
            //    var content = File.ReadAllLines(file.FullName, Encoding.UTF8);

            //    string line = "";
            //    for (int i = 0; i < content.Length; i++)
            //    {
            //        line = content[i];

            //        int indexSpace = line.LastIndexOf(' ');
            //        int indexTab = line.LastIndexOf('\t');

            //        if (indexTab < indexSpace)
            //        {
            //            indexTab = indexSpace;
            //        }
            //        string ngramWord = line.Substring(0, indexTab);
            //        if (!is1Gram)
            //        {
            //            string firstGram = ngramWord.Substring(0, ngramWord.IndexOf(' '));
            //            if (_1Statistic.ContainsKey(firstGram))
            //            {
            //                int val = _1Statistic[firstGram];
            //                _1Statistic[firstGram] = val + 1;
            //            }
            //            else
            //            {
            //                _1Statistic.Add(firstGram, 1);
            //            }
            //        }
            //        size++;
            //        int ngramCount = int.Parse(line.Substring(indexTab + 1));
            //        counts += ngramCount;

            //        //ngrams.Add(ngramWord, ngramCount);

            //        // Java - ngrams.put(ngramWord, ngramCount);
            //        // put - nếu có rồi thì update không thì thêm vào

            //        // C#
            //        if (ngrams.ContainsKey(ngramWord))
            //        {
            //            ngrams[ngramWord] = ngramCount;
            //            //ngrams.Add(ngramWord + " ", ngramCount);
            //        }
            //        else
            //        {
            //            ngrams.Add(ngramWord, ngramCount);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
            //if (is1Gram)
            //{
            //    size1Gram = size;
            //    totalcount1Gram = counts;
            //}
            //else
            //{
            //    size2Grams = size;
            //    totalcount2Grams = counts;
            //}

            try
            {
                using (FileStream fs = new FileStream(fileIn, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        int count = reader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            ngrams.Add(reader.ReadString(), reader.ReadInt32());
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return ngrams;

        }
        public static Dictionary<string, int> getNGram1Statistic(string fileIn)
        {
            Dictionary<string, int> ngrams = new Dictionary<string, int>();

            using (FileStream fs = new FileStream(fileIn, FileMode.Open, FileAccess.Read))
            {
                // đọc từ file txt
                using (StreamReader sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = "";
                        line = sr.ReadLine();

                        int indexSpace = line.LastIndexOf(' ');
                        int indexTab = line.LastIndexOf('\t');

                        if (indexTab < indexSpace)
                        {
                            indexTab = indexSpace;
                        }
                        string ngramWord = line.Substring(0, indexTab);
                        int ngramCount = int.Parse(line.Substring(indexTab + 1));
                        ngrams.Add(ngramWord, ngramCount);
                    }
                }
            }
            return ngrams;
        }


        public virtual HashSet<string> getAccentInfo()
        {
            HashSet<string> output = new HashSet<string>();
            string[] accent = new string[]
                {
                "UÙÚỦỤŨƯỪỨỬỰỮ",
                "eèéẻẹẽêềếểệễ",
                "oòóỏọõôồốổộỗơờớởợỡ",
                "OÒÓỎỌÕÔỒỐỔỘỖƠỜỚỞỢỠ",
                "uùúủụũưừứửựữ",
                "DĐ",
                "aàáảạãâầấẩậẫăằắẳặẵ",
                "dđ",
                "AÀÁẢẠÃÂẦẤẨẬẪĂẰẮẲẶẴ",
                "iìíỉịĩ",
                "EÈÉẺẸẼÊỀẾỂỆỄ",
                "YỲÝỶỴỸ",
                "IÌÍỈỊĨ",
                "yỳýỷỵỹ",
                };
            foreach (var item in accent)
            {
                output.Add(item);
            }
            return output;
        }

        public virtual HashSet<string> getVocab(string fileIn)
        {
            HashSet<string> output = new HashSet<string>();
            StreamReader fis = null;
            try
            {
                fis = File.OpenText(fileIn);
                string line = "";
                while ((line = fis.ReadLine()) != null)
                {
                    line = fis.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        output.Add(Regex.Split(line, "\\s+")[0]);
                    }
                }
            }
            catch (IOException)
            {
            }
            return output;
        }
        public virtual int getGramCount(string ngramWord, Dictionary<string, int> ngrams)
        {
            //if (ngramWord == null)
            //    ngramWord = "";
            if (!ngrams.ContainsKey(ngramWord))
            {
                return 0;
            }
            int output = ngrams[ngramWord];
            return output;
        }

        int maxn = 100;
        int maxp = 100;

        public virtual void loadNGram(string _1GramFile, string _2Gram2File, string _path1Statistic)
        {
            accents = getAccentInfo();
            _1Statistic = getNGram1Statistic(_path1Statistic);
            _1Gram = getNgrams(_1GramFile, true);
            _2Grams = getNgrams(_2Gram2File, true);
            // _1Gram = getNgrams(_1GramFile, true);
            //Initialize1Grams(_1GramFile);
            //Initialize2Grams(_2Gram2File);

            //NGramSingleton nGram = NGramSingleton.SingleInstance;

        }



        private void writeToFile(Dictionary<string, int> map, string fileOut)
        {
            try
            {

                FileStream fos = new FileStream(fileOut, FileMode.Create, FileAccess.Write);
                StreamWriter @out = new StreamWriter(fos, Encoding.UTF8);
                foreach (string ngrams in map.Keys)
                {
                    @out.Write(ngrams + "\t" + map[ngrams] + "\n");
                }
                @out.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                Console.Write(e.StackTrace);
            }
        }

        public HashSet<string> getPosibleChanges()
        {
            return globalPosibleChanges;
        }

        public virtual void setPosibleChanges()
        {
            globalPosibleChanges.Clear();
            globalPosibleChanges = new HashSet<string>();
        }

        public string predictAccentsWithMultiMatches(string sentence, int nResults, bool getWeight = true)
        {
            LinkedHashMap<string, double> output = new LinkedHashMap<string, double>();
            string @in = Utils.normaliseString(sentence);
            string lowercaseIn = @in.ToLower();
            string[] words = ("0 " + lowercaseIn + " 0").Split(' ');
            Graph graph = new VariableGraph();
            Dictionary<int, string> idxWordMap = new Dictionary<int, string>();
            int index = 0;
            int[] numberP = new int[words.Length];

            string[,] possibleChange = new string[words.Length, maxp];

            int[,] indices = new int[words.Length, maxp];
            int nVertex = 0;

            index = buildGraph(words, graph, idxWordMap, index, numberP, possibleChange, indices, nVertex);

            //Yen Algorithm for kShortestPaths
            YenTopKShortestPathsAlg yenAlg = new YenTopKShortestPathsAlg(graph);
            List<Accent.KShortestPaths.Model.Path> shortest_paths_list = yenAlg.get_shortest_paths(graph.get_vertex(0), graph.get_vertex(index - 1), nResults);
            foreach (Accent.KShortestPaths.Model.Path path in shortest_paths_list)
            {
                List<BaseVertex> pathVertex = path.get_vertices();
                string text = "";
                for (int i = 1; i < pathVertex.Count - 1; i++)
                {
                    BaseVertex vertext = pathVertex[i];
                    text += idxWordMap[vertext.get_id()] + " ";                   
                }
                text = ReplaceWordsIncorrect(text);
                output.Add(processOutput(@in, text.Trim()), path.get_weight());
            }

            // Không lấy trọng số đo lường cho các trường hợp thêm dấu.
            if (!getWeight)
                return output.ToString2();

            return output.ToString();
        }

        private int buildGraph(string[] words, Graph graph, Dictionary<int, string> idxWordMap, int index, int[] numberP, string[,] possibleChange, int[,] indices, int nVertex)
        {

            for (int i = 0; i < words.Length; i++)
            {
                globalPosibleChanges = new HashSet<string>();
                getPosibleChanges(words[i], 0, accents);
                if (globalPosibleChanges.Count() == 0)
                {
                    globalPosibleChanges.Add(words[i]);
                }
                numberP[i] = globalPosibleChanges.Count();
                nVertex += numberP[i];

                for (int y = 0; y < numberP[i]; y++)
                {
                    possibleChange[i, y] = globalPosibleChanges.ToArray()[y];
                }

                for (int j = 0; j < numberP[i]; j++)
                {
                    idxWordMap[index] = possibleChange[i, j];
                    indices[i, j] = index++;
                }
            }
            graph.initGraph(nVertex);
            for (int i = 1; i < words.Length; i++)
            {
                int recentPossibleNum = numberP[i];
                int oldPossibleNum = numberP[i - 1];

                for (int j = 0; j < recentPossibleNum; j++)
                {
                    for (int k = 0; k < oldPossibleNum; k++)
                    {
                        string _new = possibleChange[i, j];
                        string _old = possibleChange[i - 1, k];
                        int currentVertex = indices[i, j];
                        int previousVertex = indices[i - 1, k];
                        double log = -100.0;
                        int number2GRam = getGramCount(_old + " " + _new, _2Grams);
                        int number1GRam = getGramCount(_old, _1Gram);
                        if (number1GRam > 0 && number2GRam > 0)
                        {
                            log = Math.Log((double)(number2GRam + 1) / (number1GRam + _1Statistic[_old]));
                        }
                        else
                        {
                            log = Math.Log(1.0 / (2 * (size2Grams + totalcount2Grams)));
                        }

                        if (i == 2)
                        {
                            log += Math.Log((double)(number1GRam + 1) / (size1Gram + totalcount1Gram));
                        }
                        graph.add_edge(previousVertex, currentVertex, -log);

                    }
                }
            }
            return index;
        }

        //Using Dijkstra shortest path alg --> return online the best match: optimised for speed
        /// <summary>
        /// Lấy nội dung có dấu.
        /// </summary>
        /// <param name="inputContent"></param>
        /// <returns></returns>
        public string predictAccents(string inputContent)
        {
            string[] inputSentence = Regex.Split(inputContent, "[\\.\\!\\,\n\\;\\?]");
            StringBuilder output = new StringBuilder();
            foreach (string input in inputSentence)
            {
                setPosibleChanges();
                string @in = Utils.normaliseString(input);
                string lowercaseIn = @in.ToLower();
                string[] words = lowercaseIn.Split(' ');
                int[] numberP = new int[words.Length];
                int[,] trace = new int[words.Length, maxp];
                double[,] Q = new double[words.Length, maxp];
                string[,] possibleChange = new string[words.Length, maxp];
                for (int i = 0; i < words.Length; i++)
                {
                    globalPosibleChanges = new HashSet<string>();
                    getPosibleChanges(words[i], 0, accents);
                    if (globalPosibleChanges.Count() == 0)
                    {
                        globalPosibleChanges.Add(words[i]);
                    }
                    numberP[i] = globalPosibleChanges.Count();


                    // cach cu 1 dv
                    //obj1D = (possibleChange).Cast<string>().ToArray();
                    //globalPosibleChanges.CopyTo(obj1D, i);

                    //globalPosibleChanges.CopyTo((possibleChange).Cast<string>().ToArray(), i);
                    //possibleChange.SetValue(globalPosibleChanges.ToArray()[i].ToString(), i);
                    // globalPosibleChanges.CopyTo((possibleChange).Cast<string>().ToArray(), i);
                    //possibleChange = SingleToMulti(obj1D.Where(c => c != null).ToArray(), i);
                    //globalPosibleChanges.CopyTo(obj1D, i);
                    //int sqrt = obj1D.Where(c => c != null).ToArray().Length;//array.Length;

                    for (int y = 0; y < numberP[i]; y++)
                    {
                        possibleChange[i, y] = globalPosibleChanges.ToArray()[y];
                    }
                }


                for (int i = 0; i < words.Length; i++)
                {
                    for (int j = 0; j < maxp; j++)
                    {
                        trace[i, j] = 0;
                    }
                }

                for (int i = 0; i < numberP[0]; i++)
                {
                    Q[0, i] = 0.0;
                }

                if (words.Length == 1)
                {
                    int max = 0;
                    string sure = words[0];
                    for (int i = 0; i < numberP[0]; i++)
                    {
                        string possible = possibleChange[0, i]; //obj1D[i].ToString();//
                        int number1GRam = getGramCount(possible, _1Gram);
                        if (max < number1GRam)
                        {
                            max = number1GRam;
                            sure = possible;
                        }
                    }
                    output.Append(sure.Trim() + "\n");
                }
                else
                {
                    for (int i1 = 1; i1 < words.Length; i1++)
                    {
                        int recentPossibleNum = numberP[i1];
                        int oldPossibleNum = numberP[i1 - 1];
                        for (int j = 0; j < recentPossibleNum; j++)
                        {
                            Q[i1, j] = MIN;
                            double temp = MIN;
                            for (int k1 = 0; k1 < oldPossibleNum; k1++)
                            {
                                string _new = possibleChange[i1, j];
                                string _old = possibleChange[i1 - 1, k1];
                                double log = -100.0;
                                int number2GRam = getGramCount(_old + " " + _new, _2Grams);
                                int number1GRam = getGramCount(_old, _1Gram);
                                if (number1GRam > 0 && number2GRam > 0)
                                {
                                    log = Math.Log((double)(number2GRam + 1) / (number1GRam + _1Statistic[_old]));
                                }
                                else
                                {
                                    log = Math.Log(1.0 / (2 * (size2Grams + totalcount2Grams)));
                                }

                                if (i1 == 1)
                                {
                                    log += Math.Log((double)(number1GRam + 1) / (size1Gram + totalcount1Gram));
                                }
                                if (temp != Q[i1 - 1, k1])
                                {
                                    if (temp == MIN)
                                    {
                                        temp = Q[i1 - 1, k1];
                                    }
                                }
                                double value = Q[i1 - 1, k1] + log;

                                if (Q[i1, j] < value)
                                {

                                    Q[i1, j] = value;
                                    trace[i1, j] = k1;
                                }
                            }
                        }
                    }
                    double max = MIN;
                    int k = 0;
                    for (int l = 0; l < numberP[words.Length - 1]; l++)
                    {
                        if (max <= Q[words.Length - 1, l])
                        {
                            max = Q[words.Length - 1, l];
                            k = l;
                        }
                    }
                    string result = possibleChange[words.Length - 1, k];
                    k = trace[words.Length - 1, k];
                    int i = words.Length - 2;
                    while (i >= 0)
                    {
                        result = possibleChange[i, k] + " " + result;
                        k = trace[i--, k];
                    }
                    output.Append(processOutput(@in, result).Trim() + "\n");

                }
            }
            string resultOutput = output.ToString();
            resultOutput = ReplaceWordsIncorrect(resultOutput);
            return resultOutput.Trim();
        }

        private string ReplaceWordsIncorrect(string text)
        {
            if (String.IsNullOrEmpty(text))
                return "";

            var listWordNoCorrect = new Dictionary<string, string>()
            {
                {"số hữu","sở hữu"},
                {"đau đáu","đau đầu"},
                {"đầm dáng","đảm đang"},
                {"chào bán","chào bạn"},
                {"bị đầu tay","tay tôi bị đau"},
                {"tay tôi bị đầu","tay tôi bị đau"},
            };

            foreach (KeyValuePair<string, string> replacement in listWordNoCorrect)
            {
                text = Regex.Replace(text, replacement.Key, replacement.Value);
            }

            return text;
        }

        private string processOutput(string input, string output)
        {

            StringBuilder str = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char inputChar = input[i];
                char outputChar = ' ';
                if (i < output.Length)
                {
                    outputChar = output[i];
                }


                if (char.IsUpper(inputChar))
                {
                    str.Append(char.ToUpper(outputChar));
                }
                else
                {
                    str.Append(outputChar);
                }
            }
            return str.ToString();
        }


        public double getAccuracy(string fileIn)
        {
            FileProcessor fp = new FileProcessor();
            var file = new FileInfo(fileIn);

            var input = File.ReadAllText(file.FullName, Encoding.UTF8);

            //string input = fp.readFileNew(fileIn);
            input = Utils.normaliseString(input);
            string[] inputSentence = Regex.Split(input, "[\\.\\!\\,\n\\;\\?]"); //input.Split('.', '!', ',', '\n', ';', '?');
            string clearSign = CompareString.getUnsignedString(input).Trim();
            DateTime start = new DateTime();
            string @out = predictAccents(clearSign.Trim());
            double processedTime = (DateTime.Now.Millisecond - start.Millisecond) * 1.0 / 1000;
            Console.WriteLine("Processed time: " + processedTime + " seconds");
            string[] output = Regex.Split(@out, "\n");// @out.Split('\n');
            Console.WriteLine("Speed: " + output.Length * 1.0 / processedTime + " sents/second");
            Console.WriteLine("Speed: " + Regex.Split(@out, "\\s+").Length * 1.0 / processedTime + " words/second");
            int countAll = 1;
            int countMatch = 0;

            for (int i = 0; i < inputSentence.Length; i++)
            {

                if (i < output.Length)
                {
                    string[] wordsIn = Utils.normaliseString(inputSentence[i]).Trim().Split(' ');
                    string[] wordsOut = output[i].Trim().Split(' ');
                    bool shouldPrint = false;
                    if (wordsIn.Length == wordsOut.Length)
                    {
                        for (int j = 0; j < wordsOut.Length; j++)
                        {
                            if (wordsIn[j].Trim().Equals(wordsOut[j].Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                countMatch++;
                            }
                            else
                            {
                                shouldPrint = true;
                            }
                            countAll++;
                        }
                    }

                    if (shouldPrint)
                    {
                        Console.WriteLine("input: " + inputSentence[i]);
                        Console.WriteLine("output: " + output[i]);
                    }
                }
            }
            Console.WriteLine("Correct:" + countMatch);
            Console.WriteLine("All:" + countAll);

            return (double)(countMatch * 100) / countAll;
        }


        public sealed class NGramSingleton
        {
            private static readonly Lazy<NGramSingleton> singleInstance = new Lazy<NGramSingleton>(() => new NGramSingleton()); //private static Singleton singleInstance = null;  
            private NGramSingleton()
            {
                // ổ C từ server dgipro
                string _1GramFile = @"D:\HDHUY_V.1\BotProject\BotProject.Web\File\Datasets_Training_Accent\news1gram.bin";//@"C:\Hosting\Chatbot\File\Datasets_Training_Accent\news1gram"; 
                string _2GramFile = @"D:\HDHUY_V.1\BotProject\BotProject.Web\File\Datasets_Training_Accent\news2grams.bin";//@"C:\Hosting\Chatbot\File\Datasets_Training_Accent\news2grams"; 
                string _1StatisticFile = @"D:\HDHUY_V.1\BotProject\BotProject.Web\File\Datasets_Training_Accent\_1Statistic";//@"C:\Hosting\Chatbot\File\Datasets_Training_Accent\_1Statistic";//

                _1Statistic = getNGram1Statistic(_1StatisticFile);
                _1Gram = getNgrams(_1GramFile, true);
                _2Grams = getNgrams(_2GramFile, false);
            }
            public static NGramSingleton SingleInstance
            {
                get
                {
                    return singleInstance.Value;
                }
            }
            //public class DerivedClass : Singleton
            //{
            //}
        }
    }
}
