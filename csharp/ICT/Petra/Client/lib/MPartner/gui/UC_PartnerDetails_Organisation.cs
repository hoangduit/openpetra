// auto generated with nant generateWinforms from UC_PartnerDetails_Organisation.yaml and template usercontrol
//
// DO NOT edit manually, DO NOT edit with the designer
//
//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       auto generated
//
// Copyright 2004-2010 by OM International
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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using Ict.Petra.Shared;
using System.Resources;
using System.Collections.Specialized;
using Mono.Unix;
using Ict.Common;
using Ict.Common.Verification;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Common.Controls;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Shared.MPartner.Partner.Data;

namespace Ict.Petra.Client.MPartner.Gui
{

  /// auto generated user control
  public partial class TUC_PartnerDetails_Organisation: System.Windows.Forms.UserControl, Ict.Petra.Client.CommonForms.IFrmPetra
  {
    private TFrmPetraEditUtils FPetraUtilsObject;

    private Ict.Petra.Shared.MPartner.Partner.Data.PartnerEditTDS FMainDS;

    /// constructor
    public TUC_PartnerDetails_Organisation() : base()
    {
      //
      // Required for Windows Form Designer support
      //
      InitializeComponent();
      #region CATALOGI18N

      // this code has been inserted by GenerateI18N, all changes in this region will be overwritten by GenerateI18N
      this.lblPreviousName.Text = Catalog.GetString("Previous Name:");
      this.lblLocalName.Text = Catalog.GetString("Local Name:");
      this.grpNames.Text = Catalog.GetString("Names");
      this.chkReligious.Text = Catalog.GetString("Religious Organisation");
      this.chkFoundation.Text = Catalog.GetString("Foundation");
      this.lblBusinessCode.Text = Catalog.GetString("Business Code:");
      this.lblLanguageCode.Text = Catalog.GetString("Language Code:");
      this.lblAcquisitionCode.Text = Catalog.GetString("Acquisition Code:");
      this.txtContactPartnerKey.ButtonText = Catalog.GetString("Find");
      this.lblContactPartnerKey.Text = Catalog.GetString("Contact Partner:");
      this.grpMisc.Text = Catalog.GetString("Miscellaneous");
      #endregion

      this.txtPreviousName.Font = TAppSettingsManager.GetDefaultBoldFont();
      this.txtLocalName.Font = TAppSettingsManager.GetDefaultBoldFont();
    }

    /// helper object for the whole screen
    public TFrmPetraEditUtils PetraUtilsObject
    {
        set
        {
            FPetraUtilsObject = value;
        }
    }

    /// dataset for the whole screen
    public Ict.Petra.Shared.MPartner.Partner.Data.PartnerEditTDS MainDS
    {
        set
        {
            FMainDS = value;
        }
    }

    /// <summary>todoComment</summary>
    public event System.EventHandler DataLoadingStarted;

    /// <summary>todoComment</summary>
    public event System.EventHandler DataLoadingFinished;

    /// needs to be called after FMainDS and FPetraUtilsObject have been set
    public void InitUserControl()
    {
        FPetraUtilsObject.SetStatusBarText(txtPreviousName, Catalog.GetString("Enter the previously used Surname (eg before marriage)"));
        FPetraUtilsObject.SetStatusBarText(txtLocalName, Catalog.GetString("Enter a short name for this partner in your local language"));
        FPetraUtilsObject.SetStatusBarText(chkReligious, Catalog.GetString("Select if this is a religious Organisation"));
        FPetraUtilsObject.SetStatusBarText(chkFoundation, Catalog.GetString("Select if this is a Foundation"));
        FPetraUtilsObject.SetStatusBarText(cmbBusinessCode, Catalog.GetString("Select a business code"));
        cmbBusinessCode.InitialiseUserControl();
        FPetraUtilsObject.SetStatusBarText(cmbLanguageCode, Catalog.GetString("Select the partner's preferred language"));
        cmbLanguageCode.InitialiseUserControl();
        FPetraUtilsObject.SetStatusBarText(cmbAcquisitionCode, Catalog.GetString("Select a method-of-acquisition code"));
        cmbAcquisitionCode.InitialiseUserControl();
        FPetraUtilsObject.SetStatusBarText(txtContactPartnerKey, Catalog.GetString("Enter the partner who is to be addressed in correspondence"));
        InitializeManualCode();

        if(FMainDS != null)
        {
            ShowData(FMainDS.POrganisation[0]);
        }
    }

