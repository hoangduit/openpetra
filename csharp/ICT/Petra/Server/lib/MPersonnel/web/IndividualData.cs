//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
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
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Xml;
using System.IO;
using GNU.Gettext;

using Ict.Common;
using Ict.Common.IO;
using Ict.Common.Data;
using Ict.Common.DB;
using Ict.Common.Verification;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared.MPersonnel.Person;
using Ict.Petra.Shared.MPersonnel.Personnel.Data;
using Ict.Petra.Server.MPersonnel.Personnel.Data.Access;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Server.App.Core.Security;

namespace Ict.Petra.Server.MPersonnel.Person.DataElements.WebConnectors
{
    /// <summary>
    /// Web Connector for the Individual Data of a Person.
    /// </summary>
    public class TIndividualDataWebConnector
    {
        /// <summary>
        /// Passes data as a Typed DataSet to the caller, containing a DataTable that corresponds with <paramref name="AIndivDataItem"></paramref>.
        /// </summary>
        /// <remarks>Starts and ends a DB Transaction automatically if there isn't one running yet.</remarks>
        /// <param name="APartnerKey">PartnerKey of the Person to load data for.</param>
        /// <param name="AIndivDataItem">The Individual Data Item for which data should be returned.</param>
        /// <returns></returns>
        [RequireModulePermission("PERSONNEL")]
        public static IndividualDataTDS GetData(Int64 APartnerKey, TIndividualDataItemEnum AIndivDataItem)
        {
            IndividualDataTDS ReturnValue;
            TDBTransaction ReadTransaction;
            Boolean NewTransaction;

            ReadTransaction = DBAccess.GDBAccessObj.GetNewOrExistingTransaction(IsolationLevel.RepeatableRead,
                TEnforceIsolationLevel.eilMinimum,
                out NewTransaction);
            ReturnValue = GetData(APartnerKey, AIndivDataItem, ReadTransaction);

            if (NewTransaction)
            {
                DBAccess.GDBAccessObj.CommitTransaction();
#if DEBUGMODE
                if (TLogging.DL >= 7)
                {
                    Console.WriteLine("TIndividualDataWebConnector.GetData: committed own transaction.");
                }
#endif
            }

            return ReturnValue;
        }

        /// <summary>
        /// Passes data as a Typed DataSet to the caller, containing a DataTable that corresponds with <paramref name="AIndivDataItem"></paramref>.
        /// </summary>
        /// <param name="APartnerKey">PartnerKey of the Person to load data for.</param>
        /// <param name="AIndivDataItem">The Individual Data Item for which data should be returned.</param>
        /// <param name="AReadTransaction"></param>
        /// <returns>A Typed DataSet containing a DataTable that corresponds with <paramref name="AIndivDataItem"></paramref>.</returns>
        private static IndividualDataTDS GetData(Int64 APartnerKey, TIndividualDataItemEnum AIndivDataItem, TDBTransaction AReadTransaction)
        {
            IndividualDataTDS IndividualDataDS = new IndividualDataTDS("IndividualData");   // create the IndividualDataTDS DataSet that will later be passed to the Client
            IndividualDataTDSMiscellaneousDataTable MiscellaneousDataDT;
            IndividualDataTDSMiscellaneousDataRow MiscellaneousDataDR;

            #region Create 'Miscellaneous' DataRow

            MiscellaneousDataDT = IndividualDataDS.MiscellaneousData;
            MiscellaneousDataDR = MiscellaneousDataDT.NewRowTyped(false);
            MiscellaneousDataDR.PartnerKey = APartnerKey;

            MiscellaneousDataDT.Rows.Add(MiscellaneousDataDR);

            #endregion

            switch (AIndivDataItem)
            {
                case TIndividualDataItemEnum.idiSummary:
                    BuildSummaryData(APartnerKey, ref IndividualDataDS, AReadTransaction);

                    CalculateItemCounts(MiscellaneousDataDR, AReadTransaction);
                    break;

                case TIndividualDataItemEnum.idiPersonalLanguages:
                    PmPersonLanguageAccess.LoadViaPPerson(IndividualDataDS, APartnerKey, AReadTransaction);
                    break;

                case TIndividualDataItemEnum.idiSpecialNeeds:
                    PmSpecialNeedAccess.LoadByPrimaryKey(IndividualDataDS, APartnerKey, AReadTransaction);
                    break;

                    // TODO: work on all cases/load data for all Individual Data items
            }

            return IndividualDataDS;
        }

