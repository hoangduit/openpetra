// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       berndr
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
using System.Data;
using Ict.Common.Data;
using Ict.Common;
using Ict.Common.DB;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MCommon;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Server.MCommon.Data.Access;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MPersonnel;
using Ict.Petra.Shared.MPersonnel.Units.Data;
using Ict.Petra.Server.MPersonnel.Units.Data.Access;
using Ict.Petra.Shared.RemotedExceptions;
using Ict.Petra.Server.App.ClientDomain;
using Ict.Petra.Server.MCommon;

namespace Ict.Petra.Server.MPersonnel.Units
{
    /// <summary>
    /// Returns DataTables for DB tables in the MPersonnel.Person sub-namespace
    /// that can be cached on the Client side.
    ///
    /// Examples of such tables are tables that form entries of ComboBoxes or Lists
    /// and which would be retrieved numerous times from the Server as UI windows
    /// are opened.
    /// </summary>
    public class TPersonnelCacheable : TCacheableTablesLoader
    {
        /// time when this object was instantiated
        private DateTime FStartTime;

        #region TMPersonnel_Unit_Cacheable

        /// <summary>
        /// constructor
        /// </summary>
        public TPersonnelCacheable() : base()
        {
#if DEBUGMODE
            if (TSrvSetting.DL >= 9)
            {
                Console.WriteLine(this.GetType().FullName + " created: Instance hash is " + this.GetHashCode().ToString());
            }
#endif
            FStartTime = DateTime.Now;
            FCacheableTablesManager = DomainManager.GCacheableTablesManager;
        }

#if DEBUGMODE
        /// destructor
        ~TPersonnelCacheable()
        {
            if (TSrvSetting.DL >= 9)
            {
                Console.WriteLine(this.GetType().FullName + ": Getting collected after " + (new TimeSpan(
                                                                                                DateTime.Now.Ticks -
                                                                                                FStartTime.Ticks)).ToString() + " seconds.");
            }
        }
#endif



        /**
         * Returns a certain cachable DataTable that contains all columns and all
         * rows of a specified table.
         *
         * @comment Uses Ict.Petra.Shared.CacheableTablesManager to store the DataTable
         * once its contents got retrieved from the DB. It returns the cached
         * DataTable from it on subsequent calls, therefore making more no further DB
         * queries!
         *
         * @comment All DataTables are retrieved as Typed DataTables, but are passed
         * out as a normal DataTable. However, this DataTable can be cast by the
         * caller to the appropriate TypedDataTable to have access to the features of
         * a Typed DataTable!
         *
         * @param ACacheableTable Tells what cachable DataTable should be returned.
         * @param AHashCode Hash of the cacheable DataTable that the caller has. '' can
         * be specified to always get a DataTable back (see @return)
         * @param ARefreshFromDB Set to true to reload the cached DataTable from the
         * DB and through that refresh the Table in the Cache with what is now in the
         * DB (this would be done when it is known that the DB Table has changed).
         * The CacheableTablesManager will notify other Clients that they need to
         * retrieve this Cacheable DataTable anew from the PetraServer the next time
         * the Client accesses the Cacheable DataTable. Otherwise set to false.
         * @param AType The Type of the DataTable (useful in case it's a
         * Typed DataTable)
         * @return DataTable If the Hash that got passed in AHashCode doesn't fit the
         * Hash that the CacheableTablesManager has for this cacheable DataTable, the
         * specified DataTable is returned, otherwise nil.
         *
         */
        public DataTable GetStandardCacheableTable(TCacheableUnitsDataElementsTablesEnum ACacheableTable,
            String AHashCode,
            Boolean ARefreshFromDB,
            out System.Type AType)
        {
            TDBTransaction ReadTransaction;
            Boolean NewTransaction;
            String TableName;

            TableName = Enum.GetName(typeof(TCacheableUnitsDataElementsTablesEnum), ACacheableTable);
#if DEBUGMODE
            if (TSrvSetting.DL >= 7)
            {
                Console.WriteLine(this.GetType().FullName + ".GetStandardCacheableTable called with ATableName='" + TableName + "'.");
            }
#endif

            if ((ARefreshFromDB) || ((!DomainManager.GCacheableTablesManager.IsTableCached(TableName))))
            {
                ReadTransaction = DBAccess.GDBAccessObj.GetNewOrExistingTransaction(
                    Ict.Petra.Server.MCommon.MCommonConstants.CACHEABLEDT_ISOLATIONLEVEL,
                    TEnforceIsolationLevel.eilMinimum,
                    out NewTransaction);
                try
                {
                    switch (ACacheableTable)
                    {
//                        case TCacheableUnitsDataElementsTablesEnum.CampaignList:
//                            DataTable TmpTable = PAcquisitionAccess.LoadAll(ReadTransaction);
//                            DomainManager.GCacheableTablesManager.AddOrRefreshCachedTable(TableName, TmpTable, DomainManager.GClientID);
//							break
                        // Unknown Standard Cacheable DataTable


                        default:
                            throw new ECachedDataTableNotImplementedException("Requested Cacheable DataTable '" +
                            Enum.GetName(typeof(TCacheableUnitsDataElementsTablesEnum),
                                ACacheableTable) + "' is not available as a Standard Cacheable Table");
                    }
                }
                finally
                {
                    if (NewTransaction)
                    {
                        DBAccess.GDBAccessObj.CommitTransaction();
#if DEBUGMODE
                        if (TSrvSetting.DL >= 7)
                        {
                            Console.WriteLine(this.GetType().FullName + ".GetStandardCacheableTable: commited own transaction.");
                        }
#endif
                    }
                }
            }

            // Return the DataTable from the Cache only if the Hash is not the same
            return ResultingCachedDataTable(TableName, AHashCode, out AType);
        }

