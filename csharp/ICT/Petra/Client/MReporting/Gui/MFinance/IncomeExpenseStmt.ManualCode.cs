//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//       Tim Ingham
//
// Copyright 2004-2014 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using Ict.Petra.Client.MFinance.Logic;
using Ict.Petra.Client.MReporting.Logic;
using Ict.Petra.Shared.MReporting;
using Ict.Petra.Client.App.Core.RemoteObjects;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Ict.Common;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Shared;
using System.Windows.Forms;

namespace Ict.Petra.Client.MReporting.Gui.MFinance
{
    public partial class TFrmIncomeExpenseStmt
    {
        private Int32 FLedgerNumber;

        /// <summary>
        /// the report should be run for this ledger
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;

                uco_CostCentreSettings.InitialiseCostCentreList(FLedgerNumber);
                uco_GeneralSettings.InitialiseLedger(FLedgerNumber);

                FPetraUtilsObject.LoadDefaultSettings();

                FPetraUtilsObject.FFastReportsPlugin.SetDataGetter(LoadReportData);
            }
        }

        //
        // This will be called if the Fast Reports Wrapper loaded OK.
        // Returns True if the data apparently loaded OK and the report should be printed.
        private bool LoadReportData(TRptCalculator ACalc)
        {
            ArrayList reportParam = ACalc.GetParameters().Elems;

            Dictionary <String, TVariant>paramsDictionary = new Dictionary <string, TVariant>();

            foreach (Shared.MReporting.TParameter p in reportParam)
            {
                if (p.name.StartsWith("param") && (p.name != "param_calculation") && (!paramsDictionary.ContainsKey(p.name)))
                {
                    paramsDictionary.Add(p.name, p.value);
                }
            }

            Int32 ParamNestingDepth = 99;
            String DepthOption = paramsDictionary["param_depth"].ToString();

            if (DepthOption == "Summary")
            {
                ParamNestingDepth = 2;
            }

            if (DepthOption == "Standard")
            {
                ParamNestingDepth = 3;
            }

            paramsDictionary.Add("param_nesting_depth", new TVariant(ParamNestingDepth));


            //
            // The table contains Actual and Budget figures, both this period and YTD, also last year and budget last year.
            // It does not contain any variance (actual / budget) figures - these are calculated in the report.

            DataTable ReportTable = TRemote.MReporting.WebConnectors.GetReportDataTable("IncomeExpense", paramsDictionary);

            if (this.IsDisposed) // There's no cancel function as such - if the user has pressed Esc the form is closed!
            {
                return false;
            }

            if (ReportTable == null)
            {
                FPetraUtilsObject.WriteToStatusBar("Report Cancelled.");
                return false;
            }

            FPetraUtilsObject.FFastReportsPlugin.RegisterData(ReportTable, "IncomeExpense");

            //
            // I need to get the name of the current ledger..

            DataTable LedgerNameTable = TDataCache.TMFinance.GetCacheableFinanceTable(TCacheableFinanceTablesEnum.LedgerNameList);
            DataView LedgerView = new DataView(LedgerNameTable);
            LedgerView.RowFilter = "LedgerNumber=" + FLedgerNumber;
            String LedgerName = "";

            if (LedgerView.Count > 0)
            {
                LedgerName = LedgerView[0].Row["LedgerName"].ToString();
            }

            ACalc.AddStringParameter("param_ledger_name", LedgerName);
            ACalc.AddStringParameter("param_linked_partner_cc", ""); // I may want to use this for auto_email, but usually it's unused.

            //
            // For reports that must be sent on email, one page at a time,
            // I'm calling the FastReports plugin multiple times,
            // and then I'm going to return false, which will prevent the default action using this dataset.

            Shared.MReporting.TParameterList pm = ACalc.GetParameters();

            if ((pm.Get("param_auto_email").ToBool())
                && !pm.Get("param_design_template").ToBool()
                )
            {
                String CostCentreFilter = "";
                String CostCentreOptions = pm.Get("param_costcentreoptions").ToString();

                if (CostCentreOptions == "SelectedCostCentres")
                {
                    String CostCentreList = pm.Get("param_cost_centre_codes").ToString();
                    CostCentreList = CostCentreList.Replace(",", "','");                             // SQL IN List items in single quotes
                    CostCentreFilter = " AND a_cost_centre_code_c in ('" + CostCentreList + "')";
                }

                if (CostCentreOptions == "CostCentreRange")
                {
                    CostCentreFilter = " AND a_cost_centre_code_c >='" + pm.Get("param_cost_centre_code_start").ToString() +
                                       "' AND a_cost_centre_code_c >='" + pm.Get("param_cost_centre_code_end").ToString() + "'";
                }

                String Status = FastReportsWrapper.AutoEmailReports(FPetraUtilsObject,
                    FPetraUtilsObject.FFastReportsPlugin,
                    ACalc,
                    FLedgerNumber,
                    CostCentreFilter);
                MessageBox.Show(Status, Catalog.GetString("Income Expense Report"));
                return false;
            }

            return true;
        }

        private void ReadControlsManual(TRptCalculator ACalc, TReportActionEnum AReportAction)
        {
            ACalc.AddParameter("param_ledger_number_i", FLedgerNumber);
        }

        private void RunOnceOnActivationManual()
        {
            if (FPetraUtilsObject.FFastReportsPlugin.LoadedOK)
            {
                this.tabReportSettings.Controls.Remove(tpgAdditionalSettings); // These tabs represent settings that are not supported
                this.tabReportSettings.Controls.Remove(tpgColumnSettings);     // in the FastReports based solution.
            }
        }

        private void SetControlsManual(TParameterList AParameters)
        {
        }
    }
}