        private static void BuildSummaryData(Int64 APartnerKey, ref IndividualDataTDS AIndividualDataDS, TDBTransaction AReadTransaction)
        {
            string StrNotAvailable = Catalog.GetString("Not Available");
            IndividualDataTDSSummaryDataTable SummaryDT;
            IndividualDataTDSSummaryDataRow SummaryDR;
            IndividualDataTDSMiscellaneousDataRow MiscellaneousDataDR = AIndividualDataDS.MiscellaneousData[0];
            PPersonTable PPersonDT;
            PPersonRow PersonDR = null;
            POccupationTable POccupationDT = null;
            PmPassportDetailsTable PmPassportDetailsDT;
            PmStaffDataTable PmStaffDataDT;
            PmStaffDataRow PmStaffDataDR = null;
            PmJobAssignmentTable PmJobAssignmentDT = null;
            PUnitTable PUnitDT = null;
            PmJobAssignmentRow PmJobAssignmentDR;
            IndividualDataTDSJobAssignmentStaffDataCombinedRow JobAssiStaffDataCombDR;
            int JobAssiStaffDataCombKey = 0;

            SummaryDT = new IndividualDataTDSSummaryDataTable();
            SummaryDR = SummaryDT.NewRowTyped(false);

            SummaryDR.PartnerKey = APartnerKey;

            #region Person Info

            PPersonDT = PPersonAccess.LoadByPrimaryKey(APartnerKey, AReadTransaction);

            if (PPersonDT.Rows.Count == 1)
            {
                PersonDR = (PPersonRow)PPersonDT.Rows[0];

                POccupationDT = POccupationAccess.LoadByPrimaryKey(PersonDR.OccupationCode, AReadTransaction);
            }

            if (PersonDR != null)
            {
                SummaryDR.DateOfBirth = PersonDR.DateOfBirth;
                SummaryDR.Gender = PersonDR.Gender;
                SummaryDR.MaritalStatus = PersonDR.MaritalStatus;
            }
            else
            {
                SummaryDR.SetDateOfBirthNull();
                SummaryDR.Gender = StrNotAvailable;
                SummaryDR.MaritalStatus = StrNotAvailable;
            }

            #region Nationality

            PmPassportDetailsDT = PmPassportDetailsAccess.LoadViaPPerson(APartnerKey, AReadTransaction);

            // TODO

            #endregion

            #region Phone and Email (from 'Best Address')

            // TODO

            #endregion

            #endregion

            #region Job/Commitment

            PmStaffDataDT = PmStaffDataAccess.LoadViaPPerson(APartnerKey, AReadTransaction);
            MiscellaneousDataDR.ItemsCountCommitmentPeriods = PmStaffDataDT.Rows.Count;

            if (PmStaffDataDT.Rows.Count > 0)
            {
                foreach (DataRow DR in PmStaffDataDT.Rows)
                {
                    JobAssiStaffDataCombDR = AIndividualDataDS.JobAssignmentStaffDataCombined.NewRowTyped(false);
                    JobAssiStaffDataCombDR.Key = JobAssiStaffDataCombKey++;
                    JobAssiStaffDataCombDR.PartnerKey = APartnerKey;


                    PmStaffDataDR = (PmStaffDataRow)DR;

                    if (!(PmStaffDataDR.IsReceivingFieldNull())
                        && (PmStaffDataDR.ReceivingField != 0))
                    {
                        PUnitDT = PUnitAccess.LoadByPrimaryKey(PmStaffDataDR.ReceivingField, AReadTransaction);

                        JobAssiStaffDataCombDR.FieldKey = PmStaffDataDR.ReceivingField;
                        JobAssiStaffDataCombDR.FieldName = PUnitDT[0].UnitName;
                    }
                    else
                    {
                        JobAssiStaffDataCombDR.FieldKey = 0;
                        JobAssiStaffDataCombDR.FieldName = "[None]";
                    }

                    JobAssiStaffDataCombDR.Position = PmStaffDataDR.JobTitle;
                    JobAssiStaffDataCombDR.FromDate = PmStaffDataDR.StartOfCommitment;
                    JobAssiStaffDataCombDR.ToDate = PmStaffDataDR.EndOfCommitment;

                    AIndividualDataDS.JobAssignmentStaffDataCombined.Rows.Add(JobAssiStaffDataCombDR);
                }
            }
            else
            {
                PmJobAssignmentDT = PmJobAssignmentAccess.LoadViaPPartner(APartnerKey, AReadTransaction);

                if (PmJobAssignmentDT.Rows.Count > 0)
                {
                    foreach (DataRow DR in PmJobAssignmentDT.Rows)
                    {
                        JobAssiStaffDataCombDR = AIndividualDataDS.JobAssignmentStaffDataCombined.NewRowTyped(false);
                        JobAssiStaffDataCombDR.Key = JobAssiStaffDataCombKey++;
                        JobAssiStaffDataCombDR.PartnerKey = APartnerKey;

                        PmJobAssignmentDR = (PmJobAssignmentRow)DR;

                        if (PmJobAssignmentDR.UnitKey != 0)
                        {
                            PUnitDT = PUnitAccess.LoadByPrimaryKey(PmJobAssignmentDR.UnitKey, AReadTransaction);

                            JobAssiStaffDataCombDR.FieldKey = PmJobAssignmentDR.UnitKey;
                            JobAssiStaffDataCombDR.FieldName = PUnitDT[0].UnitName;
                        }
                        else
                        {
                            JobAssiStaffDataCombDR.FieldKey = 0;
                            JobAssiStaffDataCombDR.FieldName = "[None]";
                        }

                        JobAssiStaffDataCombDR.Position = PmJobAssignmentDR.PositionName;
                        JobAssiStaffDataCombDR.FromDate = PmJobAssignmentDR.FromDate;
                        JobAssiStaffDataCombDR.ToDate = PmJobAssignmentDR.ToDate;

                        AIndividualDataDS.JobAssignmentStaffDataCombined.Rows.Add(JobAssiStaffDataCombDR);
                    }
                }

                // TODO
            }

            // TODO

            #endregion

            #region Church Info

            // TODO

            #endregion


            SummaryDT.Rows.Add(SummaryDR);

            // Add Row to 'SummaryData' DataTable in Typed DataSet 'IndividualDataTDS'
            AIndividualDataDS.Merge(SummaryDT);
        }