    private void ShowData(POrganisationRow ARow)
    {
        FPetraUtilsObject.DisableDataChangedEvent();
        if (FMainDS.PPartner == null || ((FMainDS.PPartner.Rows.Count > 0) && (FMainDS.PPartner[0].IsPreviousNameNull())))
        {
            txtPreviousName.Text = String.Empty;
        }
        else
        {
            if (FMainDS.PPartner.Rows.Count > 0)
            {
                txtPreviousName.Text = FMainDS.PPartner[0].PreviousName;
            }
        }
        if (FMainDS.PPartner == null || ((FMainDS.PPartner.Rows.Count > 0) && (FMainDS.PPartner[0].IsPartnerShortNameLocNull())))
        {
            txtLocalName.Text = String.Empty;
        }
        else
        {
            if (FMainDS.PPartner.Rows.Count > 0)
            {
                txtLocalName.Text = FMainDS.PPartner[0].PartnerShortNameLoc;
            }
        }
        if (ARow.IsReligiousNull())
        {
            chkReligious.Checked = false;
        }
        else
        {
            chkReligious.Checked = ARow.Religious;
        }
        if (ARow.IsFoundationNull())
        {
            chkFoundation.Checked = false;
        }
        else
        {
            chkFoundation.Checked = ARow.Foundation;
        }
        if (ARow.IsBusinessCodeNull())
        {
            cmbBusinessCode.SelectedIndex = -1;
        }
        else
        {
            cmbBusinessCode.SetSelectedString(ARow.BusinessCode);
        }
        if (FMainDS.PPartner == null || ((FMainDS.PPartner.Rows.Count > 0) && (FMainDS.PPartner[0].IsLanguageCodeNull())))
        {
            cmbLanguageCode.SelectedIndex = -1;
        }
        else
        {
            if (FMainDS.PPartner.Rows.Count > 0)
            {
                cmbLanguageCode.SetSelectedString(FMainDS.PPartner[0].LanguageCode);
            }
        }
        if (FMainDS.PPartner == null || ((FMainDS.PPartner.Rows.Count > 0) && (FMainDS.PPartner[0].IsAcquisitionCodeNull())))
        {
            cmbAcquisitionCode.SelectedIndex = -1;
        }
        else
        {
            if (FMainDS.PPartner.Rows.Count > 0)
            {
                cmbAcquisitionCode.SetSelectedString(FMainDS.PPartner[0].AcquisitionCode);
            }
        }
        if (ARow.IsContactPartnerKeyNull())
        {
            txtContactPartnerKey.Text = String.Empty;
        }
        else
        {
            txtContactPartnerKey.Text = String.Format("{0:0000000000}", ARow.ContactPartnerKey);
        }
        FPetraUtilsObject.EnableDataChangedEvent();
    }

    private void GetDataFromControls(POrganisationRow ARow)
    {
        if ((FMainDS.PPartner != null) && (FMainDS.PPartner.Rows.Count > 0))
        {
            if (txtPreviousName.Text.Length == 0)
            {
                FMainDS.PPartner[0].SetPreviousNameNull();
            }
            else
            {
                FMainDS.PPartner[0].PreviousName = txtPreviousName.Text;
            }
        }
        if ((FMainDS.PPartner != null) && (FMainDS.PPartner.Rows.Count > 0))
        {
            if (txtLocalName.Text.Length == 0)
            {
                FMainDS.PPartner[0].SetPartnerShortNameLocNull();
            }
            else
            {
                FMainDS.PPartner[0].PartnerShortNameLoc = txtLocalName.Text;
            }
        }
        ARow.Religious = chkReligious.Checked;
        ARow.Foundation = chkFoundation.Checked;
        if (cmbBusinessCode.SelectedIndex == -1)
        {
            ARow.SetBusinessCodeNull();
        }
        else
        {
            ARow.BusinessCode = cmbBusinessCode.GetSelectedString();
        }
        if ((FMainDS.PPartner != null) && (FMainDS.PPartner.Rows.Count > 0))
        {
            if (cmbLanguageCode.SelectedIndex == -1)
            {
                FMainDS.PPartner[0].SetLanguageCodeNull();
            }
            else
            {
                FMainDS.PPartner[0].LanguageCode = cmbLanguageCode.GetSelectedString();
            }
        }
        if ((FMainDS.PPartner != null) && (FMainDS.PPartner.Rows.Count > 0))
        {
            if (cmbAcquisitionCode.SelectedIndex == -1)
            {
                FMainDS.PPartner[0].SetAcquisitionCodeNull();
            }
            else
            {
                FMainDS.PPartner[0].AcquisitionCode = cmbAcquisitionCode.GetSelectedString();
            }
        }
        if (txtContactPartnerKey.Text.Length == 0)
        {
            ARow.SetContactPartnerKeyNull();
        }
        else
        {
            ARow.ContactPartnerKey = Convert.ToInt64(txtContactPartnerKey.Text);
        }
    }

#region Implement interface functions
    /// auto generated
    public void RunOnceOnActivation()
    {
    }

    /// <summary>
    /// Adds event handlers for the appropiate onChange event to call a central procedure
    /// </summary>
    public void HookupAllControls()
    {
    }

    /// auto generated
    public void HookupAllInContainer(Control container)
    {
        FPetraUtilsObject.HookupAllInContainer(container);
    }

    /// auto generated
    public bool CanClose()
    {
        return FPetraUtilsObject.CanClose();
    }

    /// auto generated
    public TFrmPetraUtils GetPetraUtilsObject()
    {
        return (TFrmPetraUtils)FPetraUtilsObject;
    }
#endregion
  }
}