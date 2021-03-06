//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop, christophert, alanP
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
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.Data;
using Ict.Common.Verification;

using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.CommonDialogs;
using Ict.Petra.Client.MCommon;
using Ict.Petra.Client.MFinance.Gui.Setup;
using Ict.Petra.Client.MFinance.Logic;

using Ict.Petra.Shared;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MFinance.Validation;

namespace Ict.Petra.Client.MFinance.Gui.Gift
{
    /// <summary>
    /// Interface used by logic objects in order to access selected public methods in the TUC_GiftBatches class
    /// </summary>
    public interface IUC_GiftBatches
    {
        /// <summary>
        /// Load the batches for the current financial year (used in particular when the screen starts up).
        /// </summary>
        void LoadBatchesForCurrentYear();

        /// <summary>
        /// Create a new Gift Batch
        /// </summary>
        bool CreateNewAGiftBatch();

        /// <summary>
        /// Validate all data
        /// </summary>
        bool ValidateAllData(bool ARecordChangeVerification,
            TErrorProcessingMode ADataValidationProcessingMode,
            Control AValidateSpecificControl = null,
            bool ADontRecordNewDataValidationRun = true);
    }

    public partial class TUC_GiftBatches : IUC_GiftBatches
    {
        private Int32 FLedgerNumber;

        // Logic objects
        private TUC_GiftBatches_LoadAndFilter FLoadAndFilterLogicObject = null;
        private TUC_GiftBatches_Import FImportLogicObject = null;
        private TUC_GiftBatches_Post FPostingLogicObject = null;
        private TUC_GiftBatches_Receipt FReceiptingLogicObject = null;
        private TUC_GiftBatches_Cancel FCancelLogicObject = null;
        private TUC_GiftBatches_AccountAndCostCentre FAccountAndCostCentreLogicObject = null;

        private bool FActiveOnly = false;
        private bool FBankAccountOnly = true;
        private string FSelectedBatchMethodOfPayment = String.Empty;

        private ACostCentreTable FCostCentreTable = null;
        private AAccountTable FAccountTable = null;

        //System & User Defaults
        private bool FDonorZeroIsValid = false;
        private bool FRecipientZeroIsValid = false;
        private bool FWarnOfInactiveValuesOnPosting = false;

        //Date related
        private DateTime FDefaultDate;
        private DateTime FStartDateCurrentPeriod;
        private DateTime FEndDateLastForwardingPeriod;

        //Currency related
        private string FLedgerBaseCurrency = String.Empty;

        /// <summary>
        /// Flags whether all the gift batch rows for this form have finished loading
        /// </summary>
        public bool FBatchLoaded = false;

        /// <summary>
        /// Currently selected batchnumber
        /// </summary>
        public Int32 FSelectedBatchNumber = -1;

        /// <summary>
        /// load the batches into the grid
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;