        private static void CalculateItemCounts(IndividualDataTDSMiscellaneousDataRow AMiscellaneousDataDR, TDBTransaction AReadTransaction)
        {
            Int64 PartnerKey = AMiscellaneousDataDR.PartnerKey;

            // Note: Commitment Records are counted already in BuildSummaryData and therefore don't need to be done here.

            AMiscellaneousDataDR.ItemsCountPassportDetails = PmPassportDetailsAccess.CountViaPPerson(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountPersonalDocuments = PmDocumentAccess.CountViaPPerson(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountProfessionalAreas = PmPersonQualificationAccess.CountViaPPerson(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountPersonalLanguages = PmPersonLanguageAccess.CountViaPPerson(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountPersonalAbilities = PmPersonAbilityAccess.CountViaPPerson(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountPreviousExperience = PmPastExperienceAccess.CountViaPPerson(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountCommitmentPeriods = PmStaffDataAccess.CountViaPPerson(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountJobAssignments = PmJobAssignmentAccess.CountViaPPartner(PartnerKey, AReadTransaction);
            AMiscellaneousDataDR.ItemsCountProgressReports = PmPersonEvaluationAccess.CountViaPPerson(PartnerKey, AReadTransaction);
        }

        /// <summary>
        /// Saves data from the Individual Data UserControl (contained in a DataTable).
        /// </summary>
        /// <param name="AInspectDS">DataSet for the Personnel Individual Data</param>
        /// <param name="APartnerEditInspectDS">DataSet for the whole Partner Edit screen</param>
        /// <param name="ASubmitChangesTransaction">Current Transaction</param>
        /// <param name="AVerificationResult">Nil if all verifications are OK and all DB calls
        /// succeded, otherwise filled with 1..n TVerificationResult objects
        /// (can also contain DB call exceptions)</param>
        /// <returns>true if all verifications are OK and all DB calls succeeded, false if
        /// any verification or DB call failed
        /// </returns>
        [NoRemoting]
        public static TSubmitChangesResult SubmitChangesServerSide(ref IndividualDataTDS AInspectDS,
            ref PartnerEditTDS APartnerEditInspectDS,
            TDBTransaction ASubmitChangesTransaction,
            out TVerificationResultCollection AVerificationResult)
        {
            TSubmitChangesResult SubmissionResult;
            TVerificationResultCollection SingleVerificationResultCollection;
            PmSpecialNeedTable PmSpecialNeedTableSubmit;
            PmPersonLanguageTable PmPersonLanguageTableSubmit;

            AVerificationResult = new TVerificationResultCollection();

            if (AInspectDS != null)
            {
                SubmissionResult = TSubmitChangesResult.scrOK;

                if (AInspectDS.Tables.Contains(PmSpecialNeedTable.GetTableName())
                    && (AInspectDS.PmSpecialNeed.Rows.Count > 0))
                {
                    PmSpecialNeedTableSubmit = AInspectDS.PmSpecialNeed;

                    if (PmSpecialNeedAccess.SubmitChanges(PmSpecialNeedTableSubmit, ASubmitChangesTransaction,
                            out SingleVerificationResultCollection))
                    {
                        SubmissionResult = TSubmitChangesResult.scrOK;

                        // Need to merge this Table back into APartnerEditInspectDS so the updated s_modification_id_c is returned correctly to the Partner Edit screen!
                        APartnerEditInspectDS.Tables[PmSpecialNeedTable.GetTableName()].Merge(AInspectDS.PmSpecialNeed);
                    }
                    else
                    {
                        SubmissionResult = TSubmitChangesResult.scrError;
                        AVerificationResult.AddCollection(SingleVerificationResultCollection);
#if DEBUGMODE
                        if (TLogging.DL >= 9)
                        {
                            Console.WriteLine(Messages.BuildMessageFromVerificationResult(
                                    "TIndividualDataWebConnector.SubmitChangesServerSide VerificationResult: ", AVerificationResult));
                        }
#endif
                    }
                }

                if (AInspectDS.Tables.Contains(PmPersonLanguageTable.GetTableName())
                    && (AInspectDS.PmPersonLanguage.Rows.Count > 0))
                {
                    PmPersonLanguageTableSubmit = AInspectDS.PmPersonLanguage;

                    if (PmPersonLanguageAccess.SubmitChanges(PmPersonLanguageTableSubmit, ASubmitChangesTransaction,
                            out SingleVerificationResultCollection))
                    {
                        SubmissionResult = TSubmitChangesResult.scrOK;

                        // Need to merge this Table back into APartnerEditInspectDS so the updated s_modification_id_c is returned correctly to the Partner Edit screen!
                        APartnerEditInspectDS.Tables[PmPersonLanguageTable.GetTableName()].Merge(AInspectDS.PmPersonLanguage);
                    }
                    else
                    {
                        SubmissionResult = TSubmitChangesResult.scrError;
                        AVerificationResult.AddCollection(SingleVerificationResultCollection);
#if DEBUGMODE
                        if (TLogging.DL >= 9)
                        {
                            Console.WriteLine(Messages.BuildMessageFromVerificationResult(
                                    "TIndividualDataWebConnector.SubmitChangesServerSide VerificationResult: ", AVerificationResult));
                        }
#endif
                    }
                }
            }
            else
            {
#if DEBUGMODE
                if (TLogging.DL >= 8)
                {
                    Console.WriteLine("AInspectDS = nil!");
                }
#endif
                SubmissionResult = TSubmitChangesResult.scrNothingToBeSaved;
            }

            return SubmissionResult;
        }
    }
}