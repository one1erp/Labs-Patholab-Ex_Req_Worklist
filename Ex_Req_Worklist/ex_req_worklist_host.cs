using LSExtensionWindowLib;
using LSSERVICEPROVIDERLib;
using Oracle.ManagedDataAccess.Client;
//using Oracle.ManagedDataAccess.Client;

using Patholab_Common;
using Patholab_DAL_V1;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telerik.WinControls.UI;


//using Telerik.WinControls.Data;



namespace Ex_Req_Worklist

{



    [ComVisible(true)]

    [ProgId("Ex_Req_Worklist.Ex_Req_Worklist")]

    public partial class ex_req_worklist_host : UserControl, IExtensionWindow

    {

        #region Private members



        private INautilusProcessXML xmlProcessor;

        private INautilusUser _ntlsUser;

        private IExtensionWindowSite2 _ntlsSite;

        private INautilusServiceProvider sp;

        private INautilusDBConnection _ntlsCon;

        private DataLayer dal = null;

        private OracleConnection oraCon = null;

        private double _session_id;

        long sid = 1;

        private ExtraRequestRow extraRequest_slides;

        public int[] countArr = new int[5];

        private List<ExtraRequestRow> listPart_Immono, listPart_Histochemistry_Others, listPart_ExMaterial, listPart_CellBlock, listPart_Pap;

        Font f = new Font("Segoe UI", 10);//  ,FontStyle.Bold

        public bool debug;

        public string[] tabHedears = new string[6];

        private static Dictionary<string, string> dict = new Dictionary<string, string>();

        private bool winforms = true;
        private bool flag;
        List<int> reqList2Close = new List<int>();

        #endregion

        #region implementing interface

        public ex_req_worklist_host()
        {
            try
            {

                InitializeComponent();

                this.Disposed += PatholabWorkList_Disposed;

                BackColor = Color.FromName("Control");

                this.Dock = DockStyle.Fill;

                this.AutoSize = true;

                this.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                tabHedears = new string[tabControl1.TabCount];

            }

            catch (Exception e)
            {

                MessageBox.Show(e.Message);

            }

        }

        public bool CloseQuery()
        {
            DialogResult res = MessageBox.Show(@"?האם אתה בטוח שברצונך לצאת ", "ex_req_worklist", MessageBoxButtons.YesNo);

            if (res == DialogResult.Yes)
            {
                if (dal != null)
                {

                    dal.Close();

                    dal = null;
                }

                if (_ntlsSite != null) _ntlsSite = null;

                this.Dispose();

                return true;
            }
            else
            {
                return false;
            }

        }



        public WindowRefreshType DataChange()
        {

            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;

        }



        public WindowButtonsType GetButtons()

        {

            return LSExtensionWindowLib.WindowButtonsType.windowButtonsNone;

        }



        public void Internationalise()

        {

        }



        public void PreDisplay()

        {

            xmlProcessor = Utils.GetXmlProcessor(sp);

            _ntlsUser = Utils.GetNautilusUser(sp);

            activateWorkListWindow();

        }



        public void RestoreSettings(int hKey)

        {

        }



        public bool SaveData()

        {

            return true;

        }



        public void SaveSettings(int hKey)

        {

        }



        public void SetParameters(string parameters)

        {

        }



        public void SetServiceProvider(object serviceProvider)

        {

            sp = serviceProvider as NautilusServiceProvider;

            _ntlsCon = Utils.GetNtlsCon(sp);

            this.sid = (long)_ntlsCon.GetSessionId();



        }



        public void SetSite(object site)

        {

            _ntlsSite = (IExtensionWindowSite2)site;

            _ntlsSite.SetWindowInternalName("Ex_Req_Worklsit");

            _ntlsSite.SetWindowRegistryName("Ex_Req_Worklsit");

            _ntlsSite.SetWindowTitle("Ex_Req_Worklsit");

        }



        public void Setup()

        {

        }



        public WindowRefreshType ViewRefresh()

        {

            return LSExtensionWindowLib.WindowRefreshType.windowRefreshNone;

        }



