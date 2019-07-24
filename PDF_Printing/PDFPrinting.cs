using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDF_Printing
{
    public partial class PDFPrinting : Form
    {
        HelperService _helperService;
        delegate void FinalCaller(bool isCompleted);
        public PDFPrinting()
        {
            InitializeComponent();
            _helperService = new HelperService();
            progressBarControl.Maximum = 10;
            //var executingAssemblyNamespace = Assembly.GetExecutingAssembly().GetName().Name;
            //var assembly = Assembly.GetAssembly(GetType());
            //foreach (var item in assembly.DefinedTypes)
            //{

            //}
            btnStop.Enabled = false;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            _helperService.Start();
            _helperService.OnProgress += _helperService_OnProgress;
            _helperService.OnComplete += _helperService_OnComplete;
        }

        private void _helperService_OnComplete(bool isCompleted)
        {
            if (isCompleted)
            {
                if (progressBarControl.InvokeRequired)
                {
                    progressBarControl.Invoke(new FinalCaller(OnCompleteCaller), new object[] { isCompleted });
                    progressBarControl.Invoke(new MethodInvoker(delegate { progressBarControl.Maximum = 1; progressBarControl.Value = 1; }));
                }

            }
        }

        private void OnCompleteCaller(bool isCompleted)
        {
            try
            {
                if (isCompleted)
                {
                    lblStatusText.Text = $"Process Completed";
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void _helperService_OnProgress(ProgressBarEventArg progressBarEventArg)
        {
            if (progressBarControl.InvokeRequired)
            {
                progressBarControl.Invoke(new MethodInvoker(delegate { progressBarControl.Maximum = progressBarEventArg.Maximum; progressBarControl.Value = progressBarEventArg.IncreseVal; }));
            }

            if (lblStatusText.InvokeRequired)
            {
                lblStatusText.Invoke(new MethodInvoker(delegate { lblStatusText.Text = progressBarEventArg.Message; }));
            }
        }

        private void PDFPrinting_Load(object sender, EventArgs e)
        {
            clockTimer = new Timer();
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            string dateTime = DateTime.Now.ToString("dd-MM-yyyy h:mm:ss tt");
            //Text = $"PDF Example {dateTime}";
            lblDateTime.Text = dateTime;
        }
    }
}