        /**
         * Returns non-standard cachable table 'ConferenceUnits'.
         * DB Table:  p_partner_location, p_partner, p_unit, p_country
         * @comment Used eg. Select Event Dialog
         *
         * @comment Uses Ict.Petra.Shared.CacheableTablesManager to store the DataTable
         * once its contents got retrieved from the DB. It returns the cached
         * DataTable from it on subsequent calls, therefore making more no more DB
         * queries.
         *
         * @comment The DataTables is retrieved as Typed DataTables, but is passed
         * out as a normal DataTable. However, this DataTable can be cast by the
         * caller to the appropriate TypedDataTable to have access to the features of
         * a Typed DataTable!
         *
         * @param ATableName TableName that the returned DataTable should have.
         * @param AHashCode Hash of the cacheable DataTable that the caller has. '' can be
         * specified to always get a DataTable back (see @return)
         * @param ARefreshFromDB Set to true to reload the cached DataTable from the
         * DB and through that refresh the Table in the Cache with what is now in the
         * DB (this would be done when it is known that the DB Table has changed).
         * Otherwise set to false.
         * @return DataTable If the Hash passed in in AHashCode doesn't fit the Hash that
         * the CacheableTablesManager has for this cacheable DataTable, the
         * 'InstalledSitesList' DataTable is returned, otherwise nil.
         *
         */
        public DataTable GetCoferenceUnitsTable(String ATableName, String AHashCode, Boolean ARefreshFromDB, out System.Type AType)
        {
            TDBTransaction ReadTransaction;
            Boolean NewTransaction;
            DataTable TmpTable;

#if DEBUGMODE
            if (TSrvSetting.DL >= 7)
            {
                Console.WriteLine(this.GetType().FullName + ".GetCoferenceUnitsTable called.");
            }
#endif

            if ((ARefreshFromDB) || ((!DomainManager.GCacheableTablesManager.IsTableCached(ATableName))))
            {
                ReadTransaction = DBAccess.GDBAccessObj.GetNewOrExistingTransaction(
                    Ict.Petra.Server.MCommon.MCommonConstants.CACHEABLEDT_ISOLATIONLEVEL,
                    TEnforceIsolationLevel.eilMinimum,
                    out NewTransaction);
                try
                {
                    TmpTable = DBAccess.GDBAccessObj.SelectDT(
                        "SELECT DISTINCT " +
                        PPartnerTable.GetPartnerShortNameDBName() +
                        ", " + PPartnerTable.GetPartnerClassDBName() +
                        ", " + PUnitTable.GetXyzTbdCodeDBName() +
                        ", " + PCountryTable.GetTableDBName() + "." + PCountryTable.GetCountryNameDBName() +
                        ", " + PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetDateEffectiveDBName() +
                        ", " + PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetDateGoodUntilDBName() +
                        ", " + PPartnerTable.GetTableDBName() + "." + PPartnerTable.GetPartnerKeyDBName() +
                        ", " + PUnitTable.GetUnitTypeCodeDBName() +

                        " FROM PUB." + PPartnerTable.GetTableDBName() +
                        ", PUB." + PUnitTable.GetTableDBName() +
                        ", PUB." + PLocationTable.GetTableDBName() +
                        ", PUB." + PPartnerLocationTable.GetTableDBName() +
                        ", PUB." + PCountryTable.GetTableDBName() +

                        " WHERE " +
                        PPartnerTable.GetTableDBName() + "." + PPartnerTable.GetPartnerKeyDBName() + " = " +
                        PUnitTable.GetTableDBName() + "." + PUnitTable.GetPartnerKeyDBName() + " AND " +
                        PPartnerTable.GetTableDBName() + "." + PPartnerTable.GetPartnerKeyDBName() + " = " +
                        PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetPartnerKeyDBName() + " AND " +

                        PLocationTable.GetTableDBName() + "." + PLocationTable.GetSiteKeyDBName() + " = " +
                        PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetSiteKeyDBName() + " AND " +
                        PLocationTable.GetTableDBName() + "." + PLocationTable.GetLocationKeyDBName() + " = " +
                        PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetLocationKeyDBName() + " AND " +
                        PCountryTable.GetTableDBName() + "." + PCountryTable.GetCountryCodeDBName() + " = " +
                        PLocationTable.GetTableDBName() + "." + PLocationTable.GetCountryCodeDBName() + " AND " +


                        PPartnerTable.GetStatusCodeDBName() + " = 'ACTIVE' AND " +
                        PPartnerTable.GetPartnerClassDBName() + " = 'UNIT' AND (" +
                        PUnitTable.GetUnitTypeCodeDBName() + " = 'TS-CONG' OR " +
                        PUnitTable.GetUnitTypeCodeDBName() + " = 'GA-CONF' OR " +
                        PUnitTable.GetUnitTypeCodeDBName() + " = 'GC-CONG' )"
                        ,
                        ATableName, ReadTransaction);
                    DomainManager.GCacheableTablesManager.AddOrRefreshCachedTable(ATableName, TmpTable, DomainManager.GClientID);
                }
                finally
                {
                    if (NewTransaction)
                    {
                        DBAccess.GDBAccessObj.CommitTransaction();
#if DEBUGMODE
                        if (TSrvSetting.DL >= 7)
                        {
                            Console.WriteLine(this.GetType().FullName + ".GetCoferenceUnitsTable: commited own transaction.");
                        }
#endif
                    }
                }
            }

            /* Return the DataTable from the Cache only if the Hash is not the same */
            return ResultingCachedDataTable(ATableName, AHashCode, out AType);
        }