        public void refresh()

        {

        }



        #endregion

        private Dictionary<string, string> PriorityDict()
        {

            if (dict.Count() < 1 && dal != null)
            {

                PHRASE_HEADER header = dal.FindBy<PHRASE_HEADER>(ph => ph.NAME.Equals("Priority")).FirstOrDefault();

                if (header != null)

                {

                    foreach (PHRASE_ENTRY entry in header.PHRASE_ENTRY)

                    {

                        try

                        {

                            dict.Add(entry.PHRASE_NAME, entry.PHRASE_DESCRIPTION);

                        }

                        catch

                        {

                            continue;

                        }

                    }

                }

            }



            return dict;



        }
        private void initDal()
        {
            try

            {
                dal = new DataLayer();

                if (debug)

                {
                    //For running without Nautilus.

                    dal.MockConnect();
                    oraCon = dal.GetOracleConnection(_ntlsCon);

                }

                else

                {

                    dal.Connect(_ntlsCon);
                    oraCon = dal.GetOracleConnection(_ntlsCon);

                }


                PriorityDict();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"From initDal: {ex.Message}");
            }
        }
        void PatholabWorkList_Disposed(object sender, EventArgs e)
        {
            GC.Collect();
        }
        public void activateWorkListWindow()
        {
            try
            {
                if (winforms)
                {
                    int i = 0;

                    foreach (TabPage tab in this.tabControl1.TabPages)

                    {

                        tabHedears[i] = tab.Text;

                        i++;

                    }

                    initDal();
                    _ = LoadDataFromDB();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        //---------------------------------------------
        private async Task LoadDataFromDB()
        {
            listPart_Histochemistry_Others = new List<ExtraRequestRow>();

            listPart_Immono = new List<ExtraRequestRow>();

            listPart_ExMaterial = new List<ExtraRequestRow>();

            listPart_CellBlock = new List<ExtraRequestRow>();

            listPart_Pap = new List<ExtraRequestRow>();

            try
            {
                Task task1 = Task.Run(() => { SetListImmonoHisOthers(); });
                Task task2 = Task.Run(() => { SetListExMaterial(); });


                await Task.WhenAll(task1, task2);

                setListCellBlock();
                setListPap();
                UpdateTabPages();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"From LoadDataFromDB: {ex.Message}");
            }

        }
        private void UpdateTabPages()
        {
            int i = 0;

            foreach (TabPage tab in tabControl1.TabPages)
            {
                RadGridView c_grid = tab.Controls.OfType<RadGridView>().FirstOrDefault();

                if (c_grid.Columns["CreatedOn"] != null)
                {
                    c_grid.Columns["CreatedOn"].FormatString = "{0:dd/MM/yyyy}";
                }

                if (c_grid.Columns["ExRequestCreatedOn"] != null)
                {
                    c_grid.Columns["ExRequestCreatedOn"].FormatString = "{0:dd/MM/yyyy}";
                }

                if (c_grid.Columns["PathologMacroTime"] != null)
                {
                    c_grid.Columns["PathologMacroTime"].FormatString = "{0:dd/MM/yyyy}";
                }

                c_grid.RowFormatting += radGridView_RowFormatting;
                c_grid.CellFormatting += radGridView_ViewCellFormatting;
                c_grid.EnableCustomDrawing = true;
                c_grid.EnableKeyMap = true;
                c_grid.Font = new Font("Segoe UI", 12);
                c_grid.AllowEditRow = false;

                var datalist = c_grid.DataSource as List<ExtraRequestRow>;
                var count = datalist.Count();
                tab.Text = tabHedears[i] + string.Format(" ({0})", count);

                i++;
            }
        }
        private void refreshPage()

        {

            _ = LoadDataFromDB();

            TabPage c_tab = this.tabControl1.SelectedTab;

            RadGridView c_grid = c_tab.Controls.OfType<RadGridView>().FirstOrDefault();

            c_grid.Rows[0].IsCurrent = true;

            c_grid.Rows[0].IsSelected = true;

            c_grid.TableElement.ScrollToRow(0);





        }
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            buttonRefresh.Enabled = false;
            refreshPage();
            buttonRefresh.Enabled = true;
        }
        private void buttonPrint_Click(object sender, EventArgs e)

        {

            TabPage c_tab = this.tabControl1.SelectedTab;

            RadGridView c_grid = c_tab.Controls.OfType<RadGridView>().FirstOrDefault();

            RadPrintDocument setingsPrint = new RadPrintDocument();

            setingsPrint.PrinterSettings.PrinterName = "Microsoft Print to PDF";

            setingsPrint.DefaultPageSettings.Landscape = true;

            c_grid.Print(true, setingsPrint);



        }
        private void radGridView_RowFormatting(object sender, RowFormattingEventArgs e)
        {

            //ResetRowValue(e);

            var row = e.RowElement.Data.DataBoundItem as ExtraRequestRow;

            e.RowElement.DrawFill = true;





            if (row.ScannedByUser)
            {

                if (e.RowElement.IsSelected)

                {

                    e.RowElement.BackColor = (Color)System.Drawing.ColorTranslator.FromHtml("#FF8000");



                }

                else

                {

                    e.RowElement.BackColor = (Color)System.Drawing.ColorTranslator.FromHtml("#FF9933");

                }



            }

            else
            {
                if (e.RowElement.IsSelected)

                {

                    e.RowElement.BackColor = (Color)System.Drawing.ColorTranslator.FromHtml("#3399FF");

                }

                else

                {

                    if (e.RowElement.IsOdd)

                    {

                        e.RowElement.BackColor = (Color)System.Drawing.ColorTranslator.FromHtml("#CCE5FF");//CCE5FF         

                    }

                    else

                    {

                        e.RowElement.BackColor = (Color)System.Drawing.ColorTranslator.FromHtml("#FFE5CC");//FFE5CC

                    }



                    if (row.ExRequestStatus == "חדש")

                    {

                        e.RowElement.ForeColor = (Color)System.Drawing.ColorTranslator.FromHtml("#000000");

                    }

                    else if (row.ExRequestStatus == "בתהליך")

                    {

                        e.RowElement.ForeColor = (Color)System.Drawing.ColorTranslator.FromHtml("#0000FF");

                    }

                }

            }



        }



        #region list settings

        private void setListPap()
        {
            try
            {
                var ListPap = (from dp in dal.GetAll<EXTRA_PAP_DILUTION>()
                               select new ExtraRequestRow()
                               {
                                   SdgPatholabNumber = dp.U_PATHOLAB_NUMBER,
                                   ExRequestCreatedOn = dp.CREATED_ON,
                                   ExReqCreatedBy = dp.NAME,
                                   CreatedOn = dp.RECEIVED_ON,
                                   SampleName = dp.SAMP_NAME,
                                   ExRequestId = dp.EX_REQ_DATA_ID,
                                   Ex_req_status = dp.EX_REQ_STATUS,
                                   sdgId = dp.SDG_ID

                               }

                    );

                listPart_Pap.AddRange(ListPap);


                if (GridPap.InvokeRequired)
                {
                    // If we're not on the UI thread, invoke this method on the UI thread
                    GridPap.Invoke((MethodInvoker)delegate
                    {
                        GridPap.DataSource = listPart_Pap;

                    });
                }
                else
                {
                    GridPap.DataSource = listPart_Pap;

                }

                countArr[4] = listPart_Pap.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Test pap");

            }
        }

        private void setListCellBlock()
        {
            try
            {
                var ListCellBlock = (from cb in dal.GetAll<EXTRA_CELL_BLOCK>()
                                     select new ExtraRequestRow()

                                     {

                                         AliquotPatholabName = cb.ALIQUOT_NAME,
                                         PathologMacroTime = cb.FIRST_MACRO_TIME,
                                         CreatedOn = cb.RECEIVED_ON,
                                         SlideNumber = cb.ALIQUOT_NAME,
                                         ExRequestId = cb.EX_REQ_DATA_ID,
                                         SdgPatholabNumber = cb.U_PATHOLAB_NUMBER,
                                         SampleName = cb.SAMP_NAME,
                                         Ex_req_status = cb.EX_REQ_STATUS,
                                         sdgId = cb.SDG_ID
                                     }

                    );


                listPart_CellBlock.AddRange(ListCellBlock);

                if (GridCellBlock.InvokeRequired)
                {
                    // If we're not on the UI thread, invoke this method on the UI thread
                    GridCellBlock.Invoke((MethodInvoker)delegate
                    {
                        GridCellBlock.DataSource = listPart_CellBlock;
                    });
                }
                else
                {
                    GridCellBlock.DataSource = listPart_CellBlock;
                }


                countArr[3] = listPart_CellBlock.Count;


            }
            catch (Exception ex)
            {

                MessageBox.Show("Test");
            }
        }

        private void SetListExMaterial()

        {

            try

            {
                //טעינה מהDB
                var ListExMaterial = (from em in dal.GetAll<EXTRA_MATERIAL>()

                                      select new ExtraRequestRow()

                                      {

                                          sdgId = em.SDG_ID,

                                          SdgPatholabNumber = em.SDG_PATHOLAB_NUMBER,

                                          Priority_num = em.PRIORITY_NUM,

                                          _Priority = em.PRIORITY,

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



                var listExm = ListExMaterial.OrderBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.Priority_num).ToList();







                listPart_ExMaterial.AddRange(listExm);





                if (GridExMaterial.InvokeRequired)
                {
                    // If we're not on the UI thread, invoke this method on the UI thread
                    GridExMaterial.Invoke((MethodInvoker)delegate
                    {
                        GridExMaterial.DataSource = listPart_ExMaterial;
                    });
                }
                else
                {
                    GridExMaterial.DataSource = listPart_ExMaterial;
                }

                countArr[2] = listPart_ExMaterial.Count;

            }

            catch (Exception ex)

            {

                MessageBox.Show("SetListExMaterial ");

            }



        }

        private void SetListImmonoHisOthers()
        {
            try
            {
                string query = "select * from lims.EXTRA_SLIDES";
                List<ExtraRequestRow> ex_s = new List<ExtraRequestRow>();

                using (OracleCommand cmd = new OracleCommand(query, oraCon))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            extraRequest_slides = new ExtraRequestRow();
                            extraRequest_slides.sdgId = reader.GetInt32(reader.GetOrdinal("sdg_id"));
                            extraRequest_slides.CreatedOn = reader.GetDateTime(reader.GetOrdinal("containerReceivedOn"));
                            extraRequest_slides.SdgPatholabNumber = reader.GetString(reader.GetOrdinal("sdg_patholab_number"));
                            extraRequest_slides.SamplePatholabName = reader.IsDBNull(reader.GetOrdinal("sample_patholab_number")) ? null : reader.GetString(reader.GetOrdinal("sample_patholab_number"));
                            extraRequest_slides.AliquotPatholabName = reader.IsDBNull(reader.GetOrdinal("aliquot_patholab_number")) ? null : reader.GetString(reader.GetOrdinal("aliquot_patholab_number"));
                            extraRequest_slides.Priority_num = reader.GetInt32(reader.GetOrdinal("priority_num"));
                            extraRequest_slides._Priority = reader.GetString(reader.GetOrdinal("priority"));
                            extraRequest_slides.BlockNumber = reader.IsDBNull(reader.GetOrdinal("block_number")) ? null : reader.GetString(reader.GetOrdinal("block_number"));
                            extraRequest_slides.SlideNumber = reader.IsDBNull(reader.GetOrdinal("slide_number")) ? null : reader.GetString(reader.GetOrdinal("slide_number"));
                            extraRequest_slides.PathologName = reader.IsDBNull(reader.GetOrdinal("patholog_name")) ? null : reader.GetString(reader.GetOrdinal("patholog_name"));
                            extraRequest_slides.PathologMacro = reader.IsDBNull(reader.GetOrdinal("patholog_macro")) ? null : reader.GetString(reader.GetOrdinal("patholog_macro"));
                            extraRequest_slides.PathologMacroTime = reader.IsDBNull(reader.GetOrdinal("patholog_macro_time")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("patholog_macro_time"));
                            extraRequest_slides.ExRequestCreatedOn = reader.GetDateTime(reader.GetOrdinal("request_created_on"));
                            extraRequest_slides.ExRequestName = reader.IsDBNull(reader.GetOrdinal("request_name")) ? null : reader.GetString(reader.GetOrdinal("request_name"));
                            extraRequest_slides.ExRequestEntityType = reader.IsDBNull(reader.GetOrdinal("request_Entity_Type")) ? null : reader.GetString(reader.GetOrdinal("request_Entity_Type"));
                            extraRequest_slides.ExRequestDetails = reader.IsDBNull(reader.GetOrdinal("request_details")) ? null : reader.GetString(reader.GetOrdinal("request_details"));
                            extraRequest_slides.ExRequestStatus = reader.IsDBNull(reader.GetOrdinal("request_status")) ? null : reader.GetString(reader.GetOrdinal("request_status"));
                            extraRequest_slides.RequestType = reader.IsDBNull(reader.GetOrdinal("request_type")) ? null : reader.GetString(reader.GetOrdinal("request_type"));
                            extraRequest_slides.Remarks = reader.IsDBNull(reader.GetOrdinal("request_remarks")) ? null : reader.GetString(reader.GetOrdinal("request_remarks"));
                            extraRequest_slides.CuttingLaborant = reader.IsDBNull(reader.GetOrdinal("cutting_laborant")) ? null : reader.GetString(reader.GetOrdinal("cutting_laborant"));
                            extraRequest_slides.ExRequestId = reader.GetInt32(reader.GetOrdinal("req_id"));

                            ex_s.Add(extraRequest_slides);
                        }


                    }
                }

                ex_s.OrderBy(x => x.CreatedOn.HasValue ? x.CreatedOn.Value : DateTime.Now).ThenBy(x => x.SdgPatholabNumber).ThenBy(x => x.Priority_num).ToList();




                //נתנאל ביקש

                //אם לבלוק יש גם בקשת אימונו וגם היסטוכימיה אז שיופיע רק באימונוהיסטוכימיה

                //ואם יש לו רק בקשת היסטוכימיה אז צריך להופיע רק בהיסטוכימיה

                //ואם יש לו רק בקשת אימונו אז צריך להופיע רק באימונוהיסטוכימיה

                //var groupbyBlockNumber = ex_s.GroupBy(x => x.BlockNumber);
                //Debugger.Launch();
                //foreach (var reqsOnBlock in groupbyBlockNumber)
                //{
                //    if (reqsOnBlock.Any(x => x.RequestType == "I"))
                //    {
                //        listPart_Immono.AddRange(reqsOnBlock);
                //    }
                //    else
                //    {
                //        if (reqsOnBlock.Any(x => x.RequestType == "O" || x.RequestType == "H"))
                //            listPart_Histochemistry_Others.AddRange(reqsOnBlock);
                //    }
                //}
                //if (GridImmono.InvokeRequired)
                //{
                //    //If we're not on the UI thread, invoke this method on the UI thread
                //    GridImmono.Invoke((MethodInvoker)delegate
                //    {
                //        GridImmono.DataSource = listPart_Immono;
                //        GridHistochemistry.DataSource = listPart_Histochemistry_Others;
                //    });
                //}
                //else
                //{
                //    // If we are already on the UI thread, directly set the DataSource
                //    GridImmono.DataSource = listPart_Immono;
                //    GridHistochemistry.DataSource = listPart_Histochemistry_Others;
                //}



                var groupbyBlockNumber = ex_s.GroupBy(x => x.BlockNumber);
                List<RequestInfo> xxx = new List<RequestInfo>();
                query = "select BLOCKNUMBER, EXREQUESTCREATEDON from lims.EXTRA_SLIDES_STATUS_P";

                using (OracleCommand cmd = new OracleCommand(query, oraCon))
                {

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            xxx.Add(new RequestInfo
                            {
                                BLOCKNUMBER = !reader.IsDBNull(reader.GetOrdinal("BLOCKNUMBER")) ? reader.GetString(reader.GetOrdinal("BLOCKNUMBER")) : string.Empty,
                                EXREQUESTCREATEDON = !reader.IsDBNull(reader.GetOrdinal("EXREQUESTCREATEDON")) ? reader.GetString(reader.GetOrdinal("EXREQUESTCREATEDON")) : string.Empty,
                            });
                        }
                    }

                }
                foreach (var blockGrp in groupbyBlockNumber)
                {
                    var groupbyCreatedOn = blockGrp.GroupBy(x => x.CreatedOn);

                    foreach (var CreatedOnGrp in groupbyCreatedOn)
                    {
                        var NestedGrpbyType = CreatedOnGrp.GroupBy(x => x.RequestType);
                        var Has_status_p = xxx.Where(x => x.BLOCKNUMBER == blockGrp.Key && CreatedOnGrp.Key.Value.ToString("dd/MM/yyyy").Equals(x.EXREQUESTCREATEDON)).Count();
                        var HasImmuno = NestedGrpbyType.Where(t => t.Key == "I").FirstOrDefault();

                        if (HasImmuno != null || Has_status_p > 0)
                        {
                            listPart_Immono.AddRange(CreatedOnGrp.ToList());
                        }
                        else
                        {
                            listPart_Histochemistry_Others.AddRange(CreatedOnGrp.ToList());
                        }
                    }
                }





                if (GridImmono.InvokeRequired)
                {
                    // If we're not on the UI thread, invoke this method on the UI thread
                    GridImmono.Invoke((MethodInvoker)delegate
                    {
                        GridImmono.DataSource = listPart_Immono;
                        GridHistochemistry.DataSource = listPart_Histochemistry_Others;
                    });
                }
                else
                {
                    // If we are already on the UI thread, directly set the DataSource
                    GridImmono.DataSource = listPart_Immono;
                    GridHistochemistry.DataSource = listPart_Histochemistry_Others;
                }

                countArr[0] = listPart_Immono.Count;

                countArr[1] = listPart_Histochemistry_Others.Count;



            }

            catch (Exception ex)

            {

                MessageBox.Show($"SetListImmonoHisOthers : {ex.Message}");

            }

        }

        #endregion

        #region delete rows region
        private void buttonSelectRow_Click(object sender, EventArgs e)
        {
            SelectRow();
        }
        private void textBoxCloseRow_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SelectRow();
            }
        }
        private void buttonCloseRow_Click(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show("האם להסיר את הסליידים שסומנו?", "הסרת סליידים", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);
                if (result == DialogResult.Yes)
                {
                    int countCloseRows = 0;
                    int countProcessRows = 0;

                    TabPage c_tab = this.tabControl1.SelectedTab;
                    RadGridView c_grid = c_tab.Controls.OfType<RadGridView>().FirstOrDefault();

                    foreach (int index in reqList2Close)
                    {
                        ExtraRequestRow slide = c_grid.Rows[index].DataBoundItem as ExtraRequestRow;

                        var tab = c_grid.Parent as TabPage;
                        string tabTitle = tab.Text;

                        if (slide != null && slide.ScannedByUser)
                        {
                            Logger.WriteLogFile("slide found : " + slide.SlideNumber);

                            if (tabTitle.Contains("Cell Block"))
                            {
                                dal.FindBy<ALIQUOT>(x => x.NAME == slide.SlideNumber).FirstOrDefault().STATUS = "X";
                            }

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

                                    //var exrd = dal.FindBy<U_EXTRA_REQUEST_DATA_USER>(x => x.U_EXTRA_REQUEST_DATA_ID == slide.ExRequestId).SingleOrDefault();
                                    var exrd = requestToColse;

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

                    reqList2Close.Clear();

                    dal.SaveChanges();
                    refreshPage();

                    textBoxCloseRow.Text = string.Empty;



                    if (countProcessRows > 0)

                    {

                        var msg = string.Format("!{0} {1} {2} {3}", "לא ניתן להסיר בקשות בתהליך", countProcessRows, countProcessRows > 1 ? "בקשות לא הוסרו " : "בקשה לא הוסרה ", "מהרשימה ");

                        MessageBox.Show(msg, "מסך בקשות נוספות", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }

                    if (countCloseRows > 0)

                    {

                        var msg = string.Format("!{0} {1} {2} {3}", "התהליך הושלם", countCloseRows, countCloseRows > 1 ? "בקשות הוסרו " : "בקשה הוסרה ", "מהרשימה ");

                        MessageBox.Show(msg, "מסך בקשות נוספות", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }

                }

            }

            catch (Exception ex)

            {

                Logger.WriteLogFile(ex);



                MessageBox.Show(ex.Message);

            }

        }
        private void SelectRow()
        {
            if (textBoxCloseRow.Text == String.Empty)
            {
                MessageBox.Show("חובה למלא ערך בשדה מקרה ");
                flag = false;
                return;
            }

            AddReq2List();

            if (!flag) return;

            TabPage c_tab = this.tabControl1.SelectedTab;

            RadGridView c_grid = c_tab.Controls.OfType<RadGridView>().FirstOrDefault();

            GridTableElement tableElement = c_grid.CurrentView as GridTableElement;

            int indx = GetFirstVisibleRowIndex();

            if (indx != -1)
            {
                tableElement.ScrollToRow(c_grid.Rows[indx]);
            }
        }
        private void AddReq2List()
        {

            TabPage c_tab = this.tabControl1.SelectedTab;
            RadGridView c_grid = c_tab.Controls.OfType<RadGridView>().FirstOrDefault();

            var datalist = c_grid.DataSource as List<ExtraRequestRow>;

            var req2Close = datalist.Where(item => (item.SlideNumber != null && item.SlideNumber.Equals(textBoxCloseRow.Text)) ||

                     (item.SampleName != null && item.SampleName.Equals(textBoxCloseRow.Text)));

            flag = true;

            if (req2Close.Count() < 1)
            {
                MessageBox.Show("לא ניתן למצוא בקשה עם השם הנתון.");
                textBoxCloseRow.Text = string.Empty;
                flag = false;
                return;
            }

            if (req2Close.Count() > 1)
            {
                MessageBox.Show("קיימת יותר מבקשה אחת לאותה יישות,רק ישות אחד תרד מהרשימה", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            var firstReq = req2Close.First();

            req2Close.First().ScannedByUser = true;

            //Galina 05/12/2023

            int index = c_grid.CurrentRow.Index;

            if (tabControl1.TabPages.IndexOfKey(c_tab.Name) == 2 || tabControl1.TabPages.IndexOfKey(c_tab.Name) == 4)

                index = datalist.FindIndex(a => a.SampleName == firstReq.SampleName);

            else

                index = datalist.FindIndex(a => a.SlideNumber == firstReq.SlideNumber);

            c_grid.Rows[index].IsSelected = true;

            textBoxCloseRow.Text = string.Empty;



            reqList2Close.Add(index);


        }      
        public int GetFirstVisibleRowIndex()
        {

            TabPage c_tab = this.tabControl1.SelectedTab;

            RadGridView c_grid = c_tab.Controls.OfType<RadGridView>().FirstOrDefault();

            foreach (GridRowElement row in c_grid.TableElement.VisualRows)
            {

                if (row.RowInfo is GridViewDataRowInfo || row.RowInfo is GridViewGroupRowInfo)
                {
                    return row.RowInfo.Index;
                }

            }
            return -1;
        }
        #endregion
    }
    public class RequestInfo
    {
        public string BLOCKNUMBER { get; set; }
        public string EXREQUESTCREATEDON { get; set; }
    }


}