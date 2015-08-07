using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TherapyScheduler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int weekLength = 5;//So I only have to make one edit if block scheduling is added.
        public const int daySlots = 25; //Because I know I am going to miscalculate this a lot.
        public const int diffTherapyCost = 20; //Because it really should be here.
        static List<School> schools = new List<School>();
        delegate int quarterGet(int a);
        Student currentStudent = new Student();
        School currentSchool = new School();
        Grid schoolViewGrid = new Grid();
        RoutedEventHandler studentReturner;
        RoutedEventHandler schoolReturner;
        Label currentStudentLabel;
        Label currentSchoolLabel;
        const int max = 10000;
        const int slots = 6;
        static System.Random gigi = new Random();

        static int getTime(string toParse, quarterGet todo)
        {
            if (toParse == "")
                return daySlots;
            int pos = toParse.IndexOf(':');
            int hourBegin = int.Parse(toParse.Substring(0, pos));
            int quarterBegin = todo(int.Parse(toParse.Substring(pos + 1)));
            return 4 * hourBegin + quarterBegin;
        }

        static int leastInteger(int a)
        {
            if (a % 15 == 0)
                return a / 15;
            else
                return (a / 15) + 1;
        }

        private static string boolToTimeString(bool[,] temp, int index)
        {
            bool[] edgeFall = new bool[daySlots];
            bool last = temp[index, 0];
            edgeFall[0] = last;
            string returny = "";
            for (int a = 1; a < daySlots; ++a)
            {
                if (last == temp[index, a])
                    edgeFall[a] = false;
                else
                {
                    edgeFall[a] = true;
                    last = temp[index, a];
                }
            }
            bool flip = true;
            int edgePos = 0;
            for (; edgePos < daySlots; ++edgePos)
            {
                if (edgeFall[edgePos])
                {
                    returny += (edgePos / 4).ToString() + ":" + ((edgePos % 4) * 15).ToString();
                    if (edgePos % 4 == 0)
                        returny += "0";
                    if (flip)
                    {
                        returny += "-";
                        flip = false;
                    }
                    else
                    {
                        returny += ";";
                        flip = true;
                    }

                }
            }
            if (edgePos == daySlots && !flip)
            {
                returny += (edgePos / 4).ToString() + ":" + ((edgePos % 4) * 15).ToString() + ";";
            }
            return returny;
        }

        public MainWindow()
        {
            InitializeComponent();
            leftAffinityBox.SelectionMode = SelectionMode.Single;
            rightAffinityBox.SelectionMode = SelectionMode.Single;
            HashSet<string> therapyTypes = new HashSet<string>();
            try
            {
                using (System.IO.StreamReader input = new System.IO.StreamReader("Schools.txt"))
                {
                    while (!input.EndOfStream)
                    {
                        School toAdd = new School();
                        int Bext;
                        toAdd.name = input.ReadLine();
                        toAdd.times = new bool[weekLength, daySlots];
                        toAdd.attending = new List<Student>();
                        for (int a = 0; a < weekLength; ++a)
                            parseTime(toAdd.times, a, input.ReadLine());
                        Bext = input.Peek();
                        while (!(Bext == 13 || Bext == 10))
                        {
                            Student next = new Student();
                            next.name = input.ReadLine();
                            next.therapyType = input.ReadLine();
                            therapyTypes.Add(next.therapyType);
                            next.slotsPerSession = int.Parse(input.ReadLine());
                            next.blocks = int.Parse(input.ReadLine());
                            next.times = new bool[weekLength, daySlots];
                            for (int a = 0; a < weekLength; ++a)
                                parseTime(next.times, a, input.ReadLine());
                            toAdd.attending.Add(next);
                            Bext = input.Peek();
                            makeStudentGrid(toAdd, next);
                        }
                        input.ReadLine();
                        Bext = input.Peek();
                        while (!(Bext == 13 || Bext == 10 || input.EndOfStream))
                        {
                            string left = input.ReadLine();
                            string right = input.ReadLine();

                            int leftpos = -1, rightpos = -1;

                            for (int searcher = 0; searcher < toAdd.attending.Count; ++searcher)
                            {
                                if (toAdd.attending[searcher].name.Equals(left))
                                    leftpos = searcher;
                                if (toAdd.attending[searcher].name.Equals(right))
                                    rightpos = searcher;

                            }


                            string toParse = input.ReadLine();
                            toAdd.affinities[new StudentPair(toAdd.attending[leftpos], toAdd.attending[rightpos])] = int.Parse(toParse);

                            Bext = input.Peek();

                        }
                        schools.Add(toAdd);
                        makeSchoolGrid(toAdd);
                        input.ReadLine();
                    }
                }
                foreach (string bub in therapyTypes)
                {
                    Button tehAdd = new Button();
                    tehAdd.Content = bub;
                    tehAdd.Click += (object senda, System.Windows.RoutedEventArgs a) =>
                    {
                        currentStudent.therapyType = (string)tehAdd.Content;
                        typeBox.Text = currentStudent.therapyType;
                    };
                    typeBox.Items.Add(tehAdd);
                }
            }
            catch (System.IO.FileNotFoundException)
            { }
        }

        public class Student //could be a struct, but most uses of this data should use pointers to save on memory.
        {
            public string name;
            public bool[,] times;
            public string therapyType;
            public int slotsPerSession;
            public int blocks;
            public override string ToString()
            {
                return name;
            }
        }

        public class School
        {
            public string name;
            public List<Student> attending;
            public bool[,] times = new bool[weekLength, daySlots];
            public Grid studentViewGrid = new Grid();
            public Dictionary<StudentPair, int> affinities = new Dictionary<StudentPair, int>();
        }

        public struct StudentPair//Sole use of this is to allow students to be placed into the dictionary in a strict order.  It has to be a struct to ensure that.
        {
            public Student a;
            public Student b;
            public StudentPair(Student left, Student right)
            {
                if (left.name.CompareTo(right.name) < 0)
                {
                    a = left;
                    b = right;
                }
                else
                {
                    a = right;
                    b = left;
                }
            }
        }

        public class StudentPairWrapper
        {
            private Student left;
            private Student right;
            public StudentPair current = new StudentPair();

            public void modifyLeft(Student toPutIn)
            {

                left = toPutIn;
                if (right != null)
                    current = new StudentPair(left, right);
            }

            public void modifyRight(Student toPutIn)
            {
                right = toPutIn;
                if (left != null)
                    current = new StudentPair(left, right);
            }
        }

        private void activateScheduleSave(object sender, RoutedEventArgs e)
        {
            using (System.IO.StreamWriter output = new System.IO.StreamWriter("schedule.txt"))
            {
                output.Write(((Label)(((Grid)viewingArea.Content).Children[0])).Content.ToString());
            }
            MessageBox.Show("Saved to " + System.Environment.CurrentDirectory + "\\schedule.txt");
        }

        private void schoolAdd(object sender, RoutedEventArgs e)
        {
            currentSchool = new School();
            currentSchool.attending = new List<Student>();
            startGrid.Visibility = Visibility.Hidden;
            startGrid.IsEnabled = false;
            schoolTimesGrid.Visibility = Visibility.Visible;
            schoolTimesGrid.IsEnabled = true;
            viewingArea.Content = currentSchool.studentViewGrid;
            schoolReturner = backToStartAdd;
        }

        private void activateSave(object sender, RoutedEventArgs e)
        {
            using (System.IO.StreamWriter output = new System.IO.StreamWriter("Schools.txt"))
            {
                foreach (School toPrint in schools)
                {
                    output.WriteLine(toPrint.name);
                    for (int a = 0; a < weekLength; ++a)
                        output.WriteLine(boolToTimeString(toPrint.times, a));
                    foreach (Student also in toPrint.attending)
                    {
                        output.WriteLine(also.name);
                        output.WriteLine(also.therapyType);
                        output.WriteLine(also.slotsPerSession.ToString());
                        output.WriteLine(also.blocks.ToString());
                        for (int a = 0; a < weekLength; ++a)
                            output.WriteLine(boolToTimeString(also.times, a));
                    }
                    output.WriteLine();
                    foreach (StudentPair pair in toPrint.affinities.Keys)
                    {
                        output.WriteLine(pair.a);
                        output.WriteLine(pair.b);
                        output.WriteLine(toPrint.affinities[pair].ToString());
                    }
                    output.WriteLine();
                }
                output.Flush();
            }
            MessageBox.Show("Data Saved!");
        }

        void readIn()
        {
            using (System.IO.StreamReader input = new System.IO.StreamReader("Schools.txt"))
            {
                while (!input.EndOfStream)
                {
                    School toAdd = new School();
                    toAdd.name = input.ReadLine();
                    toAdd.times = new bool[weekLength, daySlots];
                    for (int a = 0; a < weekLength; ++a)
                        parseTime(toAdd.times, a, input.ReadLine());
                    while (input.Peek() != '\n')
                    {
                        Student next = new Student();
                        next.name = input.ReadLine();
                        next.therapyType = input.ReadLine();
                        next.times = new bool[weekLength, daySlots];
                        for (int a = 0; a < weekLength; ++a)
                            parseTime(next.times, a, input.ReadLine());
                    }
                    input.ReadLine();
                    while (input.Peek() != '\n')
                    {
                        string left = input.ReadLine();
                        string right = input.ReadLine();
                        int leftpos = -1, rightpos = -1;
                        for (int searcher = 0; searcher < toAdd.attending.Count; ++searcher)
                        {
                            if (toAdd.attending[searcher].name == left)
                                leftpos = searcher;
                            if (toAdd.attending[searcher].name == right)
                                rightpos = searcher;
                        }
                        toAdd.affinities[new StudentPair(toAdd.attending[leftpos], toAdd.attending[rightpos])] = int.Parse(input.ReadLine());
                    }
                    input.ReadLine();
                }
            }
            /*
            using(System.IO.StreamReader input =new System.IO.StreamReader("Affinities.txt"))
            {
                while(!input.EndOfStream)
                {
                    StudentPair nup = new StudentPair();
                    
                }
            }*/
        }

        private RoutedEventHandler schoolEditFactory(School input, Label toRemix)
        {
            return (object sender, RoutedEventArgs e) =>
            {
                schoolNameBox.Text = input.name;
                foreach (Student a in input.attending)
                {
                    leftAffinityBox.Items.Add(a);
                    rightAffinityBox.Items.Add(a);
                }
                currentSchool = input;
                schoolTimesGrid.Visibility = Visibility.Visible;
                schoolTimesGrid.IsEnabled = true;
                startGrid.Visibility = Visibility.Hidden;
                startGrid.IsEnabled = false;
                schoolReturner = backToStartEdit;
                currentSchoolLabel = toRemix;
                viewingArea.Content = currentSchool.studentViewGrid;
                Monday.Text = boolToTimeString(currentSchool.times, 0);
                Tuesday.Text = boolToTimeString(currentSchool.times, 1);
                Wednesday.Text = boolToTimeString(currentSchool.times, 2);
                Thursday.Text = boolToTimeString(currentSchool.times, 3);
                Friday.Text = boolToTimeString(currentSchool.times, 4);
            };
        }

        private RoutedEventHandler schoolRemoveFactory(School toRemove, Grid toKill)
        {
            return (object sender, RoutedEventArgs e) =>
            {
                schools.Remove(toRemove);
                schoolViewGrid.Children.Remove(toKill);
            };
        }
        private void parseTime(bool[,] arr, int day, string input)
        {
            System.Console.WriteLine(input);
            string[] sup = input.Split(';');
            if (sup.Length < 1)
                return;
            int section = 0;
            while (section < sup.Length)
            {
                while (section < sup.Length && sup[section] == "")
                    ++section;
                if (sup.Length <= section)
                    break;
                String[] subs = sup[section].Split('-');
                int begin = getTime(subs[0], a => leastInteger(a));
                int end = getTime(subs[1], a => a / 15);
                for (int setter = begin; setter < end; ++setter)
                    arr[day, setter] = true;

                ++section;
            }
            System.Console.WriteLine(input);
            for (int a = 0; a < daySlots; ++a)
            {
                if (arr[day, a])
                    System.Console.Write("1");
                else
                    System.Console.Write("0");
            }
            
        }

        private bool[,] parseTimes(string m, string tu, string w, string th, string f)
        {
            bool[,] returny = new bool[weekLength, daySlots];
            parseTime(returny, 0, m);
            parseTime(returny, 1, tu);
            parseTime(returny, 2, w);
            parseTime(returny, 3, th);
            parseTime(returny, 4, f);
            return returny;
        }

        private void backToStartAdd(object sender, RoutedEventArgs e)
        {
            currentSchool.name = schoolNameBox.Text;
            schoolNameBox.Text = String.Empty;
            currentSchool.times = parseTimes(Monday.Text, Tuesday.Text, Wednesday.Text, Thursday.Text, Friday.Text);
            schools.Add(currentSchool);
            makeSchoolGrid(currentSchool);
        }

        private void makeSchoolGrid(School toAdd)
        {

            Grid newOne = new Grid();
            newOne.RowDefinitions.Add(new RowDefinition());
            newOne.ColumnDefinitions.Add(new ColumnDefinition());
            newOne.ColumnDefinitions.Add(new ColumnDefinition());
            newOne.ColumnDefinitions.Add(new ColumnDefinition());
            Label label = new Label();
            label.Content = toAdd.name;
            Grid.SetColumn(label, 0);
            Grid.SetRow(label, 0);
            newOne.Children.Add(label);
            Button editor = new Button();
            editor.Content = "Edit";
            editor.Click += schoolEditFactory(toAdd, label);
            Grid.SetColumn(editor, 1);
            Grid.SetRow(editor, 0);
            newOne.Children.Add(editor);
            Button remove = new Button();
            remove.Content = "Remove";
            remove.Click += schoolRemoveFactory(toAdd, newOne);
            newOne.Children.Add(remove);
            Grid.SetRow(remove, 0);
            Grid.SetColumn(remove, 2);
            Grid.SetRow(newOne, schoolViewGrid.Children.Count);
            schoolViewGrid.RowDefinitions.Add(new RowDefinition());
            schoolViewGrid.Children.Add(newOne);
            schoolTimesGrid.Visibility = Visibility.Hidden;
            schoolTimesGrid.IsEnabled = false;
            startGrid.IsEnabled = true;
            startGrid.Visibility = Visibility.Visible;
            viewingArea.Content = schoolViewGrid;
        }

        private void backToStartEdit(object sender, RoutedEventArgs e)
        {
            currentSchool.name = schoolNameBox.Text;
            currentSchool.times = parseTimes(Monday.Text, Tuesday.Text, Wednesday.Text, Thursday.Text, Friday.Text);
            schoolTimesGrid.Visibility = Visibility.Hidden;
            schoolTimesGrid.IsEnabled = false;
            startGrid.IsEnabled = true;
            startGrid.Visibility = Visibility.Visible;
            viewingArea.Content = schoolViewGrid;
            currentSchoolLabel.Content = currentSchool.name;

        }

        private void backToStart(object sender, RoutedEventArgs e)
        {
            schoolReturner(sender, e);
            for (int a = leftAffinityBox.Items.Count - 1; a > -1; --a)
            {
                leftAffinityBox.Items.RemoveAt(a);
                rightAffinityBox.Items.RemoveAt(a);
            }
        }

        private static int costCalc(Student[, ,] toCalc, School[,] schoolCalc)
        {
            int totalCost = 0;
            //let's hope this does not throw the value out of range.
            foreach (School brush in schools)
            {
                foreach (Student paint in brush.attending)
                    totalCost += max*paint.blocks;
            }
            for (int a = 0; a < weekLength; ++a)
            {
                HashSet<Student> lastSet = null;
                for (int b = 0; b < daySlots; ++b)
                {
                    if (schoolCalc[a, b] != null)
                    {
                        HashSet<Student> currentSet = new HashSet<Student>();
                        int nulls = 0;

                        for (int c = 0; c < slots; ++c)
                        {
                            //The hashset has a problem with nulls, so this prevents nulls from getting in.
                            if (toCalc[a, b, c] == null)
                                ++nulls;
                            else
                                currentSet.Add(toCalc[a, b, c]);
                        }
                        foreach (Student afd in currentSet)
                        {
                            //surely there has to be a better way to find the longest run of a student, but this is the best I could think of.
                            if (lastSet != null && !lastSet.Contains(afd))//It makes sure runs aren't counted twice.
                            {
                                int subpos = b;
                                int finds = 0;
                                bool found;
                                do
                                {
                                    found = false;
                                    for (int c = 0; !found && c < slots; ++c)
                                    {
                                        if (afd == toCalc[a, subpos, c])
                                        {
                                            found = true;
                                            ++finds;
                                        }
                                    }
                                    ++subpos;
                                } while (found&&subpos<daySlots);
                                //divide finds by the number of slots per session to get the number of sessions this run composes.  It's formatted like this so that it truncates the sessions to an integer.
                                int slotsTaken=(finds / afd.slotsPerSession);
                                if(afd.name=="Liberty Prime")
                                    System.Console.WriteLine("liberty Prime slots= "+slotsTaken.ToString());

                                totalCost-=slotsTaken*max;

                            }
                            //factor in affinities.
                            foreach (Student ase in currentSet)
                            {
                                StudentPair key = new StudentPair(afd, ase);
                                if (schoolCalc[a, b].affinities.Keys.Contains(key))//Let's hope the optimizer realizes the key for the affinities has to be found within its own hash set
                                    totalCost -= schoolCalc[a, b].affinities[key];
                                if (afd.therapyType != ase.therapyType)
                                    totalCost += diffTherapyCost;

                            }
                            if (!afd.times[a, b])
                                totalCost += max;
                        }
                        //check to see if a school is available at the time it is scheduled here.
                        if (!schoolCalc[a, b].times[a, b])
                            totalCost += max;
                        //This makes sure that a change in schools has at least a half an hour between them.  Hardcoded in for now.  In the future it should be possible to adjust the time between.
                        if ((b >= 1 && schoolCalc[a, b - 1] != null && schoolCalc[a, b - 1] != schoolCalc[a, b]) || (b >= 2 && schoolCalc[a, b - 2] != null && schoolCalc[a, b - 2] != schoolCalc[a, b]))
                            totalCost += max;
                        //calculate all the conflicts by having the same student in the same timeslot.  It works by removing the unique elements from the slots and excluding nulls
                        totalCost += (slots - currentSet.Count - nulls) * max;
                        lastSet = currentSet;
                    }
                    else
                        lastSet = null;
                }

            }

            return totalCost;
        }

        private void activateSchedule(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Currently does nothing.");
            startGrid.Visibility = Visibility.Hidden;
            scheduleSaveGrid.Visibility = Visibility.Visible;
            startGrid.IsEnabled = false;
            scheduleSaveGrid.IsEnabled = true;
            Student[, ,] schedule = new Student[weekLength, daySlots, slots];
            School[,] schoolBlock = new School[weekLength, daySlots];
            Student[, ,] scheduleCopy;
            School[,] schoolBlockCopy;
            int daypos = 0;
            int weekpos = 0;
            int slot = 0;
            schoolBlock[0, 0] = schools[0];
            foreach (School ack in schools)
            {
                foreach (Student otto in ack.attending)
                {
                    int max = otto.blocks * otto.slotsPerSession;
                    for (int copier = 0; copier < max; ++copier)
                    {
                        schedule[weekpos, daypos, slot] = otto;
                        schoolBlock[weekpos, daypos] = ack;

                        if (slot < slots - 1)
                            ++slot;
                        else
                        {
                            slot = 0;

                            if (daypos < daySlots - 1)
                            {
                                ++daypos;

                            }
                            else
                            {

                                daypos = 0;
                                if (weekpos < weekLength - 1)
                                    ++weekpos;
                                else
                                    MessageBox.Show("There are not enough slots to put students in.");
                            }
                        }
                    }
                }
                if (daypos < daySlots - 1)
                {
                    ++daypos;

                }
                else
                {
                    daypos = 0;
                    if (weekpos < weekLength - 1)
                        ++weekpos;
                    else
                        MessageBox.Show("There are not enough slots to put students in.");

                }
            }
            schoolBlockCopy = (School[,])schoolBlock.Clone();
            scheduleCopy = (Student[, ,])schedule.Clone();
            int oldCost = costCalc(schedule, schoolBlock);
            int newCost;
            double sup = Math.Log(oldCost, 2);
            int prob = (int)sup;
            double lastCost = oldCost;
            int ite = 0;
            while (prob > 0)
            {
                System.Console.WriteLine("proceeding...");
                if (oldCost < lastCost)
                {
                    prob = (int)(sup * Math.Pow(1023.0 / 1024, ite) * oldCost / lastCost);
                    schoolBlockCopy = (School[,])schoolBlock.Clone();
                    scheduleCopy = (Student[, ,])schedule.Clone();
                    System.Console.WriteLine(oldCost);
                    ++ite;
                }
                else
                {
                    oldCost = (int)lastCost;
                    schoolBlock = schoolBlockCopy;
                    schedule = scheduleCopy;
                }
                for (int iter = 0; iter < 1000; ++iter)
                {
                    int swapADay, swapBDay, swapAWeek, swapBWeek, swapASlot, swapBSlot;
                    //I have a preference for having the continue part of a loop be a true bool.
                    bool didNotGo = true;
                    while (didNotGo)
                    {
                        bool Anull, Bnull;

                        swapADay = gigi.Next(daySlots);
                        swapBDay = gigi.Next(daySlots);
                        swapAWeek = gigi.Next(weekLength);
                        swapBWeek = gigi.Next(weekLength);
                        swapASlot = gigi.Next(slots);
                        swapBSlot = gigi.Next(slots);
                        Anull = schedule[swapAWeek, swapADay, swapASlot] != null;
                        Bnull = schedule[swapBWeek, swapBDay, swapBSlot] != null;
                        if (Anull != Bnull)
                        {
                            //Just allow the program to put it in if one of the schools is null but not the other.
                            if (((schoolBlock[swapAWeek, swapADay] == null) != (schoolBlock[swapBWeek, swapBDay] == null)))
                            {
                                //I should rewrite this, because a seven parameter function and one that's only used here is unacceptable.
                                if (Anull)
                                    nullSwap(schedule, schoolBlock, swapADay, swapBDay, swapAWeek, swapBWeek, swapASlot, swapBSlot, ref  oldCost, prob);
                                else
                                    nullSwap(schedule, schoolBlock, swapBDay, swapADay, swapBWeek, swapAWeek, swapBSlot, swapASlot, ref oldCost, prob);
                                didNotGo = false;
                            }
                            else if (schoolBlock[swapAWeek, swapADay] == schoolBlock[swapBWeek, swapBDay])//if both of them are at the same school then the swap is allowed.
                            {
                                
                                if (Anull)
                                {
                                    schedule[swapBWeek, swapBDay, swapBSlot] = schedule[swapAWeek, swapADay, swapASlot];
                                    schedule[swapAWeek, swapADay, swapASlot] = null;
                                    newCost = costCalc(schedule, schoolBlock);
                                    if (newCost > oldCost && rejectAnyway(oldCost, newCost, prob))
                                    {
                                        schedule[swapAWeek, swapADay, swapASlot] = schedule[swapBWeek, swapBDay, swapBSlot];
                                        schedule[swapBWeek, swapBDay, swapBSlot] = null;
                                    }
                                    else
                                        oldCost = newCost;
                                    didNotGo = false;
                                }
                                else
                                {
                                    schedule[swapAWeek, swapADay, swapASlot] = schedule[swapBWeek, swapBDay, swapBSlot];
                                    schedule[swapBWeek, swapBDay, swapBSlot] = null;
                                    newCost = costCalc(schedule, schoolBlock);
                                    if (newCost > oldCost && rejectAnyway(oldCost, newCost, prob))
                                    {
                                        schedule[swapBWeek, swapBDay, swapBSlot] = schedule[swapAWeek, swapADay, swapASlot];
                                        schedule[swapAWeek, swapADay, swapASlot] = null;
                                    }
                                    else
                                        oldCost = newCost;
                                    didNotGo = false;
                                }
                            }

                        }
                        else if (Anull && Bnull && schoolBlock[swapAWeek, swapADay] == schoolBlock[swapBWeek, swapBDay])
                        {
                            Student temp = schedule[swapBWeek, swapBDay, swapBSlot];
                            schedule[swapBWeek, swapBDay, swapBSlot] = schedule[swapAWeek, swapADay, swapASlot];
                            schedule[swapAWeek, swapADay, swapASlot] = temp;

                            newCost = costCalc(schedule, schoolBlock);
                            if (newCost > oldCost && rejectAnyway(oldCost, newCost, prob))
                            {
                                temp = schedule[swapBWeek, swapBDay, swapBSlot];
                                schedule[swapBWeek, swapBDay, swapBSlot] = schedule[swapAWeek, swapADay, swapASlot];
                                schedule[swapAWeek, swapADay, swapASlot] = temp;
                            }
                            else
                            {
                                oldCost = newCost;
                                didNotGo = false;
                            }
                        }

                    }
                }
            }
            string toFill = "";
            for (int a = 0; a < daySlots; ++a)
            {
                string toAdd = "";
                for (int b = 0; b < weekLength; ++b)
                {
                    string newOne;
                    if (schoolBlock[b, a] == null)
                        newOne = "  (";
                    else
                        newOne = (char)schoolBlock[b, a].name[0]+"" + (char)schoolBlock[b, a].name[1] + "(";

                    for (int c = 0; c < slots; ++c)
                    {

                        if (schedule[b, a, c] != null)
                        {
                            char[] place = new char[14];
                            for (int s = 0; s < schedule[b, a, c].name.Length; ++s)
                                place[s] = schedule[b, a, c].name[s];
                            for (int s = schedule[b, a, c].name.Length; s < 14; ++s)
                                place[s] = ' ';
                            newOne += new string(place);
                        }
                        else
                            newOne += new string(' ', 14);

                    }
                    newOne = newOne + ")";
                    toAdd = toAdd + newOne;
                }
                toFill = toFill + toAdd + "\n";
            }
            Label con = new Label();
            Grid due = new Grid();
            con.Content = toFill;
            due.Children.Add(con);
            con.FontFamily = new FontFamily("Consolas");
            con.FontSize = 10;
            viewingArea.Content = due;
        }
        static int count = 0;
        private static bool rejectAnyway(int oldCost, int newCost, int prob)
        {
            //Jeez getting the Maxwell-Boltzmann distribution formula right is painful despite its simplicity.
            double delta = (newCost - oldCost);
            double acceptance = -1 * prob * Math.Log(gigi.NextDouble());
            if (count == 1000)
            {
                //System.Console.WriteLine(delta.ToString() + " " + acceptance.ToString());
                // System.Console.WriteLine(delta < acceptance);
                count = 0;
            }
            else
                ++count;
            return delta > acceptance;
        }

        private static void nullSwap(Student[, ,] schedule, School[,] schoolBlock, int swapADay, int swapBDay, int swapAWeek, int swapBWeek, int swapASlot, int swapBSlot, ref int curCost, int prob)
        {
            int search = 0;
            schedule[swapBWeek, swapBDay, swapBSlot] = schedule[swapAWeek, swapADay, swapASlot];
            schoolBlock[swapBWeek, swapBDay] = schoolBlock[swapAWeek, swapADay];
            schedule[swapAWeek, swapADay, swapASlot] = null;
            for (; search < slots; ++search)
            {
                if (schedule[swapAWeek, swapADay, search] != null)
                    break;
            }
            if (search == slots)
                schoolBlock[swapAWeek, swapADay] = null;
            int otherCost = costCalc(schedule, schoolBlock);
            if (otherCost > curCost && rejectAnyway(curCost, otherCost, prob))
            {
                schedule[swapAWeek, swapADay, swapASlot] = schedule[swapBWeek, swapBDay, swapBSlot];
                schoolBlock[swapAWeek, swapADay] = schoolBlock[swapBWeek, swapBDay];
                schedule[swapBWeek, swapBDay, swapBSlot] = null;
                schoolBlock[swapBWeek, swapBDay] = null;
            }
            else
                curCost = otherCost;
        }

        private void addType(object sender, RoutedEventArgs e)
        {
            TextBox addBox = new TextBox();
            addBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            addBox.Width = 100;
            typeBox.Items.Add(addBox);
            addBox.KeyDown += (object senduh, System.Windows.Input.KeyEventArgs b) =>
            {
                if (b.Key == System.Windows.Input.Key.Enter)
                {
                    Button toAdd = new Button();
                    toAdd.Content = addBox.Text;
                    toAdd.Click += (object senda, System.Windows.RoutedEventArgs a) =>
                    {
                        currentStudent.therapyType = (string)toAdd.Content;
                        typeBox.Text = currentStudent.therapyType;
                    };
                    typeBox.Items[typeBox.Items.IndexOf(addBox)] = toAdd;
                    typeBox.SelectedValue = null;
                }
            };
        }

        private RoutedEventHandler studentEditFactory(Student input, Label toRemix)
        {
            return (object sender, RoutedEventArgs e) =>
                {
                    nameBox.Text = input.name;
                    currentStudent = input;
                    mainGrid.Visibility = Visibility.Hidden;
                    mainGrid.IsEnabled = false;
                    studentAdder.Visibility = Visibility.Visible;
                    studentAdder.IsEnabled = true;
                    studentReturner = studentReturnEdit;
                    currentStudentLabel = toRemix;
                    Mo.Text = boolToTimeString(currentStudent.times, 0);
                    Tu.Text = boolToTimeString(currentStudent.times, 1);
                    We.Text = boolToTimeString(currentStudent.times, 2);
                    Th.Text = boolToTimeString(currentStudent.times, 3);
                    Fr.Text = boolToTimeString(currentStudent.times, 4);
                    typeBox.Text = input.therapyType;
                    blockBox.Text = input.blocks.ToString();
                    lengthTimeBox.Text = input.slotsPerSession.ToString();
                };
        }

        private RoutedEventHandler removeFactory(Student toRemove, Grid toKill)
        {
            return (object sender, RoutedEventArgs e) =>
                {
                    currentSchool.attending.Remove(toRemove);
                    currentSchool.studentViewGrid.Children.Remove(toKill);
                    //AffineBox.Items.Remove(toSlaughter);
                    currentSchool.studentViewGrid.UpdateLayout();
                };
        }


        private void studentReturnAdd(object sender, RoutedEventArgs e)
        {
            currentStudent.name = nameBox.Text;
            if (int.TryParse(blockBox.Text, out currentStudent.blocks) && int.TryParse(lengthTimeBox.Text, out currentStudent.slotsPerSession))
            {
                nameBox.Text = new String(' ', 1);
                studentAdder.Visibility = Visibility.Hidden;
                studentAdder.IsEnabled = false;
                mainGrid.Visibility = Visibility.Visible;
                mainGrid.IsEnabled = true;
                currentSchool.attending.Add(currentStudent);
                makeStudentGrid(currentSchool, currentStudent);
            }
            else
                MessageBox.Show("Enter a number for how many slots the student needs.");
        }

        private void makeStudentGrid(School toMod, Student toAdd)
        {
            Grid newOne = new Grid();
            newOne.ColumnDefinitions.Add(new ColumnDefinition());
            newOne.ColumnDefinitions.Add(new ColumnDefinition());
            newOne.ColumnDefinitions.Add(new ColumnDefinition());
            newOne.RowDefinitions.Add(new RowDefinition());
            Label newLabel = new Label();
            newLabel.Content = toAdd.name;
            Grid.SetColumn(newLabel, 0);
            Grid.SetRow(newLabel, 0);
            newOne.Children.Add(newLabel);
            Button newEdit = new Button();
            newEdit.Content = "Edit";
            newEdit.Click += studentEditFactory(toAdd, newLabel);

            Grid.SetColumn(newEdit, 1);
            Grid.SetRow(newEdit, 0);
            newOne.Children.Add(newEdit);
            Button newRemove = new Button();
            newRemove.Content = "Remove";
            newRemove.Click += removeFactory(toAdd, newOne);
            Grid.SetColumn(newRemove, 2);
            Grid.SetRow(newRemove, 0);
            newOne.Children.Add(newRemove);
            newOne.Height = 100;
            newOne.HorizontalAlignment = HorizontalAlignment.Stretch;
            toMod.studentViewGrid.RowDefinitions.Add(new RowDefinition());
            Grid.SetRow(newOne, toMod.studentViewGrid.Children.Count);
            toMod.studentViewGrid.Children.Add(newOne);
        }

        private void studentReturnEdit(object sender, RoutedEventArgs e)
        {
            currentStudent.name = nameBox.Text;
            nameBox.Text = new String(' ', 1);
            studentAdder.Visibility = Visibility.Hidden;
            studentAdder.IsEnabled = false;
            mainGrid.Visibility = Visibility.Visible;
            mainGrid.IsEnabled = true;
            currentStudentLabel.Content = currentStudent.name;
        }
        private void studentReturn(object sender, RoutedEventArgs e)
        {
            studentReturner(sender, e);
            currentStudent.times = parseTimes(Mo.Text, Tu.Text, We.Text, Th.Text, Fr.Text);
        }

        private void studentAdd(object sender, RoutedEventArgs e)
        {
            mainGrid.Visibility = Visibility.Hidden;
            mainGrid.IsEnabled = false;
            studentAdder.Visibility = Visibility.Visible;
            studentAdder.IsEnabled = true;
            lengthTimeBox.Text = "This is how many slots of fifteen minutes the student needs per session";
            blockBox.Text = "This is how many blocks of time the student needs.";
            Mo.Text = "";
            Tu.Text = "";
            We.Text = "";
            Th.Text = "";
            Fr.Text = "";
            currentStudent = new Student();
            studentReturner = studentReturnAdd;
        }

        private void affinityEditor(object sender, RoutedEventArgs e)
        {
            AffinityEditor.Visibility = Visibility.Visible;
            mainGrid.Visibility = Visibility.Hidden;
        }

        StudentPairWrapper toEdit = new StudentPairWrapper();

        private void leftSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                toEdit.modifyLeft((Student)e.AddedItems[0]);
                affinityAmountBox.Text = currentSchool.affinities[toEdit.current].ToString();
            }
            catch (KeyNotFoundException)
            {
                affinityAmountBox.Text = "0";
            }
            catch (IndexOutOfRangeException)
            { }
        }

        private void rightSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                toEdit.modifyRight((Student)e.AddedItems[0]);
                affinityAmountBox.Text = currentSchool.affinities[toEdit.current].ToString();
            }
            catch (KeyNotFoundException)
            {
                affinityAmountBox.Text = "0";
            }
            catch (IndexOutOfRangeException)
            { }
        }

        private void SetAffinity(object sender, RoutedEventArgs e)
        {
            currentSchool.affinities[toEdit.current] = int.Parse(affinityAmountBox.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AffinityEditor.Visibility = Visibility.Hidden;
            mainGrid.Visibility = Visibility.Visible;
        }

        private void Move(object sender, KeyEventArgs e)
        {
        }

        private void scheduleToStart(object sender, RoutedEventArgs e)
        {
            viewingArea.Content = schoolViewGrid;
            startGrid.Visibility = Visibility.Visible;
            scheduleSaveGrid.Visibility = Visibility.Hidden;
            startGrid.IsEnabled = true;
            scheduleSaveGrid.IsEnabled = false;
        }
    }
}