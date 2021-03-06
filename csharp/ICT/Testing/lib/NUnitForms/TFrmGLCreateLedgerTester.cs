﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       wolfgangu
//
// Copyright 2004-2011 by OM International
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

using System;
using System.IO;
using System.Windows.Forms;

using Ict.Common;
using Ict.Common.IO;
using Ict.Petra.Client.MFinance.Gui;
using Ict.Petra.Client.MFinance.Gui.GL;
using Ict.Petra.Client.MFinance.Gui.Setup;
using Ict.Testing.NUnitForms;
using Ict.Testing.NUnitPetraClient;
using NUnit.Extensions.Forms;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Ict.Testing.NUnitForms
{
    /// <summary>
    /// Description of TFrmGLCreateLedgerTester.
    /// </summary>
    public sealed class TFrmGLCreateLedgerTester
    {
        /// <summary>
        /// ...
        /// </summary>
        public TCmbAutoPopulatedTester cmbCountryCode;
        /// <summary>
        /// ...
        /// </summary>
        public TCmbAutoPopulatedTester cmbBaseCurrency;
        /// <summary>
        /// ...
        /// </summary>
        public TCmbAutoPopulatedTester cmbIntlCurrency;
        /// <summary>
        /// ...
        /// </summary>
        public TextBoxTester txtLedgerName;

        /// <summary>
        /// ...
        /// </summary>
        public TextBoxTester dtpCalendarStartDate;
        /// <summary>
        /// ...
        /// </summary>
        public NumericUpDownTester nudLedgerNumber;
        /// <summary>
        /// ...
        /// </summary>
        public NumericUpDownTester nudNumberOfPeriods;
        /// <summary>
        /// ...
        /// </summary>
        public NumericUpDownTester nudCurrentPeriod;
        /// <summary>
        /// ...
        /// </summary>
        public NumericUpDownTester nudNumberOfFwdPostingPeriods;

        /// <summary>
        /// ...
        /// </summary>
        public ToolStripButtonTester tbbCreate;

        TFrmGLCreateLedgerDialog TFrmGLCreateLedgerDialog;

        private static TFrmGLCreateLedgerTester instance = new TFrmGLCreateLedgerTester();


        /// <summary>
        /// ...
        /// </summary>
        public static TFrmGLCreateLedgerTester Instance {
            get
            {
                return instance;
            }
        }

        private TFrmGLCreateLedgerTester()
        {
            TFrmGLCreateLedgerDialog = new TFrmGLCreateLedgerDialog(null);

            nudLedgerNumber = new NumericUpDownTester("nudLedgerNumber", TFrmGLCreateLedgerDialog);

            txtLedgerName = new TextBoxTester("txtLedgerName", TFrmGLCreateLedgerDialog);

            cmbCountryCode = new TCmbAutoPopulatedTester("cmbCountryCode", TFrmGLCreateLedgerDialog);
            cmbBaseCurrency = new TCmbAutoPopulatedTester("cmbBaseCurrency", TFrmGLCreateLedgerDialog);
            cmbIntlCurrency = new TCmbAutoPopulatedTester("cmbIntlCurrency", TFrmGLCreateLedgerDialog);

            // TextBoxTester dtpCalendarStartDate = new TextBoxTester("dtpCalendarStartDate", TFrmGLCreateLedgerDialog);

            nudNumberOfPeriods = new NumericUpDownTester("nudNumberOfPeriods", TFrmGLCreateLedgerDialog);
            nudCurrentPeriod = new NumericUpDownTester("nudCurrentPeriod", TFrmGLCreateLedgerDialog);
            nudNumberOfFwdPostingPeriods = new NumericUpDownTester("nudNumberOfFwdPostingPeriods", TFrmGLCreateLedgerDialog);

            tbbCreate = new ToolStripButtonTester("tbbCreate", TFrmGLCreateLedgerDialog);
        }
    }
}