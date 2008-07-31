// <copyright file="Home.ascx.cs" company="Engage Software">
// Engage: Dashboard - http://www.engagemodules.com
// Copyright (c) 2004-2008
// by Engage Software ( http://www.engagesoftware.com )
// </copyright>
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

namespace Engage.Dnn.Dashboard
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Web.Hosting;
    using System.Xml.XPath;
    using DotNetNuke.Common;
    using DotNetNuke.Services.Exceptions;
    using DotNetNuke.Services.Packages;

    /// <summary>
    /// Displays an overview of the statistics contained in the module
    /// </summary>
    public partial class Home : ModuleBase
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            this.Load += this.Page_Load;
            this.InstallChartingButton.Click += this.InstallChartingButton_Click;
        }

        /// <summary>
        /// Determines whether the Telerik Charting component has been installed into the web.config.
        /// </summary>
        /// <returns><c>true</c> if Telerik charting is installed in the web.config, otherwise <c>false</c></returns>
        private bool ChartingIsInstalled()
        {
            // ReSharper disable AssignNullToNotNullAttribute
            XPathDocument configXml = new XPathDocument(HostingEnvironment.MapPath("~/web.config"));

            // ReSharper restore AssignNullToNotNullAttribute
            double iisVersion;
            string iisSoftwareVariable = this.Request.ServerVariables["SERVER_SOFTWARE"];
            if (iisSoftwareVariable.Contains("/") && double.TryParse(iisSoftwareVariable.Split('/')[1], NumberStyles.Float, CultureInfo.InvariantCulture, out iisVersion))
            {
                XPathExpression httpHandlerPath = iisVersion < 7
                                                      ? XPathExpression.Compile("//configuration/system.web/httpHandlers/add[@path='ChartImage.axd']")
                                                      : XPathExpression.Compile("//configuration/system.webServer/handlers/add[@path='ChartImage.axd']");

                return configXml.CreateNavigator().SelectSingleNode(httpHandlerPath) != null;
            }

            return false;
        }

        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Catching Exception at highest level as a safeguard")]
        private void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!Page.IsPostBack)
                {
                    this.FillData();
                }
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        /// <summary>
        /// Handles the Click event of the InstallChartingButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void InstallChartingButton_Click(object sender, EventArgs e)
        {
            this.InstallTelerikCharting();
        }

        /// <summary>
        /// Installs support for Telerik Charting in the web.config.
        /// </summary>
        private void InstallTelerikCharting()
        {
            using (Stream xmlMergeManifest = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Engage.Dnn.Dashboard.01.00.00.xml"))
            {
                XmlMerge configMerge = new XmlMerge(xmlMergeManifest, "01.00.00", "Engage: Dashboard");
                configMerge.UpdateConfigs();
                this.Response.Redirect(Globals.NavigateURL());
            }
        }

        /// <summary>
        /// Fills the data.
        /// </summary>
        private void FillData()
        {
            this.SetUpDashboardGauges();
            this.SetUpCharting();
        }

        /// <summary>
        /// Sets up the Dundas Guage that visualizes the percentage of pages which lack a keyword or a description
        /// and the percentage of overall database size accounted for by the log file.
        /// </summary>
        private void SetUpDashboardGauges()
        {
            double databaseSize;
            double logSize;

            this.DashboardGaugeContainer.ImageUrl = ApplicationUrl + DesktopModuleFolderName + "DashboardGuageTempFiles";

            this.DashboardGaugeContainer.CircularGauges["PagesGauge"].Scales[0].Minimum = 0;
            this.DashboardGaugeContainer.CircularGauges["PagesGauge"].Scales[0].Maximum = 100;
            this.DashboardGaugeContainer.CircularGauges["PagesGauge"].Scales[0].Interval = 10;

            this.DashboardGaugeContainer.CircularGauges["DatabaseGauge"].Scales[0].Minimum = 0;
            this.DashboardGaugeContainer.CircularGauges["DatabaseGauge"].Scales[0].Maximum = 100;
            this.DashboardGaugeContainer.CircularGauges["DatabaseGauge"].Scales[0].Interval = 10;

            this.DashboardGaugeContainer.CircularGauges["PagesGauge"].Pointers[0].Value = 
                (DataProvider.Instance().CountPagesWithoutDescriptionOrKeywords(this.PortalId) /
                DataProvider.Instance().CountPages(this.PortalId)) * 100;
            
            // todo: make getting the size of the database and the log safer!
            if (Double.TryParse(DataProvider.Instance().GetSizeOfDatabase().Replace(" KB", ""), out databaseSize) 
                && Double.TryParse(DataProvider.Instance().GetDatabaseLogSize().Replace(" KB", ""), out logSize))
            {
                this.DashboardGaugeContainer.CircularGauges["DatabaseGauge"].Pointers[0].Value = (logSize / databaseSize) * 100;
            }
        }

        /// <summary>
        /// Sets up charting.
        /// </summary>
        private void SetUpCharting()
        {
            if (this.ChartingIsInstalled())
            {
                this.UserRegistrationsChart.DataSource = DataProvider.Instance().GetUserRegistrationsInDateSpan(DateTime.Today.AddMonths(-1), DateTime.Today, this.PortalId);
                this.UserRegistrationsChart.DataBind();
            }
            else
            {
                this.ChartsMultiview.SetActiveView(this.InstallChartingView);
            }
        }
    }
}
