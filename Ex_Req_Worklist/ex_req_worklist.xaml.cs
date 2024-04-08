using LSExtensionWindowLib;
using LSSERVICEPROVIDERLib;
using Patholab_DAL_V1;
using Patholab_Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
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
using forms = System.Windows.Forms;
using System.Windows.Xps.Packaging;
using System.Configuration;
using System.Printing;
using System.Windows.Documents;
using System.Drawing.Printing;
using System.Reflection;
using System.Data;
using System.Text;



using Excel = Microsoft.Office.Interop.Excel;




namespace Ex_Req_Worklist
{
    /// <summary>
    /// Interaction logic for ex_req_worklist.xaml
    /// </summary>
    public partial class ex_req_worklist : UserControl
    {
        private DataLayer dal;
        private INautilusDBConnection ntlsCon;
        private List<ExtraRequestRow> listPart_Immono, listPart_Histochemistry, listPart_ExMaterial, listPart_Others;
        private ListView currentListView;
        private Dictionary<string, Tuple<ListView, List<ExtraRequestRow>>> dict;
        public bool debug;
        private System.Drawing.Printing.PrintDocument docToPrint =
    new System.Drawing.Printing.PrintDocument();

        //private List<string> dsgs = new List<string>();
        private DataTable dataTable = new DataTable();
        private bool start = true;
        private int itemGroup = 0;
        public int[] countArr = new int[4];
        public string[] tabHedears = new string[4];
        private bool Select = false;
        private System.Linq.IQueryable<Ex_Req_Worklist.ExtraRequestRow> list1,list2,list3;
        long sid = 1;

        public ex_req_worklist(INautilusDBConnection _ntlsCon, bool dbg)
        {
            InitializeComponent();

            this.ntlsCon = _ntlsCon;
            this.sid = (long)_ntlsCon.GetSessionId();
            this.debug = dbg;
            int i = 0;
            foreach (var tab in tabControl1.Items.OfType<TabItem>())
            {
                tabHedears[i] = (tab.Header as string).Trim();
                i++;
            }
            init();


        }

        private void init()
        {

            //Connect to DB
            dal = new DataLayer();
            if (debug)
            {
                //For running without Nautilus.
                dal.MockConnect();
            }
            else
            {
                dal.Connect(ntlsCon);
            }


            // This dict maps each PartType ("I" / "H" ......) to a Tuple containing the ListView and List associated with the partType
            dict = new Dictionary<string, Tuple<ListView, List<ExtraRequestRow>>>();

            listPart_Histochemistry = new List<ExtraRequestRow>();
            listPart_Immono = new List<ExtraRequestRow>();
            listPart_ExMaterial = new List<ExtraRequestRow>();
            listPart_Others = new List<ExtraRequestRow>();

            add2dic(listPart_Histochemistry, "H");
            add2dic(listPart_Immono, "I");
            add2dic(listPart_ExMaterial, "M");
            add2dic(listPart_Others, "O");

            SetListHI(/*listPart_Histochemistry, listPart_Immono*/);//2
            SetListOther();//3
            SetListExMaterial();//4

            int i = 0;
            foreach (var tab in tabControl1.Items.OfType<TabItem>())
            {
                var listview = tab.Content as ListView;
                tab.Header = tabHedears[i] + string.Format(" ({0})", countArr[i]);
                i++;
            }
            start = false;

        }



        #region LoadData
        private void SetListHI(/*List<ExtraRequestRow> listPart_Histochemistry, List<ExtraRequestRow> listPart_Immono*/)
        {
            if (!Select)
            {
                 list1 = (from req in dal.FindBy<EXTRA_SLIDES>(item => item.REQUEST_TYPE.Equals("I") || item.REQUEST_TYPE.Equals("H"))
                            select new ExtraRequestRow()
                            {
                                sdgId = req.SDG_ID,
                                SdgPatholabNumber = req.SDG_PATHOLAB_NUMBER,
                                Priority = req.PRIORITY,
                                RequestType = req.REQUEST_TYPE,
                                CreatedOn = req.CONTAINERRECEIVEDON,
                                BlockNumber = req.BLOCK_NUMBER,
                                SlideNumber = req.SLIDE_NUMBER,
                                ExRequestDetails = req.REQUEST_DETAILS,
                                PathologName = req.PATHOLOG_NAME,
                                ExRequestCreatedOn = req.REQUEST_CREATED_ON,
                                CuttingLaborant = req.CUTTING_LABORANT,
                                Remarks = req.REQUEST_REMARKS,
                                PathologMacro = req.PATHOLOG_MACRO,
                                PathologMacroTime = req.PATHOLOG_MACRO_TIME,
                                ExRequestId = req.REQ_ID,
                                ExRequestEntityType = req.REQUEST_ENTITY_TYPE,//block\silide\sample
                                ExRequestName = req.REQUEST_NAME,
                                ExRequestStatus = req.REQUEST_STATUS,
                                SamplePatholabName = req.SAMPLE_PATHOLAB_NUMBER,
                                AliquotPatholabName = req.ALIQUOT_PATHOLAB_NUMBER,
                                Group = null//getGroupItem(req)//req.SDG_PATHOLAB_NUMBER
                            });
            }


            listHi = list1.OrderBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.Priority).ToList();

