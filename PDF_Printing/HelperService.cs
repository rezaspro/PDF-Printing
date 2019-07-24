using PDF_Printing.Helper;
using PdfEdit.Drawing;
using PdfEdit.Pdf;
using PdfEdit.Pdf.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDF_Printing
{
    public class HelperService
    {
        Thread _operationThread;
        volatile bool _isThreadRunning = false;

        int _fileSize = 1000;
        int _fileCount = 0;

        volatile object _documentLocking = new object();
        volatile object _stampeLocking = new object();
        public HelperService()
        {

        }

        public delegate void ProgressBarDelegate(ProgressBarEventArg progressBarEventArg);
        public event ProgressBarDelegate OnProgress;

        public delegate void OperationComplete(bool isCompleted);
        public event OperationComplete OnComplete;

        public void Start()
        {
            _operationThread = new Thread(PdfOperation);
            //OnProgress += HelperService_OnProgress;
            _isThreadRunning = true;
            _operationThread.Start();
        }

        private void HelperService_OnProgress(ProgressBarEventArg progressBarEventArg)
        {
            //throw new System.NotImplementedException();
        }
        void PdfOperation()
        {
            try
            {
                List<Task> _workerTaskList = new List<Task>();
                List<DocumentFileInfo> documentFileInfoList = new List<DocumentFileInfo>();

                #region Initial Work

                DirectoryInfo resourcesDirectory = CommonHelper.GetRequiredDirectory(CommonHelper.RequiredDirectory.ResourcesDirectory);
                DirectoryInfo stampeDirectory = CommonHelper.GetRequiredDirectory(CommonHelper.RequiredDirectory.StampeDirectory);


                OnProgress(new ProgressBarEventArg() { Maximum = _fileSize, IncreseVal = 0, Message = "Operation Started" });
                int printCode = 1000;
                for (int i = 1; i <= _fileSize; i++)
                {
                    PdfDocument pdfDocument = new PdfDocument();
                    var page = pdfDocument.AddPage();
                    page.Size = PdfEdit.PageSize.A4;
                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    gfx.DrawString("This is a test document?", new XFont("Ariel", 10), XBrushes.Black, new PointF(100, 100));
                    gfx.Dispose();
                    pdfDocument.SetLicenseInfo("companyName", "licenseKey");
                    printCode = printCode + 1;
                    string path = $"{resourcesDirectory.FullName}\\{printCode}.pdf";
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    OnProgress(new ProgressBarEventArg() { Maximum = _fileSize, IncreseVal = i, Message = $"Document No: {printCode} Generated" });
                    pdfDocument.Save(path);
                    documentFileInfoList.Add(new DocumentFileInfo() { Path = path, PrintCode = printCode });
                    Thread.Sleep(1000);
                    //OnProgress(new ProgressBarEventArg() { Maximum = fileSize-1, IncreseVal = 0, Message = "" });
                }


                #endregion

                int threadNo = 3;
                int fileMerge = 1;
                ConcurrentStack<DocumentFileInfo> documentConcurrentStack = new ConcurrentStack<DocumentFileInfo>(documentFileInfoList.ToArray());
                //List<DocumentFileInfo> tempDocumentFileInfo = new List<DocumentFileInfo>();
                for (int i = 1; i < threadNo; i++)
                {
                    Task task = new Task(() =>
                    {
                        while (true)
                        {
                            List<DocumentFileInfo> tempDocumentFileInfoList = new List<DocumentFileInfo>();
                            for (int s = 0; s < fileMerge; s++)
                            {
                                DocumentFileInfo documentFileInfoFromStack = null;
                                lock (_documentLocking)
                                {
                                    documentConcurrentStack.TryPop(out documentFileInfoFromStack);
                                    if (documentConcurrentStack == null)
                                    {
                                        break;
                                    }
                                }

                                if (tempDocumentFileInfoList.Contains(documentFileInfoFromStack) == false)
                                {
                                    tempDocumentFileInfoList.Add(documentFileInfoFromStack);
                                }
                            }

                            if (tempDocumentFileInfoList != null && tempDocumentFileInfoList.Any())
                            {
                                StampeDocument(tempDocumentFileInfoList, stampeDirectory);
                            }
                        }

                    });
                    _workerTaskList.Add(task);
                }

                _workerTaskList.ForEach(t => t.Start());
                Task.WaitAll(_workerTaskList.ToArray());
                _workerTaskList.ForEach(task => task.Dispose());

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void StampeDocument(List<DocumentFileInfo> tempDocumentFileInfo, DirectoryInfo returnDir)
        {
            try
            {
                //if we unlock this code, it will throw an exception
                //lock (_stampeLocking)
                //{
                    if (tempDocumentFileInfo != null)
                    {
                        foreach (var item in tempDocumentFileInfo)
                        {
                            PdfDocument pdfDocument;
                            PdfPage page;
                            if (item == null)
                            {
                                return;
                            }
                            if (File.Exists(item.Path))
                            {
                                OnProgress(new ProgressBarEventArg() { Maximum = 1, Message = $"Prepared For Stamping... {item.PrintCode}" });
                                Thread.Sleep(1000); // delay for visualization
                                _fileCount++;
                                pdfDocument = PdfReader.Open(item.Path);
                                page = pdfDocument.Pages[0];

                                //draw barcode
                                XGraphics gfx = XGraphics.FromPdfPage(page);
                                var printCodeText = item.PrintCode.ToString();
                                var barCode39 = new PdfEdit.Drawing.BarCodes.Code3of9Standard(printCodeText)
                                {
                                    TextLocation = new PdfEdit.Drawing.BarCodes.TextLocation(),
                                    StartChar = Convert.ToChar("*"),
                                    EndChar = Convert.ToChar("*"),
                                    Direction = PdfEdit.Drawing.BarCodes.CodeDirection.LeftToRight
                                };
                                XFont fontBarcode = new XFont("Arial", 14, XFontStyle.Regular);
                                XSize barcodeSize = new XSize(Convert.ToDouble(40), Convert.ToDouble(25));

                                barCode39.Size = barcodeSize;
                                gfx.DrawBarCode(barCode39, new XPoint(100, 110));

                                //draw string 
                                gfx.DrawString(printCodeText, new XFont("Arial", 12), XBrushes.Black, 20, 20);

                                gfx.Dispose();

                                pdfDocument.SetLicenseInfo("companyName", "licenseKey");
                                string path = $"{returnDir.FullName}\\{Path.GetFileName(item.Path)}";
                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                }
                                pdfDocument.Save(path);

                                OnProgress(new ProgressBarEventArg() { Maximum = 1, IncreseVal = 1, Message = $"Stamping {item.PrintCode} Completed" });
                            }
                        }
                    }
                //}//lock

                if (_fileCount == 10)
                {
                    OnComplete(true);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        internal void Stop()
        {
            if (_operationThread != null && _operationThread.IsAlive)
            {
                _operationThread.Join(2000);
                OnProgress(new ProgressBarEventArg() { IncreseVal = 0, Message = $"Stopping..." });
            }
            else
            {
                _isThreadRunning = false;
            }
        }
    }

    public class ProgressBarEventArg
    {
        public int IncreseVal { get; set; }
        public string Message { get; set; }
        public int Maximum { get; set; }
    }
}