                ParentForm.Cursor = Cursors.WaitCursor;
                InitialiseLogicObjects();
                InitialiseLedgerControls();
            }
        }

        private Boolean ViewMode
        {
            get
            {
                return ((TFrmGiftBatch)ParentForm).ViewMode;
            }
        }

        private GiftBatchTDS ViewModeTDS
        {
            get
            {
                return ((TFrmGiftBatch)ParentForm).ViewModeTDS;
            }
        }

        /// <summary>
        /// return the method of Payment for the transaction tab
        /// </summary>
        public String MethodOfPaymentCode
        {
            get
            {
                return FSelectedBatchMethodOfPayment;
            }
        }

        private void InitialiseLogicObjects()
        {
            FLoadAndFilterLogicObject = new TUC_GiftBatches_LoadAndFilter(FPetraUtilsObject, FLedgerNumber, FMainDS, FFilterAndFindObject);
            FImportLogicObject = new TUC_GiftBatches_Import(FPetraUtilsObject, FLedgerNumber, this);
            FPostingLogicObject = new TUC_GiftBatches_Post(FPetraUtilsObject, FLedgerNumber, FMainDS);
            FReceiptingLogicObject = new TUC_GiftBatches_Receipt();
            FCancelLogicObject = new TUC_GiftBatches_Cancel(FPetraUtilsObject, FLedgerNumber, FMainDS);
            FAccountAndCostCentreLogicObject = new TUC_GiftBatches_AccountAndCostCentre(FLedgerNumber,
                FMainDS,
                cmbDetailBankAccountCode,
                cmbDetailBankCostCentre);
        }

        private void InitialiseLedgerControls()
        {
            // Load Motivation detail in this central place; it will be used by UC_GiftTransactions
            AMotivationDetailTable MotivationDetail = (AMotivationDetailTable)TDataCache.TMFinance.GetCacheableFinanceTable(
                TCacheableFinanceTablesEnum.MotivationList,
                FLedgerNumber);

            MotivationDetail.TableName = FMainDS.AMotivationDetail.TableName;
            FMainDS.Merge(MotivationDetail);

            FMainDS.AMotivationDetail.AcceptChanges();

            FMainDS.AGiftBatch.DefaultView.Sort = String.Format("{0}, {1} DESC",
                AGiftBatchTable.GetLedgerNumberDBName(),
                AGiftBatchTable.GetBatchNumberDBName()
                );

            SetupExtraGridFunctionality();
            FAccountAndCostCentreLogicObject.RefreshBankAccountAndCostCentreData(FLoadAndFilterLogicObject, out FCostCentreTable, out FAccountTable);

            // if this form is readonly, then we need all codes, because old codes might have been used
            bool ActiveOnly = false; //this.Enabled;
            SetupAccountAndCostCentreCombos(ActiveOnly);

            cmbDetailMethodOfPaymentCode.AddNotSetRow("", "");
            TFinanceControls.InitialiseMethodOfPaymentCodeList(ref cmbDetailMethodOfPaymentCode, ActiveOnly);

            TLedgerSelection.GetCurrentPostingRangeDates(FLedgerNumber,
                out FStartDateCurrentPeriod,
                out FEndDateLastForwardingPeriod,
                out FDefaultDate);
            lblValidDateRange.Text = String.Format(Catalog.GetString("Valid between {0} and {1}"),
                FStartDateCurrentPeriod.ToShortDateString(), FEndDateLastForwardingPeriod.ToShortDateString());

            FLoadAndFilterLogicObject.InitialiseDataSources(cmbDetailBankCostCentre, cmbDetailBankAccountCode);
        }

        private void RunOnceOnParentActivationManual()
        {
            try
            {
                ParentForm.Cursor = Cursors.WaitCursor;
                grdDetails.DoubleClickCell += new TDoubleClickCellEventHandler(this.ShowTransactionTab);
                grdDetails.DataSource.ListChanged += new System.ComponentModel.ListChangedEventHandler(DataSource_ListChanged);

                // Load the ledger table so we know the base currency
                FMainDS.Merge(TRemote.MFinance.Gift.WebConnectors.LoadALedgerTable(FLedgerNumber));
                FLedgerBaseCurrency = FMainDS.ALedger[0].BaseCurrency;

                FLoadAndFilterLogicObject.ActivateFilter();
                LoadBatchesForCurrentYear();

                // read system and user defaults
                FDonorZeroIsValid = ((TFrmGiftBatch)ParentForm).FDonorZeroIsValid;
                FRecipientZeroIsValid = ((TFrmGiftBatch)ParentForm).FRecipientZeroIsValid;
                FWarnOfInactiveValuesOnPosting = ((TFrmGiftBatch)ParentForm).FWarnOfInactiveValuesOnPosting;

                SetInitialFocus();
            }
            finally
            {
                ParentForm.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Refresh the data in the grid and the details after the database content was changed on the server
        /// The current filter is not changed.  The highlighted row index remains the same (if possible) after the refresh.
        /// </summary>
        public void RefreshAllData(bool AShowStatusDialogOnLoad = true, bool AIsMessageRefresh = false)
        {
            TFrmGiftBatch myParentForm = (TFrmGiftBatch)ParentForm;

            // Remember our current row position
            int nCurrentRowIndex = GetSelectedRowIndex();
            int nCurrentBatchNumber = -1;

            if ((myParentForm != null) && (myParentForm.InitialBatchNumber > 0))
            {
                nCurrentBatchNumber = myParentForm.InitialBatchNumber;
                myParentForm.InitialBatchNumber = -1;
            }
            else if (AIsMessageRefresh)
            {
                if (FPetraUtilsObject.HasChanges && !myParentForm.SaveChanges())
                {
                    string msg = String.Format(Catalog.GetString("A validation error has occured on the Gift Batches" +
                            " form while trying to refresh.{0}{0}" +
                            "You will need to close and reopen the Gift Batches form to see the new batch" +
                            " after you have fixed the validation error."),
                        Environment.NewLine);

                    MessageBox.Show(msg, "Refresh Gift Batches");
                    return;
                }

                nCurrentBatchNumber = 1;
            }
            else if (FPreviouslySelectedDetailRow != null)
            {
                nCurrentBatchNumber = FPreviouslySelectedDetailRow.BatchNumber;
            }

            TFrmGiftBatch parentForm = (TFrmGiftBatch)ParentForm;
            Cursor prevCursor = null;

            if (parentForm != null)
            {
                prevCursor = parentForm.Cursor;
            }
            else
            {
                prevCursor = this.Cursor;
            }

            parentForm.Cursor = Cursors.WaitCursor;

            if ((FMainDS != null) && (FMainDS.AGiftBatch != null))
            {
                // Remove all data from our DataSet object - the grid will go empty!
                FMainDS.AGiftBatch.Rows.Clear();
            }

            try
            {
                FPetraUtilsObject.DisableDataChangedEvent();

                // Calling ApplyFilter() will automatically load the data for the currently selected year
                //  because our ApplyFilterManual() code will do that for us
                FFilterAndFindObject.ApplyFilter();

                // Now we can select the gift batch we had before (if it still exists on the grid)
                for (int i = 0; (i < FMainDS.AGiftBatch.Rows.Count); i++)
                {
                    if (FMainDS.AGiftBatch[i].BatchNumber == nCurrentBatchNumber)
                    {
                        DataView dv = ((DevAge.ComponentModel.BoundDataView)grdDetails.DataSource).DataView;
                        Int32 RowNumberGrid = DataUtilities.GetDataViewIndexByDataTableIndex(dv, FMainDS.AGiftBatch, i) + 1;

                        nCurrentRowIndex = RowNumberGrid;
                        break;
                    }
                }

                ShowDetails(nCurrentRowIndex);

                UpdateRecordNumberDisplay();

                TUC_GiftTransactions TransactionForm = parentForm.GetTransactionsControl();

                if (TransactionForm != null)
                {
                    parentForm.EnableTransactions(grdDetails.Rows.Count > 1);

                    // if the batch number = -1 then this is not a valid instance of TUC_GiftTransactions and we do not need to refresh
                    if (TransactionForm.FBatchNumber != -1)
                    {
                        TransactionForm.ShowStatusDialogOnLoad = AShowStatusDialogOnLoad;

                        // This will update the transactions to match the current batch
                        TransactionForm.RefreshData();

                        TransactionForm.ShowStatusDialogOnLoad = true;
                    }
                }
            }
            finally
            {
                FPetraUtilsObject.EnableDataChangedEvent();
                parentForm.Cursor = prevCursor;
            }
        }

        /// reset the control
        public void ClearCurrentSelection()
        {
            if (FPetraUtilsObject.HasChanges)
            {
                GetDataFromControls();
            }

            this.FPreviouslySelectedDetailRow = null;
            ShowData();
        }

        /// <summary>
        /// enable or disable the buttons
        /// </summary>
        public void UpdateChangeableStatus()
        {
            bool changeable = (FPreviouslySelectedDetailRow != null) && (!ViewMode)
                              && (FPreviouslySelectedDetailRow.BatchStatus == MFinanceConstants.BATCH_UNPOSTED);

            pnlDetails.Enabled = changeable;

            this.btnNew.Enabled = !ViewMode;
            this.btnCancel.Enabled = changeable;
            this.btnPostBatch.Enabled = changeable;
            mniBatch.Enabled = !ViewMode;
            mniPost.Enabled = !ViewMode;
            tbbExportBatches.Enabled = !ViewMode;
            tbbImportBatches.Enabled = !ViewMode;
            tbbPostBatch.Enabled = !ViewMode;
        }

        /// <summary>
        /// Checks various things on the form before saving
        /// </summary>
        public void CheckBeforeSaving()
        {
            UpdateBatchPeriod(null, null);
        }

        /// <summary>
        /// Sets the initial focus to the grid or the New button depending on the row count
        /// </summary>
        public void SetInitialFocus()
        {
            if (grdDetails.CanFocus)
            {
                if (grdDetails.Rows.Count < 2)
                {
                    btnNew.Focus();
                }
                else
                {
                    grdDetails.Focus();
                }
            }
        }

        /// <summary>
        /// load the batches into the grid
        /// </summary>
        public void LoadBatchesForCurrentYear()
        {
            TFrmGiftBatch MyParentForm = (TFrmGiftBatch) this.ParentForm;
            bool PerformStandardLoad = true;

            if (MyParentForm.InitialBatchYear >= 0)
            {
                FLoadAndFilterLogicObject.StatusAll = true;

                int yearIndex = FLoadAndFilterLogicObject.FindYearAsIndex(MyParentForm.InitialBatchYear);

                if (yearIndex >= 0)
                {
                    FLoadAndFilterLogicObject.YearIndex = yearIndex;

                    if (MyParentForm.InitialBatchPeriod >= 0)
                    {
                        FLoadAndFilterLogicObject.PeriodIndex = FLoadAndFilterLogicObject.FindPeriodAsIndex(MyParentForm.InitialBatchPeriod);
                    }
                    else
                    {
                        FLoadAndFilterLogicObject.PeriodIndex = (MyParentForm.InitialBatchYear == FMainDS.ALedger[0].CurrentFinancialYear) ? 1 : 0;
                    }

                    PerformStandardLoad = false;
                }

                // Reset the start-up value
                MyParentForm.InitialBatchYear = -1;
            }

            MyParentForm.ClearCurrentSelections();

            if (ViewMode)
            {
                FMainDS.Merge(ViewModeTDS);
                FLoadAndFilterLogicObject.DisableYearAndPeriod(true);
            }

            if (PerformStandardLoad)
            {
                // Set up for current year with current and forwarding periods (on initial load this will already be set so will not fire a change)
                FLoadAndFilterLogicObject.YearIndex = 0;
                FLoadAndFilterLogicObject.PeriodIndex = 0;
            }

            // Get the data, populate the grid and re-select the current row (or first row if none currently selected) ...
            RefreshAllData();

            FBatchLoaded = true;
        }

        private void SetupAccountAndCostCentreCombos(bool AActiveOnly, AGiftBatchRow ARow = null)
        {
            if (!FBatchLoaded || (FActiveOnly != AActiveOnly))
            {
                FActiveOnly = AActiveOnly;

                FAccountAndCostCentreLogicObject.SetupAccountAndCostCentreCombos(AActiveOnly, ARow);
            }
        }

        /// <summary>
        /// Gift Type radiobutton selection changed
        /// </summary>
        private void GiftTypeChanged(Object sender, EventArgs e)
        {
            bool BankAccountOnly = true;

            // show all accounts for 'Gift In Kind' and 'Other'
            if (rbtGiftInKind.Checked || rbtOther.Checked)
            {
                BankAccountOnly = false;
            }

            if (BankAccountOnly != FBankAccountOnly)
            {
                FAccountAndCostCentreLogicObject.SetupAccountCombo(FActiveOnly,
                    BankAccountOnly,
                    ref lblDetailBankAccountCode,
                    FPreviouslySelectedDetailRow);
                FBankAccountOnly = BankAccountOnly;
            }
        }

        private void RefreshBankAccountAndCostCentreFilters(bool AActiveOnly, AGiftBatchRow ARow = null)
        {
            if (FActiveOnly != AActiveOnly)
            {
                FActiveOnly = AActiveOnly;

                FAccountAndCostCentreLogicObject.RefreshBankAccountAndCostCentreFilters(AActiveOnly, ARow);
            }
        }

        private void SetupExtraGridFunctionality()
        {
            //Prepare grid to highlight inactive accounts/cost centres
            // Create a cell view for special conditions
            SourceGrid.Cells.Views.Cell strikeoutCell = new SourceGrid.Cells.Views.Cell();
            strikeoutCell.Font = new System.Drawing.Font(grdDetails.Font, FontStyle.Strikeout);
            //strikeoutCell.ForeColor = Color.Crimson;

            // Create a condition, apply the view when true, and assign a delegate to handle it
            SourceGrid.Conditions.ConditionView conditionAccountCodeActive = new SourceGrid.Conditions.ConditionView(strikeoutCell);
            conditionAccountCodeActive.EvaluateFunction = delegate(SourceGrid.DataGridColumn column, int gridRow, object itemRow)
            {
                DataRowView row = (DataRowView)itemRow;
                string accountCode = row[AGiftBatchTable.ColumnBankAccountCodeId].ToString();
                return !FAccountAndCostCentreLogicObject.AccountIsActive(accountCode);
            };

            SourceGrid.Conditions.ConditionView conditionCostCentreCodeActive = new SourceGrid.Conditions.ConditionView(strikeoutCell);
            conditionCostCentreCodeActive.EvaluateFunction = delegate(SourceGrid.DataGridColumn column, int gridRow, object itemRow)
            {
                DataRowView row = (DataRowView)itemRow;
                string costCentreCode = row[AGiftBatchTable.ColumnBankCostCentreId].ToString();
                return !FAccountAndCostCentreLogicObject.CostCentreIsActive(costCentreCode);
            };

            //Add conditions to columns
            int indexOfCostCentreCodeDataColumn = 7;
            int indexOfAccountCodeDataColumn = 8;

            grdDetails.Columns[indexOfCostCentreCodeDataColumn].Conditions.Add(conditionCostCentreCodeActive);
            grdDetails.Columns[indexOfAccountCodeDataColumn].Conditions.Add(conditionAccountCodeActive);
        }

        /// <summary>
        /// get the row of the current batch
        /// </summary>
        /// <returns>AGiftBatchRow</returns>
        public AGiftBatchRow GetCurrentBatchRow()
        {
            if (FBatchLoaded && (FPreviouslySelectedDetailRow != null))
            {
                return (AGiftBatchRow)FMainDS.AGiftBatch.Rows.Find(new object[] { FLedgerNumber, FPreviouslySelectedDetailRow.BatchNumber });
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// return any specified gift batch row
        /// </summary>
        /// <returns>AGiftBatchRow</returns>
        public AGiftBatchRow GetAnyBatchRow(Int32 ABatchNumber)
        {
            if (FBatchLoaded)
            {
                return (AGiftBatchRow)FMainDS.AGiftBatch.Rows.Find(new object[] { FLedgerNumber, ABatchNumber });
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// This Method is needed for UserControls who get dynamically loaded on TabPages.
        /// </summary>
        public void AdjustAfterResizing()
        {
            // TODO Adjustment of SourceGrid's column widths needs to be done like in Petra 2.3 ('SetupDataGridVisualAppearance' Methods)
        }

        /// <summary>
        /// show ledger number
        /// </summary>
        private void ShowDataManual()
        {
            //Nothing to do as yet
        }

        private void ShowDetailsManual(AGiftBatchRow ARow)
        {
            ((TFrmGiftBatch)ParentForm).EnableTransactions(ARow != null
                && ARow.BatchStatus != MFinanceConstants.BATCH_CANCELLED);

            if (ARow == null)
            {
                FSelectedBatchNumber = -1;
                dtpDetailGlEffectiveDate.Date = FDefaultDate;
                UpdateChangeableStatus();
                txtDetailHashTotal.CurrencyCode = String.Empty;
                return;
            }

            bool Unposted = (ARow.BatchStatus == MFinanceConstants.BATCH_UNPOSTED);

            if (!FPostingLogicObject.PostingInProgress)
            {
                bool activeOnly = false; //unposted
                RefreshBankAccountAndCostCentreFilters(activeOnly, ARow);
            }

            FLedgerNumber = ARow.LedgerNumber;
            FSelectedBatchNumber = ARow.BatchNumber;

            FPetraUtilsObject.DetailProtectedMode = (!Unposted || ViewMode);
            UpdateChangeableStatus();

            //Update the batch period if necessary
            UpdateBatchPeriod();

            RefreshCurrencyRelatedControls();

            if (Unposted)
            {
                //Check for inactive cost centre and/or account codes
                if (!cmbDetailBankCostCentre.SetSelectedString(ARow.BankCostCentre, -1))
                {
                    MessageBox.Show(String.Format(Catalog.GetString("Batch {0} - the Cost Centre: '{1}' is no longer active and so cannot be used."),
                            ARow.BatchNumber,
                            ARow.BankCostCentre),
                        Catalog.GetString("Gift Batch"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                if (!cmbDetailBankAccountCode.SetSelectedString(ARow.BankAccountCode, -1))
                {
                    MessageBox.Show(String.Format(Catalog.GetString("Batch {0} - the Bank Account: '{1}' is no longer active and so cannot be used."),
                            ARow.BatchNumber,
                            ARow.BankAccountCode),
                        Catalog.GetString("Gift Batch"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void RefreshCurrencyRelatedControls()
        {
            string CurrencyCode = FPreviouslySelectedDetailRow.CurrencyCode;

            txtDetailHashTotal.CurrencyCode = CurrencyCode;
            ((TFrmGiftBatch)ParentForm).GetTransactionsControl().UpdateCurrencySymbols(CurrencyCode);

            txtDetailExchangeRateToBase.NumberValueDecimal = FPreviouslySelectedDetailRow.ExchangeRateToBase;
            //txtDetailExchangeRateToBase.Enabled =
            //    (FPreviouslySelectedDetailRow.ExchangeRateToBase != DEFAULT_CURRENCY_EXCHANGE);

            btnGetSetExchangeRate.Enabled = (CurrencyCode != FLedgerBaseCurrency);

            // Note from AlanP Jan 2015:
            // We used to put the focus on the Get/Set button as the text in the currency box changed.
            // This was bad for two reasons
            //  1. Some currencies do not have a unique first letter.  So as a typist you would need to type two or even three letters to select your currency.
            //     This was impossible once the focus had shifted to the button.
            //  2. It was worse than that.  Once the focus is on the button Windows has this great 'feature' that typing a letter perofrms the
            //     action of ALT+letter without needing to press the ALT key!  So very unexpected things happened.
            //if (AFromUserAction && btnGetSetExchangeRate.Enabled)
            //{
            //    btnGetSetExchangeRate.Focus();
            //}
        }

        private void ShowTransactionTab(Object sender, EventArgs e)
        {
            if ((grdDetails.Rows.Count > 1) && ValidateAllData(false, TErrorProcessingMode.Epm_All))
            {
                ((TFrmGiftBatch)ParentForm).SelectTab(TFrmGiftBatch.eGiftTabs.Transactions);
            }
        }

        /// <summary>
        /// Re-show the specified row
        /// </summary>
        /// <param name="AModifiedBatchRow"></param>
        /// <param name="ARedisplay"></param>
        public void UndoModifiedBatchRow(AGiftBatchRow AModifiedBatchRow, bool ARedisplay)
        {
            //Check if new row or not
            if (AModifiedBatchRow.RowState == DataRowState.Added)
            {
                return;
            }

            AModifiedBatchRow.RejectChanges();

            if (ARedisplay)
            {
                ShowDetails(AModifiedBatchRow);
            }
        }

        /// <summary>
        /// add a new batch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewRow(System.Object sender, EventArgs e)
        {
            GetDataFromControls();

            if (!TExtraGiftBatchChecks.CanContinueWithAnyExWorkers(TExtraGiftBatchChecks.GiftBatchAction.NEWBATCH, FMainDS, FPetraUtilsObject))
            {
                return;
            }

            if (CreateNewAGiftBatch())
            {
                if (!EnsureNewBatchIsVisible())
                {
                    return;
                }

                pnlDetails.Enabled = true;

                // NOTE: we need to suppress change detection here because otherwise the DateChanged event fires off
                //   It would not normally be a problem although it can give strange effects of focussing the date box AND the description (!)
                //   and also may sometimes give problems with running some tests.
                //  So do not remove change detection suppression so we do not run UpdateGiftBatchPeriod()
                FPetraUtilsObject.SuppressChangeDetection = true;
                FPreviouslySelectedDetailRow.GlEffectiveDate = FDefaultDate;
                dtpDetailGlEffectiveDate.Date = FDefaultDate;
                FPetraUtilsObject.SuppressChangeDetection = false;

                Int32 yearNumber = 0;
                Int32 periodNumber = 0;

                if (GetAccountingYearPeriodByDate(FLedgerNumber, FDefaultDate, out yearNumber, out periodNumber))
                {
                    FPreviouslySelectedDetailRow.BatchPeriod = periodNumber;
                }

                UpdateRecordNumberDisplay();

                ((TFrmGiftBatch)ParentForm).SaveChanges();
            }
        }

        private bool EnsureNewBatchIsVisible()
        {
            // Can we see the new row, bearing in mind we have filtering that the standard filter code does not know about?
            DataView dv = ((DevAge.ComponentModel.BoundDataView)grdDetails.DataSource).DataView;
            Int32 RowNumberGrid = DataUtilities.GetDataViewIndexByDataTableIndex(dv,
                FMainDS.AGiftBatch,
                FMainDS.AGiftBatch.Rows.Count - 1) + 1;

            if (RowNumberGrid < 1)
            {
                MessageBox.Show(
                    Catalog.GetString(
                        "The new row has been added but the filter may be preventing it from being displayed. The filter will be reset."),
                    Catalog.GetString("New Batch"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (!FLoadAndFilterLogicObject.StatusEditing)
                {
                    FLoadAndFilterLogicObject.StatusEditing = true;
                }

                //Set year and period to correct value
                FLoadAndFilterLogicObject.YearIndex = 0;
                FLoadAndFilterLogicObject.PeriodIndex = 0;

                FFilterAndFindObject.FilterPanelControls.ClearAllDiscretionaryFilters();

                if (SelectDetailRowByDataTableIndex(FMainDS.AGiftBatch.Rows.Count - 1))
                {
                    // Good - we found the row so now we need to do the other stuff to the new record
                    txtDetailBatchDescription.Text = MCommonResourcestrings.StrPleaseEnterDescription;
                    txtDetailBatchDescription.Focus();
                }
                else
                {
                    // This is not supposed to happen!!
                    MessageBox.Show(
                        Catalog.GetString(
                            "The filter was reset but unexpectedly the new batch is not in the list. Please close the screen and do not save changes."),
                        Catalog.GetString("New Gift Batch"),
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return false;
                }
            }

            return true;
        }

        private void MethodOfPaymentChanged(object sender, EventArgs e)
        {
            if (FPetraUtilsObject.SuppressChangeDetection || (FPreviouslySelectedDetailRow == null)
                || (GetCurrentBatchRow().BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            FSelectedBatchMethodOfPayment = cmbDetailMethodOfPaymentCode.GetSelectedString();

            if ((FSelectedBatchMethodOfPayment != null) && (FSelectedBatchMethodOfPayment.Length > 0))
            {
                ((TFrmGiftBatch)ParentForm).GetTransactionsControl().UpdateMethodOfPayment(false);
            }
        }

        private void CurrencyChanged(object sender, EventArgs e)
        {
            if (FPetraUtilsObject.SuppressChangeDetection || (FPreviouslySelectedDetailRow == null)
                || (GetCurrentBatchRow().BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            string NewCurrency = cmbDetailCurrencyCode.GetSelectedString();

            if (FPreviouslySelectedDetailRow.CurrencyCode != NewCurrency)
            {
                Console.WriteLine("--- New currency is " + NewCurrency);
                FPreviouslySelectedDetailRow.CurrencyCode = NewCurrency;
                FPreviouslySelectedDetailRow.ExchangeRateToBase = (NewCurrency == FLedgerBaseCurrency) ? 1.0m : 0.0m;

                RefreshCurrencyRelatedControls();
                RecalculateTransactionAmounts();
            }
        }

        private void HashTotalChanged(object sender, EventArgs e)
        {
            TTxtNumericTextBox txn = (TTxtNumericTextBox)sender;

            if (txn.NumberValueDecimal == null)
            {
                return;
            }

            Decimal HashTotal = Convert.ToDecimal(txtDetailHashTotal.NumberValueDecimal);
            Form p = ParentForm;

            if (p != null)
            {
                TUC_GiftTransactions t = ((TFrmGiftBatch)ParentForm).GetTransactionsControl();

                if (t != null)
                {
                    t.UpdateHashTotal(HashTotal);
                }
            }
        }

        /// <summary>
        /// Select a special batch number from outside
        /// </summary>
        /// <param name="ABatchNumber"></param>
        /// <returns>True if the record is displayed in the grid, False otherwise</returns>
        public bool SelectBatchNumber(Int32 ABatchNumber)
        {
            for (int i = 0; (i < FMainDS.AGiftBatch.Rows.Count); i++)
            {
                if (FMainDS.AGiftBatch[i].BatchNumber == ABatchNumber)
                {
                    return SelectDetailRowByDataTableIndex(i);
                }
            }

            return false;
        }

        private void ValidateDataDetailsManual(AGiftBatchRow ARow)
        {
            if ((ARow == null) || (ARow.BatchStatus != MFinanceConstants.BATCH_UNPOSTED))
            {
                return;
            }

            TVerificationResultCollection VerificationResultCollection = FPetraUtilsObject.VerificationResultCollection;

            //Hash total special case in view of the textbox handling
            ParseHashTotal(ARow);

            //Check if the user has made a Bank Cost Centre or Account Code inactive
            //this was removed because of speed issues!
            //TODO: Revisit this
            //RefreshBankCostCentreAndAccountCodes();

            TSharedFinanceValidation_Gift.ValidateGiftBatchManual(this, ARow, ref VerificationResultCollection,
                FValidationControlsDict, FAccountTable, FCostCentreTable);
        }

        private void ParseHashTotal(AGiftBatchRow ARow)
        {
            decimal CorrectHashValue = 0m;

            if (ARow.BatchStatus != MFinanceConstants.BATCH_UNPOSTED)
            {
                return;
            }

            if ((txtDetailHashTotal.NumberValueDecimal != null) && txtDetailHashTotal.NumberValueDecimal.HasValue)
            {
                CorrectHashValue = txtDetailHashTotal.NumberValueDecimal.Value;
            }
            else
            {
                ARow.HashTotal = 0m;
            }

            if (ARow.HashTotal != CorrectHashValue)
            {
                ARow.HashTotal = CorrectHashValue;
                txtDetailHashTotal.NumberValueDecimal = CorrectHashValue;
                FPetraUtilsObject.SetChangedFlag();
            }
        }

        /// <summary>
        /// Focus on grid
        /// </summary>
        public void SetFocusToGrid()
        {
            if ((grdDetails != null) && grdDetails.CanFocus)
            {
                grdDetails.Focus();
            }
        }

        private void DataSource_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            if (grdDetails.CanFocus && (grdDetails.Rows.Count > 1))
            {
                grdDetails.AutoResizeGrid();
            }
        }

        private int GetDataTableRowIndexByPrimaryKeys(int ALedgerNumber, int ABatchNumber)
        {
            int RowPos = 0;
            bool BatchFound = false;

            foreach (DataRowView rowView in FMainDS.AGiftBatch.DefaultView)
            {
                AGiftBatchRow row = (AGiftBatchRow)rowView.Row;

                if ((row.LedgerNumber == ALedgerNumber) && (row.BatchNumber == ABatchNumber))
                {
                    BatchFound = true;
                    break;
                }

                RowPos++;
            }

            if (!BatchFound)
            {
                RowPos = 0;
            }

            //remember grid is out of sync with DataView by 1 because of grid header rows
            return RowPos + 1;
        }

        private bool AllowInactiveFieldValues(ref bool APostingConfirmed)
        {
            //Check for inactive Bank Cost Centre & Account
            string BankCostCentre = FPreviouslySelectedDetailRow.BankCostCentre;
            string BankAccount = FPreviouslySelectedDetailRow.BankAccountCode;

            if (FWarnOfInactiveValuesOnPosting
                && (!FAccountAndCostCentreLogicObject.AccountIsActive(BankAccount)
                    || !FAccountAndCostCentreLogicObject.CostCentreIsActive(BankCostCentre)))
            {
                string msg =
                    string.Format(Catalog.GetString(
                            "Gift batch {0} has an inactive bank cost centre and/or account!{1}{1}Do you want to continue posting batch {0} ?"),
                        FPreviouslySelectedDetailRow.BatchNumber,
                        Environment.NewLine);

                if (MessageBox.Show(msg, Catalog.GetString("Post Gift Batch"), MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return false;
                }

                APostingConfirmed = true;
            }

            return true;
        }

        private Boolean LoadAllBatchData(int ABatchNumber)
        {
            return ((TFrmGiftBatch)ParentForm).EnsureGiftDataPresent(FLedgerNumber, ABatchNumber);
        }

        private void RecalculateTransactionAmounts(decimal ANewExchangeRate = 0)
        {
            string CurrencyCode = FPreviouslySelectedDetailRow.CurrencyCode;

            if (ANewExchangeRate == 0)
            {
                if (CurrencyCode == FLedgerBaseCurrency)
                {
                    ANewExchangeRate = 1;
                }
            }

            ((TFrmGiftBatch)ParentForm).GetTransactionsControl().UpdateBaseAmount(false);
        }

        private void ApplyFilterManual(ref string AFilterString)
        {
            if (FLoadAndFilterLogicObject != null)
            {
                FLoadAndFilterLogicObject.ApplyFilterManual(ref AFilterString);
            }
        }

        private void ExportBatches(System.Object sender, System.EventArgs e)
        {
            if (FPetraUtilsObject.HasChanges)
            {
                // without save the server does not have the current changes, so forbid it.
                MessageBox.Show(Catalog.GetString("Please save changed Data before the Export!"),
                    Catalog.GetString("Export Error"));
                return;
            }

            TFrmGiftBatchExport exportForm = new TFrmGiftBatchExport(FPetraUtilsObject.GetForm());
            exportForm.LedgerNumber = FLedgerNumber;
            exportForm.Show();
        }

        private void UpdateBatchPeriod(object sender, EventArgs e)
        {
            UpdateBatchPeriod(true);
        }

        /// <summary>
        /// Update batch period if necessary
        /// </summary>
        public void UpdateBatchPeriod(bool AFocusOnDate = false)
        {
            if ((FPetraUtilsObject == null) || FPetraUtilsObject.SuppressChangeDetection || (FPreviouslySelectedDetailRow == null))
            {
                return;
            }

            Int32 PeriodNumber = 0;
            Int32 YearNumber = 0;
            DateTime ActualDateValue;

            try
            {
                if (dtpDetailGlEffectiveDate.ValidDate(false))
                {
                    ActualDateValue = dtpDetailGlEffectiveDate.Date.Value;

                    //If invalid date return;
                    if ((ActualDateValue < FStartDateCurrentPeriod) || (ActualDateValue > FEndDateLastForwardingPeriod))
                    {
                        return;
                    }

                    if (FPreviouslySelectedDetailRow.GlEffectiveDate != ActualDateValue)
                    {
                        FPreviouslySelectedDetailRow.GlEffectiveDate = ActualDateValue;

                        // reset exchange rate
                        FPreviouslySelectedDetailRow.ExchangeRateToBase =
                            (FPreviouslySelectedDetailRow.CurrencyCode == FLedgerBaseCurrency) ? 1.0m : 0.0m;

                        RefreshCurrencyRelatedControls();
                        RecalculateTransactionAmounts();
                    }

                    if (GetAccountingYearPeriodByDate(FLedgerNumber, ActualDateValue, out YearNumber, out PeriodNumber))
                    {
                        if (PeriodNumber != FPreviouslySelectedDetailRow.BatchPeriod)
                        {
                            FPreviouslySelectedDetailRow.BatchPeriod = PeriodNumber;

                            //Period has changed, so update transactions DateEntered
                            ((TFrmGiftBatch)ParentForm).GetTransactionsControl().UpdateDateEntered(FPreviouslySelectedDetailRow);

                            if (FLoadAndFilterLogicObject.YearIndex != 0)
                            {
                                FLoadAndFilterLogicObject.YearIndex = 0;
                                FLoadAndFilterLogicObject.PeriodIndex = 1;

                                if (AFocusOnDate)
                                {
                                    dtpDetailGlEffectiveDate.Date = ActualDateValue;
                                    dtpDetailGlEffectiveDate.Focus();
                                }
                            }
                            else if (FLoadAndFilterLogicObject.PeriodIndex != 1)
                            {
                                FLoadAndFilterLogicObject.PeriodIndex = 1;

                                if (AFocusOnDate)
                                {
                                    dtpDetailGlEffectiveDate.Date = ActualDateValue;
                                    dtpDetailGlEffectiveDate.Focus();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
        }

        private bool GetAccountingYearPeriodByDate(Int32 ALedgerNumber, DateTime ADate, out Int32 AYear, out Int32 APeriod)
        {
            return TRemote.MFinance.GL.WebConnectors.GetAccountingYearPeriodByDate(ALedgerNumber, ADate, out AYear, out APeriod);
        }

        /// <summary>
        /// this supports the batch export files from Petra 2.x.
        /// Each line starts with a type specifier, B for batch, J for journal, T for transaction
        /// </summary>
        private void ImportBatches(System.Object sender, System.EventArgs e)
        {
            FImportLogicObject.ImportBatches(TUC_GiftBatches_Import.TGiftImportDataSourceEnum.FromFile);
        }

        /// <summary>
        /// this supports the batch export files from Petra 2.x.
        /// Each line starts with a type specifier, B for batch, J for journal, T for transaction
        /// </summary>
        private void ImportFromClipboard(System.Object sender, System.EventArgs e)
        {
            FImportLogicObject.ImportBatches(TUC_GiftBatches_Import.TGiftImportDataSourceEnum.FromClipboard);
        }

        /// <summary>
        /// Imports a transactions file
        /// </summary>
        public bool ImportTransactions(TUC_GiftBatches_Import.TGiftImportDataSourceEnum AImportDataSource)
        {
            if (!FLoadAndFilterLogicObject.StatusEditing)
            {
                FLoadAndFilterLogicObject.StatusEditing = true;
            }

            bool bSuccess = FImportLogicObject.ImportTransactions(FPreviouslySelectedDetailRow, AImportDataSource);

            if (bSuccess)
            {
                // We need to update the last transaction number for the batch
                ParentForm.Cursor = Cursors.WaitCursor;

                FMainDS.AGiftBatch.Merge(TRemote.MFinance.Gift.WebConnectors.LoadAGiftBatchSingle(
                        FLedgerNumber, FPreviouslySelectedDetailRow.BatchNumber).AGiftBatch);
                FMainDS.AGiftBatch.AcceptChanges();

                ParentForm.Cursor = Cursors.Default;
            }

            return bSuccess;
        }

        private void PostBatch(System.Object sender, EventArgs e)
        {
            bool Success = false;

            bool LoadDialogVisible = false;
            TFrmStatusDialog dlgStatus = new TFrmStatusDialog(FPetraUtilsObject.GetForm());

            if (GetSelectedRowIndex() < 0)
            {
                return; // Oops - there's no selected row.
            }

            try
            {
                bool postingAlreadyConfirmed = false;

                Cursor = Cursors.WaitCursor;

                dlgStatus.Show();
                LoadDialogVisible = true;
                dlgStatus.Heading = String.Format(Catalog.GetString("Batch {0}"), FSelectedBatchNumber);
                dlgStatus.CurrentStatus = Catalog.GetString("Loading gifts ready for posting...");

                if (!LoadAllBatchData(FSelectedBatchNumber))
                {
                    Cursor = Cursors.Default;
                    MessageBox.Show(Catalog.GetString("The Gift Batch is empty!"), Catalog.GetString("Posting failed"),
                        MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    dlgStatus.Close();
                    LoadDialogVisible = false;
                    return;
                }

                dlgStatus.CurrentStatus = Catalog.GetString("Checking for inactive values...");

                if (!AllowInactiveFieldValues(ref postingAlreadyConfirmed))
                {
                    dlgStatus.Close();
                    LoadDialogVisible = false;
                    return;
                }

                dlgStatus.Close();
                LoadDialogVisible = false;

                Success = FPostingLogicObject.PostBatch(FPreviouslySelectedDetailRow,
                    postingAlreadyConfirmed,
                    FWarnOfInactiveValuesOnPosting,
                    FDonorZeroIsValid,
                    FRecipientZeroIsValid);

                if (Success)
                {
                    // Posting succeeded so now deal with gift receipting ...
                    GiftBatchTDS PostedGiftTDS = TRemote.MFinance.Gift.WebConnectors.LoadAGiftBatchAndRelatedData(FLedgerNumber,
                        FSelectedBatchNumber,
                        false);

                    FReceiptingLogicObject.PrintGiftBatchReceipts(PostedGiftTDS);

                    // Now we need to get the data back from the server to pick up all the changes
                    RefreshAllData();

                    if (FPetraUtilsObject.HasChanges)
                    {
                        ((TFrmGiftBatch)ParentForm).SaveChangesManual();
                    }
                }
            }
            catch (Exception ex)
            {
                TLogging.LogException(ex, Utilities.GetMethodSignature());
                throw;
            }
            finally
            {
                if (LoadDialogVisible)
                {
                    dlgStatus.Close();
                    LoadDialogVisible = false;
                }

                Cursor = Cursors.Default;
            }
        }

        private void ReverseGiftBatch(System.Object sender, System.EventArgs e)
        {
            ((TFrmGiftBatch)ParentForm).GetTransactionsControl().ReverseGiftBatch(null, null);
        }

        private void FieldAdjustment(System.Object sender, System.EventArgs e)
        {
            TFrmGiftFieldAdjustment FieldAdjustmentForm = new TFrmGiftFieldAdjustment(ParentForm);

            FieldAdjustmentForm.LedgerNumber = FLedgerNumber;

            FieldAdjustmentForm.ShowDialog();
        }

        private void SetExchangeRateValue(Object sender, EventArgs e)
        {
            TFrmSetupDailyExchangeRate setupDailyExchangeRate =
                new TFrmSetupDailyExchangeRate(FPetraUtilsObject.GetForm());

            decimal selectedExchangeRate;
            DateTime selectedEffectiveDate;
            int selectedEffectiveTime;

            if (setupDailyExchangeRate.ShowDialog(
                    FLedgerNumber,
                    dtpDetailGlEffectiveDate.Date.Value,
                    cmbDetailCurrencyCode.GetSelectedString(),
                    (txtDetailExchangeRateToBase.NumberValueDecimal == null) ? 0.0m : txtDetailExchangeRateToBase.NumberValueDecimal.Value,
                    out selectedExchangeRate,
                    out selectedEffectiveDate,
                    out selectedEffectiveTime) == DialogResult.Cancel)
            {
                return;
            }

            if (FPreviouslySelectedDetailRow.ExchangeRateToBase != selectedExchangeRate)
            {
                FPreviouslySelectedDetailRow.ExchangeRateToBase = selectedExchangeRate;

                RefreshCurrencyRelatedControls();
                RecalculateTransactionAmounts(selectedExchangeRate);

                FPetraUtilsObject.VerificationResultCollection.Clear();
            }
        }

        private void CancelRecord(System.Object sender, EventArgs e)
        {
            int CurrentlySelectedRow = grdDetails.GetFirstHighlightedRowIndex();

            // load all data for batch
            LoadAllBatchData(FSelectedBatchNumber);

            FCancelLogicObject.CancelBatch(FPreviouslySelectedDetailRow);

            SelectRowInGrid(CurrentlySelectedRow);

            UpdateRecordNumberDisplay();
        }
    }
}