            switch (itemGroup)
            {
                case 1:
                    listHi = list1.OrderBy(x => x.SdgPatholabNumber).ThenBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.Priority).ToList();
                    break;
                case 2:
                    listHi = list1.OrderByDescending(x => x.ExRequestCreatedOn.Value).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.Priority).ToList();
                    break;
            }

            getGroupItem(listHi);

            //נתנאל ביקש
            //אם לבלוק יש גם בקשת אימונו וגם היסטוכימיה אז שיופיע רק באימונוהיסטוכימיה
            //ואם יש לו רק בקשת היסטוכימיה אז צריך להופיע רק בהיסטוכימיה 
            //ואם יש לו רק בקשת אימונו אז צריך להופיע רק באימונוהיסטוכימיה

            //1st group by block number
            var groupbyBlockNumber = listHi.GroupBy(x => x.BlockNumber);

            foreach (var blockGrp in groupbyBlockNumber)
            {
                var NestedGrpbyType = blockGrp.GroupBy(x => x.RequestType);

                if (NestedGrpbyType.Count() > 1)
                {
                    //The block has 2 types of requests (H and I)
                    listPart_Immono.AddRange(blockGrp.ToList());

                }
                else
                //The block has only one type of request (H or I)
                {

                    foreach (var item in NestedGrpbyType)
                    {
                        if (item.Key == "H")
                        {
                            listPart_Histochemistry.AddRange(item.ToList());
                        }
                        else if (item.Key == "I")
                        {
                            listPart_Immono.AddRange(item.ToList());
                        }
                    }

                }

            }
            countArr[0] = listPart_Immono.Count;
            countArr[1] = listPart_Histochemistry.Count;
        }


        // Generating the appropriate list of the given partType.
        private void SetListOther()
        {
            try
            {
                if (!Select)
                {
                    list2 = (from req in dal.FindBy<EXTRA_SLIDES>(item => item.REQUEST_TYPE.Equals("O"))
                             //  select new ExtraRequestRow
                             select new ExtraRequestRow()
                             {
                                 sdgId = req.SDG_ID,
                                 SdgPatholabNumber = req.SDG_PATHOLAB_NUMBER,
                                 Priority = req.PRIORITY,
                                 RequestType = req.REQUEST_TYPE,
                                 CreatedOn = req.CONTAINERRECEIVEDON,
                                 BlockNumber = req.BLOCK_NUMBER,
                                 SlideNumber = req.SLIDE_NUMBER,
                                 ExRequestDetails = req.REQUEST_DETAILS,
                                 PathologName = req.PATHOLOG_NAME,
                                 ExRequestCreatedOn = req.REQUEST_CREATED_ON,
                                 CuttingLaborant = req.CUTTING_LABORANT,
                                 Remarks = req.REQUEST_REMARKS,
                                 PathologMacro = req.PATHOLOG_MACRO,
                                 PathologMacroTime = req.PATHOLOG_MACRO_TIME,
                                 ExRequestId = req.REQ_ID,
                                 ExRequestEntityType = req.REQUEST_ENTITY_TYPE,//block\silide\sample
                                 ExRequestName = req.REQUEST_NAME,
                                 ExRequestStatus = req.REQUEST_STATUS,
                                 SamplePatholabName = req.SAMPLE_PATHOLAB_NUMBER,
                                 AliquotPatholabName = req.ALIQUOT_PATHOLAB_NUMBER,
                                 Group = null//getGroupItem(req)//req.SDG_PATHOLAB_NUMBER
                             });
                }


                listOther = list2.OrderBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.SamplePatholabName)
                         .ThenBy(x => x.AliquotPatholabName).ThenBy(x => x.SlideNumber).ThenBy(x => x.Priority).ToList();

                switch (itemGroup)
                {
                    case 1:
                        listOther = list2.OrderBy(x => x.SdgPatholabNumber).ThenBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.SamplePatholabName)
                         .ThenBy(x => x.AliquotPatholabName).ThenBy(x => x.SlideNumber).ThenBy(x => x.Priority).ToList();
                        break;
                    case 2:
                        listOther = list2.OrderByDescending(x => x.ExRequestCreatedOn.Value).ThenBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.SamplePatholabName)
 .ThenBy(x => x.AliquotPatholabName).ThenBy(x => x.SlideNumber).ThenBy(x => x.Priority).ToList();
                        break;
                }

                getGroupItem(listOther);

                countArr[2] = listOther.Count();

                listPart_Others.AddRange(listOther);

            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void SetListExMaterial()
        {
            if (!Select)
            {
                list3 = (from em in dal.GetAll<EXTRA_MATERIAL>()
                         select new ExtraRequestRow()
                         {
                             sdgId = em.SDG_ID,
                             SdgPatholabNumber = em.SDG_PATHOLAB_NUMBER,
                             Priority = em.PRIORITY,
                             CreatedOn = em.CONTAINERRECEIVEDON,
                             BlockNumber = em.SAMPLE_NAME,
                             ExRequestDetails = em.U_REQUEST_DETAILS,
                             PathologName = em.PATHOLOG_NAME,
                             ExRequestCreatedOn = em.REQ_CREATED_ON,
                             CuttingLaborant = em.PATHOLOG_NAME,
                             Remarks = em.REQUEST_REMARKS,
                             PathologMacro = em.PATHOLOG_MACRO,
                             PathologMacroTime = em.PATHOLOG_MACRO_TIME,
                             ExRequestId = em.REQ_ID,
                             ExRequestEntityType = em.REQUEST_ENTITY_TYPE,
                             ExRequestStatus = em.REQUEST_STATUS,
                             SampleName = em.SAMPLE_NAME,
                             RequestType = em.REQUEST_TYPE,
                             Group = null

                         });
            }

            listExm = list3.OrderBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.Priority).ToList();
            switch (itemGroup)
            {
                case 1:
                    listExm = list3.OrderBy(x => x.SdgPatholabNumber).ThenBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.Priority).ToList();
                    break;
                case 2:
                    listExm = list3.OrderByDescending(x => x.ExRequestCreatedOn.Value).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.Priority).ToList();
                    break;
            }

            getGroupItem(listExm);

            countArr[3] = listExm.Count();

            listPart_ExMaterial.AddRange(listExm);
        }


        #endregion


        private void add2dic(List<ExtraRequestRow> list, string partType)
        {

            ListView listView = tabControl1.Items.OfType<TabItem>().Where(tab => tab.Tag.Equals(partType)).FirstOrDefault().Content as ListView;

            if (!dict.ContainsKey(partType))
            {
                dict.Add(partType, new Tuple<ListView, List<ExtraRequestRow>>(listView, list));
            }
            else
            {
                dict[partType] = new Tuple<ListView, List<ExtraRequestRow>>(listView, list);
            }
        }

        // Each tabItem contains a listview. This method sets the appropriate listview to the relevant data.
        // In addition, when switching from one tab to another, calculating the number of rows and displays it.

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tab = tabControl1.SelectedItem as TabItem;

            if (tab != null)
            {
                var tuple = dict[tab.Tag.ToString()];
                SetListViewSource(tuple.Item1, tuple.Item2);
                currentListView = tuple.Item1;

                var parentheses = tab.Header.ToString().IndexOf("(");
                if (parentheses != -1)
                {
                    tab.Header = tab.Header.ToString().Substring(0, parentheses) + string.Format("({0})", tuple.Item2.Count);
                }
                else
                {
                    tab.Header = tab.Header.ToString() + string.Format(" ({0})", tuple.Item2.Count);
                }
            }
            textBoxCloseRow.Focus();
        }

        private void SetListViewSource(ListView listView, List<ExtraRequestRow> list)
        {
            listView.ItemsSource = null;
            listView.Items.Clear();
            listView.ItemsSource = list;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
            if (view.GroupDescriptions.Count() == 0)
            {
                PropertyGroupDescription groupDescription = new PropertyGroupDescription("Group");
                view.GroupDescriptions.Add(groupDescription);
            }


        }


        // refreshing the listview such that updates will appear
        private void refreshPage()
        {
            init();

            tabControl1_SelectionChanged(null, null);
        }


        // this method expands the headers to fit the listview width full size.
        private void listView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                ListView lv = sender as ListView;
                double width = lv.ActualWidth;
                var columns = getCurrentGridViewColumns(lv);
                double widthPerColumn = width / (columns.Count > 0 ? columns.Count : 1);
                foreach (var header in columns)
                {
                    header.Width = widthPerColumn;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);

                return;
            }

        }

        private GridViewColumnCollection getCurrentGridViewColumns(ListView listView)
        {
            GridView grid = listView.View as GridView;

            if (grid != null)
            {
                return grid.Columns;
            }

            return null;
        }

        private void buttonCloseRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = forms.MessageBox.Show("האם להסיר את הסליידים שסומנו?", "הסרת סליידים", forms.MessageBoxButtons.YesNo, forms.MessageBoxIcon.Asterisk);

                if (result == forms.DialogResult.Yes)
                {
                    int countCloseRows = 0;
                    int countProcessRows = 0;
                    foreach (var selectedItem in currentListView.Items)
                    {
                        ExtraRequestRow slide = selectedItem as ExtraRequestRow;

                        if (slide != null && slide.ScannedByUser)
                        {
                            Logger.WriteLogFile("slide found : " + slide.SlideNumber);

                            if (slide.ExRequestStatus == "בתהליך")
                            {
                                Logger.WriteLogFile("slide in process : " + slide.SlideNumber);
                                countProcessRows++;
                            }
                            else
                            {
                                U_EXTRA_REQUEST_DATA_USER requestToColse =
                                    dal.FindBy<U_EXTRA_REQUEST_DATA_USER>
                                    (item => item.U_EXTRA_REQUEST_DATA_ID == slide.ExRequestId).FirstOrDefault();

                                if (requestToColse != null)
                                {
                                    Logger.WriteLogFile("slide found in data user : " + requestToColse.U_SLIDE_NAME);
                                    var exrd = dal.FindBy<U_EXTRA_REQUEST_DATA_USER>(x => x.U_EXTRA_REQUEST_DATA_ID == slide.ExRequestId).SingleOrDefault();
                                    Logger.WriteLogFile("slide in data user : " + exrd.U_SLIDE_NAME);
                                    exrd.U_STATUS = "P";//"X"
                                    dal.InsertToSdgLog(slide.sdgId, "EXTRA.STORAGE", sid, "מסך בקשות נוספות - הסרה מהרשימה");
                                    countCloseRows++;
                                    Logger.WriteLogFile("the update sucsess the status is : " + exrd.U_STATUS);

                                }
                                else
                                {
                                    Logger.WriteLogFile("slide not found in data user : " + requestToColse.U_SLIDE_NAME);
                                }
                            }

                        }
                    }

                    dal.SaveChanges();
                    refreshPage();

                    textBoxCloseRow.Text = string.Empty;
                    if (countProcessRows > 0)
                    {
                        MessageBox.Show(string.Format("!{0} {1} {2} {3}", "לא ניתן להסיר בקשות בתהליך", countProcessRows, countProcessRows > 1 ? "בקשות לא הוסרו " : "בקשה לא הוסרה ", "מהרשימה "), "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    if (countCloseRows > 0)
                    {
                        MessageBox.Show(string.Format("!{0} {1} {2} {3}", "התהליך הושלם", countCloseRows, countCloseRows > 1 ? "בקשות הוסרו " : "בקשה הוסרה ", "מהרשימה "), "", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);

                MessageBox.Show(ex.Message);
            }
        }

        private void textBoxCloseRow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                buttonSelectRow_Click(null, null);
            }
        }

        private void buttonSelectRow_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxCloseRow.Text))
            {
                //currentListView.SelectedItems.Add(currentListView.ItemsSource.OfType<PatientRow>().Where(item => item.BlockNumber != null && item.BlockNumber.Equals(textBoxCloseRow.Text)).FirstOrDefault());
                var req2Close = currentListView.ItemsSource.OfType<ExtraRequestRow>().Where
                    (item => (item.SlideNumber != null
                        && item.SlideNumber.Equals(textBoxCloseRow.Text)
                        && item.ExRequestEntityType == "Block")
                        ||
                        (item.SampleName != null &&
                        item.SampleName.Equals(textBoxCloseRow.Text)
                        && item.ExRequestEntityType == "Sample"));

                // .FirstOrDefault();


                if (req2Close.Count() < 1)
                {
                    MessageBox.Show("Request with the given aliquot name cannot be found.");

                }
                else if (req2Close.Count() > 1)
                {
                    MessageBox.Show("קיימת יותר מבקשה אחת לאותה יישות,רק ישות אחד תרד מהרשימה", "", MessageBoxButton.OK, MessageBoxImage.Warning);

                }
                else
                {
                    //foreach (var req in req2Close)
                    // {

                    req2Close.First().ScannedByUser = true;

                    //    currentListView.SelectedItems.Add(req);
                    //}
                }

                textBoxCloseRow.Text = string.Empty;
            }
        }

        #region Sort methods

        GridViewColumnHeader lastHeaderClicked = null;
        ListSortDirection lastDirection;
        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string sortBy = headerClicked.Tag.ToString();

                    Sort(sortBy, direction, currentListView);

                    lastHeaderClicked = headerClicked;
                    lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction, ListView listView)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        #endregion

        #region filter methods

        Dictionary<GridViewColumnHeader, string> dictFilter = new Dictionary<GridViewColumnHeader, string>();
        GridViewColumnHeader currentFilteredHeader;
        formFilter filterForm;
        CollectionView view;
        string txtFilter = string.Empty;
        bool hasFilter = false;

        private void GridViewColumnHeader_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            currentFilteredHeader = sender as GridViewColumnHeader;
            displayContextMenu();
        }

        private void displayContextMenu()
        {
            forms.ContextMenuStrip contexMenu = new forms.ContextMenuStrip();
            forms.ToolStripItem itemAddFiler;
            forms.ToolStripItem itemRemoveFilter;

            if (!hasFilter)
            {
                itemAddFiler = contexMenu.Items.Add("Filter");
                itemAddFiler.Click += new EventHandler(addFilter);
            }
            else
            {
                itemAddFiler = contexMenu.Items.Add("Add Another Filter");
                itemAddFiler.Click += new EventHandler(addFilter);
                itemRemoveFilter = contexMenu.Items.Add("Remove Filter");
                itemRemoveFilter.Click += new EventHandler(removeFilter);
            }

            contexMenu.Show(forms.Cursor.Position);
        }

        private void addFilter(object sender, EventArgs e)
        {
            using (filterForm = new formFilter(currentFilteredHeader.Content as string))
            {
                forms.DialogResult result = filterForm.ShowDialog();
                if (result == forms.DialogResult.OK)
                {
                    txtFilter = filterForm.filterSentence;

                    try
                    {
                        if (!dictFilter.ContainsKey(currentFilteredHeader))
                        {
                            dictFilter.Add(currentFilteredHeader, txtFilter);
                        }
                        else
                        {
                            dictFilter[currentFilteredHeader] = txtFilter;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLogFile(ex);

                    }

                    view = (CollectionView)CollectionViewSource.GetDefaultView(currentListView.ItemsSource);
                    view.Filter = UserFilter;
                    hasFilter = true;

                    var item = sender as forms.ToolStripItem;
                    if (item != null)
                    {
                        item.Click -= addFilter;
                    }
                }
            }
        }

        private void removeFilter(object sender, EventArgs e)
        {
            view = (CollectionView)CollectionViewSource.GetDefaultView(currentListView.ItemsSource);
            view.Filter = clearFilter;
            hasFilter = false;
            dictFilter.Clear();

            var item = sender as forms.ToolStripItem;
            if (item != null)
            {
                item.Click -= removeFilter;
            }
        }

        // this function will iterate on all rows of the listview and because it is returning true - all rows will be visible.
        private bool clearFilter(object item)
        {
            return true;
        }

        private bool UserFilter(object item)
        {
            ExtraRequestRow patient = item as ExtraRequestRow;
            try
            {
                bool res = true;
                foreach (var keyHeader in dictFilter.Keys)
                {
                    switch (keyHeader.Name.Substring(0, keyHeader.Name.Length - 1))
                    {

                        case "headerSdgPatholabNumber":
                            res &= patient.SdgPatholabNumber != null && patient.SdgPatholabNumber.IndexOf(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case "headerPriority":
                            res &= patient.Priority != null && patient.Priority.ToString().Equals(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase);
                            break;
                        case "headerCreatedOn":
                            CultureInfo ciAliquot = new CultureInfo("he-IL");
                            try
                            {
                                DateTime d1 = Convert.ToDateTime(patient.CreatedOn.Value);
                                DateTime d2 = Convert.ToDateTime(dictFilter[keyHeader], ciAliquot);

                                res &= d1.ToShortDateString().Equals(d2.ToShortDateString());
                            }
                            catch
                            {

                                res &= true;
                            }
                            break;
                        case "headerBlockNumber":
                            res &= patient.BlockNumber != null && patient.BlockNumber.Equals(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase);
                            break;
                        case "headerSlideNumber":
                            res &= patient.SlideNumber != null && patient.SlideNumber.Equals(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase);
                            break;
                        case "headerExRequestId":
                            res &= patient.ExRequestId != null && patient.ExRequestId.ToString().Equals(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase);
                            break;
                        case "headerExRequestDetails":
                            res &= patient.ExRequestDetails != null && patient.ExRequestDetails.IndexOf(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case "headerPathologName":
                            res &= patient.PathologName != null && patient.PathologName.IndexOf(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case "headerExRequestCreatedOn":
                            CultureInfo ciRequest = new CultureInfo("he-IL");
                            try
                            {
                                DateTime d1 = Convert.ToDateTime(patient.CreatedOn.Value);
                                DateTime d2 = Convert.ToDateTime(dictFilter[keyHeader], ciRequest);

                                res &= d1.ToShortDateString().Equals(d2.ToShortDateString());
                            }
                            catch
                            {
                                res &= true;
                            }
                            break;
                        case "headerCuttingLaborant":
                            res &= patient.CuttingLaborant != null && patient.CuttingLaborant.IndexOf(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case "headerRemarks":
                            res &= patient.Remarks != null && patient.Remarks.IndexOf(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case "headerPathologMacro":
                            res &= patient.PathologMacro != null && patient.PathologMacro.IndexOf(dictFilter[keyHeader], StringComparison.OrdinalIgnoreCase) >= 0;
                            break;
                        case "headerPathologMacroTime":
                            CultureInfo ciAliquot1 = new CultureInfo("he-IL");
                            try
                            {
                                DateTime d1 = Convert.ToDateTime(patient.PathologMacroTime.Value);
                                DateTime d2 = Convert.ToDateTime(dictFilter[keyHeader], ciAliquot1);

                                res &= d1.ToShortDateString().Equals(d2.ToShortDateString());
                            }
                            catch
                            {
                                res &= true;
                            }
                            break;
                        default:
                            res &= true;
                            break;
                    }

                    if (!res) return false;
                }

                return res;
            }
            catch (Exception ex)
            {
                Logger.WriteLogFile(ex);

                return true;
            }
        }

        //// this function will iterate on all rows of the listview and because it is returning true - all rows will be visible.
        //private bool clearFilter(object item)
        //{
        //    return true;
        //}

        //private bool UserFilter(object item)
        //{
        //    if (string.IsNullOrEmpty(txtFilter))
        //        return true;

        //    PatientRow patient = item as PatientRow;
        //    try
        //    {
        //        switch (currentFilteredHeader.Name.Substring(0, currentFilteredHeader.Name.Length - 1))
        //        {

        //            case "headerSdgPatholabNumber":
        //                return patient.SdgPatholabNumber != null && patient.SdgPatholabNumber.IndexOf(txtFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        //            case "headerPriority":
        //                //return patient.Priority != null && patient.Priority.ToString().Equals(txtFilter, StringComparison.OrdinalIgnoreCase);
        //                Predicate<PatientRow> pred = (toCheck) => toCheck.Priority != null && toCheck.Priority.ToString().Equals(txtFilter, StringComparison.OrdinalIgnoreCase);
        //                bool res = pred(patient);
        //                return res;
        //            case "headerCreatedOn":
        //                CultureInfo ciAliquot = new CultureInfo("he-IL");
        //                try
        //                {
        //                    DateTime d1 = Convert.ToDateTime(patient.CreatedOn.Value);
        //                    DateTime d2 = Convert.ToDateTime(txtFilter, ciAliquot);

        //                    return d1.ToShortDateString().Equals(d2.ToShortDateString());
        //                }
        //                catch
        //                {
        //                    return true;
        //                }
        //            case "headerBlockNumber":
        //                return patient.BlockNumber != null && patient.BlockNumber.Equals(txtFilter, StringComparison.OrdinalIgnoreCase);
        //            case "headerSlideNumber":
        //                return patient.SlideNumber != null && patient.SlideNumber.Equals(txtFilter, StringComparison.OrdinalIgnoreCase);
        //            case "headerExRequestId":
        //                return patient.ExRequestId != null && patient.ExRequestId.ToString().Equals(txtFilter, StringComparison.OrdinalIgnoreCase);
        //            case "headerExRequestDetails":
        //                return patient.ExRequestDetails != null && patient.ExRequestDetails.IndexOf(txtFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        //            case "headerPathologName":
        //                return patient.PathologName != null && patient.PathologName.IndexOf(txtFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        //            case "headerExRequestCreatedOn":
        //                CultureInfo ciRequest = new CultureInfo("he-IL");
        //                try
        //                {
        //                    DateTime d1 = Convert.ToDateTime(patient.CreatedOn.Value);
        //                    DateTime d2 = Convert.ToDateTime(txtFilter, ciRequest);

        //                    return d1.ToShortDateString().Equals(d2.ToShortDateString());
        //                }
        //                catch
        //                {
        //                    return true;
        //                }
        //            case "headerCuttingLaborant":
        //                return patient.CuttingLaborant != null && patient.CuttingLaborant.IndexOf(txtFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        //            case "headerSedimentationLaborant":
        //                return patient.SedimentationLaborant != null && patient.SedimentationLaborant.IndexOf(txtFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        //            case "headerRemarks":
        //                return patient.Remarks != null && patient.Remarks.IndexOf(txtFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        //            case "headerPathologMacro":
        //                return patient.PathologMacro != null && patient.PathologMacro.IndexOf(txtFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        //            case "headerPathologMacroTime":
        //                CultureInfo ciAliquot1 = new CultureInfo("he-IL");
        //                try
        //                {
        //                    DateTime d1 = Convert.ToDateTime(patient.PathologMacroTime.Value);
        //                    DateTime d2 = Convert.ToDateTime(txtFilter, ciAliquot1);

        //                    return d1.ToShortDateString().Equals(d2.ToShortDateString());
        //                }
        //                catch
        //                {
        //                    return true;
        //                }
        //            default:
        //                return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return true;
        //    }
        //}

        #endregion

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            refreshPage();


        }

        private void buttonPrint_Click(object sender, RoutedEventArgs e)
        {
            rowcount = 0;
            dataTable.Columns.Clear();
            dataTable.Rows.Clear();
            //dataTable.Clear();

            GridView gv = (GridView)currentListView.View;

            string[] columnArr = new string[gv.Columns.Count()];
            int col = 0;
            foreach (GridViewColumn item in gv.Columns)
            {
                var column = (item.Header as ContentControl).Content;
                columnArr[col] = column.ToString();
                dataTable.Columns.Add(column.ToString());
                col++;
            }

            Logger.WriteLogFile("columns: " + gv.Columns.Count.ToString());
            int row = 0;

            System.Collections.IList myList;

            if (currentListView.SelectedItems.Count > 0) myList = currentListView.SelectedItems;
            else myList = currentListView.Items;


            foreach (ExtraRequestRow rowObj in myList)//currentListView.Items
            {
                var values = new object[columnArr.Length];
                int i = 0;
                foreach (GridViewColumn item in gv.Columns)
                {
                    var column = (item.Header as ContentControl).Tag;
                    string valueFld = GetPropertyValue(rowObj, column.ToString()).ToString();
                    values[i] = valueFld;
                    i++;
                }
                dataTable.Rows.Add(values);
                row++;
            }
            Logger.WriteLogFile("rows: " + row.ToString());

            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true;//לרוחב

            pd.PrintPage += new PrintPageEventHandler(this.document_PrintPage);//.PrintTextFileHandler);


            System.Windows.Forms.PrintDialog printDlg = new System.Windows.Forms.PrintDialog();

            printDlg.Document = pd;

            if (printDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                pd.Print();

        }

        private string getListViewColumnsHeaders()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}", "מספר מקרה", "עדיפות", "תאריך יצירת המקרה", "מספר בלוק", "מספר סלייד", "שם הצביעה",
                @"שם פתולוג/ית ", "תאריך הבקשה", "נחתך על ידי", "הערות", "רופא מאקרו", "תאריך מאקרו");
        }

        private void textBoxCloseRow_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        public static object GetPropertyValue(object source, string propertyName)
        {
            PropertyInfo property = source.GetType().GetProperty(propertyName);
            var i = property.GetValue(source, null);


            if (i != null)
            {
                if (i.GetType().FullName == "System.DateTime")
                {
                    DateTime d = (DateTime)i;
                    return d.ToString("dd/MM/yyyy");
                }
                return i;
            }
            else
                return "";
        }


        private int rowcount = 0;
        private List<ExtraRequestRow> listExm;
        private List<ExtraRequestRow> listOther;
        private List<ExtraRequestRow> listHi;


        private void document_PrintPage(object sender, PrintPageEventArgs e)
        {
            string header = "Master List    " + DateTime.Now.ToString("dd/MM/yyyy");
            string footer = string.Empty;
            int columnCount = dataTable.Columns.Count;
            int maxRows = dataTable.Rows.Count;
            float linesPerPage = 24;
            StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);
            format1.LineAlignment = StringAlignment.Far;
            format1.Alignment = StringAlignment.Far;
            var maximumLengthForColumns = Enumerable.Range(0, dataTable.Columns.Count).Select(col2 => dataTable.AsEnumerable().Select(row => row[col2].ToString())//.OfType<string>()
        .Max(val2 => val2.ToString())).ToList();//.Length



            //string maxString = dataTable.AsEnumerable()
            //         .Select(row => row[9].ToString())
            //         .OrderByDescending(st => st.Length).FirstOrDefault();

            List<string> maxFld = new List<string>();
            for (int i = 0; i < columnCount; i++)
            {
                string maxString = dataTable.AsEnumerable()
           .Select(row => row[i].ToString())
           .OrderByDescending(st => st.Length).FirstOrDefault();
                if (dataTable.Columns[i].ColumnName.Length > maxString.Length)
                {
                    maxString = dataTable.Columns[i].ColumnName;
                }
                maxFld.Add(maxString);
            }

            using (Graphics g = e.Graphics)
            {
                System.Drawing.Brush brush = new SolidBrush(System.Drawing.Color.Black);
                System.Drawing.Pen pen = new System.Drawing.Pen(brush);
                Font font = new Font("Verdana", 7);
                Font fontHedear = new Font("Arial", 8);
                SizeF size;

                float x = 0, y = 0, width = 1080;
                float xPadding;

                /*
                // Here title is written, sets to top-middle position of the page
                size = g.MeasureString(header, font);
                xPadding = (width - size.Width) / 2;
                g.DrawString(header, font, brush, x + 250, y + 5);
                */

                x = 0;
                y += 30;

                // Writes out all column names in designated locations, aligned as a table

                //for (int i = columnCount - 1; i >= 0; i--)
                for (int i = 0; i < columnCount; i++)
                {
                    //maxFld[i]
                    //size = g.MeasureString(dataTable.Columns[i].ColumnName, font);
                    size = g.MeasureString(maxFld[i], font);
                    //xPadding = (width - size.Width) / 2
                    xPadding = width;
                    g.DrawString(dataTable.Columns[i].ColumnName, fontHedear, brush, x + xPadding, y + 5, format1);
                    x -= size.Width + 15;//maximumLengthForColumns[i];
                }

                x = 0;
                y += 40;
                width = 1080;
                // Process each row and place each item under correct column.
                int pageRow = 0;
                linesPerPage = 24;//((e.MarginBounds.Height / font.GetHeight(g))/2)-5
                Logger.WriteLogFile("linesPerPage: " + linesPerPage.ToString());

                while (pageRow < linesPerPage && rowcount < maxRows)
                {
                    DataRow row = (DataRow)dataTable.Rows[rowcount];

                    for (int i = 0; i < columnCount; i++)
                    {
                        //size = g.MeasureString(row[i].ToString(), font);
                        size = g.MeasureString(maxFld[i], font);
                        //xPadding = (width - size.Width) / 2;
                        xPadding = width;
                        g.DrawString(row[i].ToString(), font, brush, x + xPadding, y + 5, format1);
                        //x += width;
                        x -= size.Width + 15;
                    }

                    //e.HasMorePages = rowcount - 1 < maxRows;

                    x = 0;
                    y += 30;
                    rowcount++;
                    pageRow++;
                }
                x = 0;
                y += 30;
                Logger.WriteLogFile("pageRow: " + pageRow.ToString());
                Logger.WriteLogFile("rowcount: " + rowcount.ToString());

                /*
                footer = "Total: " + maxRows + " |Signed:..........................";
                size = g.MeasureString(footer, font);
                xPadding = (width - size.Width) / 2;
                g.DrawString(footer, font, brush, x + 250, y + 5)
                */
            }

            //If PrintPageEventArgs has more pages to print  
            if (rowcount < maxRows)
            {
                e.HasMorePages = true;
            }
            else
            {

                e.HasMorePages = false;
            }
        }

        private void getGroupItem(List<ExtraRequestRow> list)
        {

            foreach (var item in list)
            {
                switch (itemGroup)
                {
                    case 0:
                        item.Group = null;
                        break;
                    case 1:
                        item.Group = item.SdgPatholabNumber;
                        break;
                    case 2:
                        item.Group = item.ExRequestCreatedOn.Value.ToString("dd/MM/yyyy");
                        break;
                    default:
                        item.Group = null;
                        break;
                }
            }

        }

        private void radio_None_Checked(object sender, RoutedEventArgs e)
        {
            if (!start)
            {
                itemGroup = 0;              
                Select = true;
                init();
                tabControl1_SelectionChanged(null, null);
                Select = false;
            }
        }

        private void radio_Number_Checked(object sender, RoutedEventArgs e)
        {
            itemGroup = 1;
            Select = true;
            init();
            tabControl1_SelectionChanged(null, null);
            Select = false;
        }

        private void radio_Date_Checked(object sender, RoutedEventArgs e)
        {
            itemGroup = 2;
            Select = true;
            init();
            tabControl1_SelectionChanged(null, null);
            Select = false;
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }



        //private void getGroupItem()
        //{
        //    var tab = tabControl1.SelectedItem as TabItem;
        //    if (tab != null)
        //    {
        //        var tuple = dict[tab.Tag.ToString()];
        //        foreach (var item in tuple.Item2)
        //        {
        //            switch (itemGroup)
        //            {
        //                case 0:
        //                    item.Group = null;
        //                    break;
        //                case 1:
        //                    item.Group = item.SdgPatholabNumber;
        //                    break;
        //                case 2:
        //                    item.Group = item.ExRequestCreatedOn.Value.ToString("dd/MM/yyyy");
        //                    break;
        //                default:
        //                    item.Group = null;
        //                    break;
        //            }
        //        }
        //        //SetListViewSource(tuple.Item1, tuple.Item2);
        //        //currentListView = tuple.Item1;
        //        tabControl1_SelectionChanged(null, null);             
        //    }
        //}



    }
}