        /**
         * Returns non-standard cachable table 'CampaignList'.
         * DB Table:  p_partner_location, p_partner, p_unit, p_country
         * @comment Used eg. in Select Event Dialog
         *
         * @comment Uses Ict.Petra.Shared.CacheableTablesManager to store the DataTable
         * once its contents got retrieved from the DB. It returns the cached
         * DataTable from it on subsequent calls, therefore making more no more DB
         * queries.
         *
         * @comment The DataTables is retrieved as Typed DataTables, but is passed
         * out as a normal DataTable. However, this DataTable can be cast by the
         * caller to the appropriate TypedDataTable to have access to the features of
         * a Typed DataTable!
         *
         * @param ATableName TableName that the returned DataTable should have.
         * @param AHashCode Hash of the cacheable DataTable that the caller has. '' can be
         * specified to always get a DataTable back (see @return)
         * @param ARefreshFromDB Set to true to reload the cached DataTable from the
         * DB and through that refresh the Table in the Cache with what is now in the
         * DB (this would be done when it is known that the DB Table has changed).
         * Otherwise set to false.
         * @return DataTable If the Hash passed in in AHashCode doesn't fit the Hash that
         * the CacheableTablesManager has for this cacheable DataTable, the
         * 'InstalledSitesList' DataTable is returned, otherwise nil.
         *
         */
        public DataTable GetCampaignUnitsTable(String ATableName, String AHashCode, Boolean ARefreshFromDB, out System.Type AType)
        {
            TDBTransaction ReadTransaction;
            Boolean NewTransaction;
            DataTable TmpTable;

#if DEBUGMODE
            if (TSrvSetting.DL >= 7)
            {
                Console.WriteLine(this.GetType().FullName + ".GetCampaignUnitsTable called.");
            }
#endif

            if ((ARefreshFromDB) || ((!DomainManager.GCacheableTablesManager.IsTableCached(ATableName))))
            {
                ReadTransaction = DBAccess.GDBAccessObj.GetNewOrExistingTransaction(
                    Ict.Petra.Server.MCommon.MCommonConstants.CACHEABLEDT_ISOLATIONLEVEL,
                    TEnforceIsolationLevel.eilMinimum,
                    out NewTransaction);

                try
                {
                    TmpTable = DBAccess.GDBAccessObj.SelectDT(
                        "SELECT DISTINCT " +
                        PPartnerTable.GetPartnerShortNameDBName() +
                        ", " + PPartnerTable.GetPartnerClassDBName() +
                        ", " + PUnitTable.GetXyzTbdCodeDBName() +
                        ", " + PCountryTable.GetTableDBName() + "." + PCountryTable.GetCountryNameDBName() +
                        ", " + PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetDateEffectiveDBName() +
                        ", " + PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetDateGoodUntilDBName() +
                        ", " + PPartnerTable.GetTableDBName() + "." + PPartnerTable.GetPartnerKeyDBName() +
                        ", " + PUnitTable.GetUnitTypeCodeDBName() +

                        " FROM PUB." + PPartnerTable.GetTableDBName() +
                        ", PUB." + PUnitTable.GetTableDBName() +
                        ", PUB." + PLocationTable.GetTableDBName() +
                        ", PUB." + PPartnerLocationTable.GetTableDBName() +
                        ", PUB." + PCountryTable.GetTableDBName() +

                        " WHERE " +
                        PPartnerTable.GetTableDBName() + "." + PPartnerTable.GetPartnerKeyDBName() + " = " +
                        PUnitTable.GetTableDBName() + "." + PUnitTable.GetPartnerKeyDBName() + " AND " +
                        PPartnerTable.GetStatusCodeDBName() + " = 'ACTIVE' AND " +
                        PPartnerTable.GetTableDBName() + "." + PPartnerTable.GetPartnerKeyDBName() + " = " +
                        PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetPartnerKeyDBName() + " AND " +
                        PLocationTable.GetTableDBName() + "." + PLocationTable.GetSiteKeyDBName() + " = " +
                        PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetSiteKeyDBName() + " AND " +
                        PLocationTable.GetTableDBName() + "." + PLocationTable.GetLocationKeyDBName() + " = " +
                        PPartnerLocationTable.GetTableDBName() + "." + PPartnerLocationTable.GetLocationKeyDBName() + " AND " +
                        PCountryTable.GetTableDBName() + "." + PCountryTable.GetCountryCodeDBName() + " = " +
                        PLocationTable.GetTableDBName() + "." + PLocationTable.GetCountryCodeDBName() + " AND " +
                        PUnitTable.GetXyzTbdCodeDBName() + " <> '' ",
                        ATableName, ReadTransaction);
                    DomainManager.GCacheableTablesManager.AddOrRefreshCachedTable(ATableName, TmpTable, DomainManager.GClientID);
                }
                finally
                {
                    if (NewTransaction)
                    {
                        DBAccess.GDBAccessObj.CommitTransaction();
#if DEBUGMODE
                        if (TSrvSetting.DL >= 7)
                        {
                            Console.WriteLine(this.GetType().FullName + ".GetCampaignUnitsTable: commited own transaction.");
                        }
#endif
                    }
                }
            }

            /* Return the DataTable from the Cache only if the Hash is not the same */
            return ResultingCachedDataTable(ATableName, AHashCode, out AType);
        }
    }
    #endregion
}