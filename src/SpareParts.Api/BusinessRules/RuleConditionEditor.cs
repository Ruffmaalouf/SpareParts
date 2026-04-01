using Dapper;
using Microsoft.CodeAnalysis;
using Pixel.iris5.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Pixel.iris5.shared.br
{
    public class RuleConditionEditor
    {
        public bool p_isLife { get; set; }
        public string p_XmlCondition { get; set; }

        public string connStr;

        string m_errorMessage = "";
        ChunkType m_ChunkType;
        string m_fieldToValidate; // TONY 20-02-2019
        int m_LinkedToPropertyID = -1;
          
        int m_CompanyId = -1;

        //string m_Products = "";
        //int m_ComputationId = -1;

        List<SystemTableDto> m_listSystemTable = new List<SystemTableDto>();
        List<CORE_COUNTRY> m_listCountry = new List<CORE_COUNTRY>();
        List<CORE_COUNTRY> m_listNationality = new List<CORE_COUNTRY>();
        List<OB_OBJECT_PROPERTY> m_listSmaProperty = new List<OB_OBJECT_PROPERTY>();
        List<OB_PROPERTY_TABLE_VALUE> m_listSmaPropertyTableValue = new List<OB_PROPERTY_TABLE_VALUE>();
        List<UW_QUESTION> m_listQuestion = new List<UW_QUESTION>();
        List<UW_FLAG> m_listFlag = new List<UW_FLAG>();
        List<UW_FLAG_VALUE> m_listFlagValue = new List<UW_FLAG_VALUE>();
        List<UW_COINS> m_listCoIns = new List<UW_COINS>();
        List<UW_COMPUT_FRACTION> m_listComputFraction = new List<UW_COMPUT_FRACTION>();
        List<UW_DISEASE> m_listDisease = new List<UW_DISEASE>();
        List<CL_ASSESSMENT_TYPE> m_listAssessmentType = new List<CL_ASSESSMENT_TYPE>();
        List<PF_RELATION> m_listRelation = new List<PF_RELATION>();
        List<UW_END_TYPE> m_listEndType = new List<UW_END_TYPE>();
        List<UW_END_SUB_TYPE> m_listEndSubType = new List<UW_END_SUB_TYPE>();
        List<PF_PROFILE> m_listBroker = new List<PF_PROFILE>(); //ListProfiles
        List<PF_PROFILE_TYPE> m_listProfileType = new List<PF_PROFILE_TYPE>();
        List<PF_PROFESSION> m_listProfession = new List<PF_PROFESSION>();
        List<CORE_TYPE> m_listProfessionClass = new List<CORE_TYPE>();
        List<CORE_CURRENCY> m_listCurrency = new List<CORE_CURRENCY>();
        List<CORE_BRANCH> m_listBranch = new List<CORE_BRANCH>();
        List<CORE_STATUS_REASON> m_listStatusReason = new List<CORE_STATUS_REASON>();
        List<PF_CATEGORY> m_listCategory = new List<PF_CATEGORY>();
        List<PF_TAG> m_listTag = new List<PF_TAG>();
        List<CORE_TYPE> m_listGender = new List<CORE_TYPE>();
        List<CORE_TYPE> m_listMaritalStatus = new List<CORE_TYPE>();
        List<CORE_TYPE> m_listContactType = new List<CORE_TYPE>();
        List<CORE_TYPE> m_listDedType = new List<CORE_TYPE>();
        List<CORE_TYPE> m_listLimitationType = new List<CORE_TYPE>();
        List<CORE_TYPE> m_listAuthorityLevel = new List<CORE_TYPE>();
        List<CORE_TYPE> m_listSourceChannel = new List<CORE_TYPE>();
        List<UW_PROD> m_listProduct = new List<UW_PROD>();
        List<UW_PROD_PKG> m_listPackage = new List<UW_PROD_PKG>();
        List<UW_COB> m_listCob = new List<UW_COB>();
        List<UW_LOB> m_listLob = new List<UW_LOB>();

        //List<SEC_ROLE> m_listAuthorizerRole = new List<SEC_ROLE>();
        List<SEC_ROLE> m_listUserRole = new List<SEC_ROLE>();


        //List<ProductCoverDto> m_listCover = new List<ProductCoverDto>();
        List<UW_COVER> m_listCover = new List<UW_COVER>();
        List<UW_CLAUSE> m_listClause = new List<UW_CLAUSE>(); //AN 2026-03-26
        List<UW_DEDUCTIBLE> m_listDed = new List<UW_DEDUCTIBLE>();
        //List<CORE_TYPE> m_listBRuleAction = new List<CORE_TYPE>();



        //Life  
        List<LF_CLAUSE> m_lifeListClause = new List<LF_CLAUSE>(); //AN 2026-03-26
        List<LF_COVER> m_LifeListCover = new List<LF_COVER>();
        List<LF_QUESTION> m_LifeListQuestion = new List<LF_QUESTION>();
        List<LF_END_TYPE> m_LifeListEndType = new List<LF_END_TYPE>();
        List<LF_END_SUB_TYPE> m_LifeListEndSubType = new List<LF_END_SUB_TYPE>();

        // giorgio 15-dec-2025 SFA-1180
        List<string> smaSupporttedFunctions = new List<string>
        {
            "DATEDIFF",
            "OLDVALUE",
            "PRINCIPALVALUE",
            "ISNULL",
            "COMPETITORSMA",
            "ADD",
            "SUBSTRACT",
            "MULTIPLY",
            "DIVIDE",
            "ISPOLICYEXISTFORSAMEPROPERTY",
            "ISPOLICYEXISTFORSAMEPROPERTYSAMECOB"
        };
        // end giorgio 15-dec-2025 SFA-1180

        // giorgio 16-July-2025 VIA-68 added _uiLanguage

        //TODO RALPH 26-03-2026 BNK-529
        //public string IsValid(string _conditionText, int _companyId, int _lobId, int _cobId, string _products, int _objectId, int _computationId, string _uiLanguage)
        public string IsValid(string _conditionText, int _companyId, int _lobId, int _cobId, string _products, string _objects, int _computationId, string _uiLanguage)
        {
            m_CompanyId = _companyId;
            //m_Products = _products;
            //m_ComputationId = _computationId;

            if (this._checkSyntax(_conditionText) == false)
            {
                return "NOT VALID SYNTAX " + m_errorMessage;
            }

            using (var dbConn = Pixel.Core.Dapper.My.ConnectionFactory())
            {
                dbConn.ConnectionString = connStr;
                dbConn.Open();

                // giorgio 16-July-2025 VIA-68
                //_loadLists(dbConn, _objectId, _lobId, _cobId, _products);
                //TODO RALPH 26-03-2026 BNK-529
                //_loadLists(dbConn, _objectId, _lobId, _cobId, _products, _uiLanguage);
                _loadLists(dbConn, _objects, _lobId, _cobId, _products, _uiLanguage);

                _writeXML_Header();

                if (_checkLogic(_conditionText.TrimStart() + " ", ChunkType.Undefined, false, false, "") == false)
                {
                    if (m_errorMessage != "")
                    {
                        return "NOT VALID LOGIC " + m_errorMessage;
                    }
                }
            }

            return "";
        }

        // giorgio 16-July-2025 VIA-68 added _uiLanguage
        ////TODO RALPH 26-03-2026 BNK-529 Added string objects
        //void _loadLists(System.Data.Common.DbConnection dbConn, int _objectId = "", int _lobId = -1, int _cobId = -1, string _products = "", string _uiLanguage = "EN")
        void _loadLists(System.Data.Common.DbConnection dbConn, string _objects = "", int _lobId = -1, int _cobId = -1, string _products = "", string _uiLanguage = "EN")
        {
            SmaHelper smaHelper = new SmaHelper();
            //objectHelper.connectionSQL = connStr;
            //TODO RALPH 26-03-2026 BNK-529
            //if (_objectId > 0)
            //{
            //    m_listSystemTable = smaHelper.GetListSystemTablesByObjectID(connStr, _objectId);
            //}
            //else
            //{
            //    m_listSystemTable = smaHelper.GetListSystemTables(connStr);
            //}
            if (_objects != "")
            {
                m_listSystemTable = smaHelper.GetListSystemTablesByObjectID(connStr, -1, _objects);
            }
            else
            {
                m_listSystemTable = smaHelper.GetListSystemTables(connStr);
            }
            //END TODO RALPH

            m_listLob.AddRange(dbConn.Query<UW_LOB>("SELECT ID, LOB_CODE, LOB_DESC FROM UW_LOB "));

            if (p_isLife)
            {
                if (_lobId > 0)
                {
                    m_listProduct.AddRange(dbConn.Query<UW_PROD>("SELECT ID, PRODUCT_CODE AS PROD_CODE, PRODUCT_DESC AS PROD_DESC FROM LF_PRODUCT WHERE LOB_ID = " + _lobId));
                    m_listCob.AddRange(dbConn.Query<UW_COB>("SELECT ID, COB_CODE, COB_DESC FROM UW_COB WHERE LOB_ID = " + _lobId));
                }
                else
                {
                    m_listProduct.AddRange(dbConn.Query<UW_PROD>("SELECT ID, PRODUCT_CODE AS PROD_CODE, PRODUCT_DESC AS PROD_DESC FROM LF_PRODUCT "));
                    m_listCob.AddRange(dbConn.Query<UW_COB>("SELECT ID, COB_CODE, COB_DESC FROM UW_COB "));
                }
            }
            else
            {
                if (_lobId > 0)
                {
                    m_listProduct.AddRange(dbConn.Query<UW_PROD>("SELECT ID, PROD_CODE, PROD_DESC FROM UW_PROD WHERE LOB_ID = " + _lobId));
                    m_listCob.AddRange(dbConn.Query<UW_COB>("SELECT ID, COB_CODE, COB_DESC FROM UW_COB WHERE LOB_ID = " + _lobId));
                }
                else
                {
                    m_listProduct.AddRange(dbConn.Query<UW_PROD>("SELECT ID, PROD_CODE, PROD_DESC FROM UW_PROD "));
                    m_listCob.AddRange(dbConn.Query<UW_COB>("SELECT ID, COB_CODE, COB_DESC FROM UW_COB "));
                }
            }

            if (_products != "")
            {
                m_listPackage.AddRange(dbConn.Query<UW_PROD_PKG>(
                    " SELECT UW_PROD_PKG.ID, PKG_DESC + ' - ' + UW_PROD.PROD_DESC AS PKG_DESC " +
                    " FROM UW_PROD_PKG " +
                    " LEFT JOIN UW_PROD ON UW_PROD_PKG.PROD_ID = UW_PROD.ID" +
                    " WHERE PROD_ID IN (" + _products + ")"));
            }

            m_listCountry.AddRange(dbConn.Query<CORE_COUNTRY>("SELECT ID, COUNTRY_CODE, COUNTRY_DESC FROM CORE_COUNTRY"));
            m_listNationality.AddRange(dbConn.Query<CORE_COUNTRY>("SELECT ID, NATIONALITY_DESC FROM CORE_COUNTRY WHERE NATIONALITY_DESC IS NOT NULL"));
            m_listProfileType.AddRange(dbConn.Query<PF_PROFILE_TYPE>("SELECT ID, PROFILE_TYPE_CODE, PROFILE_TYPE_DESC FROM PF_PROFILE_TYPE"));
            m_listProfession.AddRange(dbConn.Query<PF_PROFESSION>("SELECT ID, PROFESSION_CODE, PROFESSION_DESC FROM PF_PROFESSION"));
            m_listRelation.AddRange(dbConn.Query<PF_RELATION>("SELECT ID, ISNULL(RELATION_CODE,'') AS RELATION_CODE, RELATION_DESC FROM PF_RELATION"));
            m_listBroker.AddRange(dbConn.Query<PF_PROFILE>("SELECT ID, PROFILE_CODE, PROFILE_NAME FROM PF_PROFILE WHERE PROFILE_TYPE_ID = 1"));
            m_listCurrency.AddRange(dbConn.Query<CORE_CURRENCY>("SELECT ID, CUR_CODE, CUR_DESC FROM CORE_CURRENCY"));
            m_listStatusReason.AddRange(dbConn.Query<CORE_STATUS_REASON>("SELECT ID, STATUS_REASON_CODE, STATUS_REASON_DESC FROM CORE_STATUS_REASON"));
            m_listBranch.AddRange(dbConn.Query<CORE_BRANCH>("SELECT ID, BRANCH_CODE, BRANCH_DESC FROM CORE_BRANCH"));
            m_listCategory.AddRange(dbConn.Query<PF_CATEGORY>("SELECT * FROM PF_CATEGORY"));
            m_listTag.AddRange(dbConn.Query<PF_TAG>("SELECT * FROM PF_TAG"));

            // giorgio 16-July-2025 VIA-68
            //m_listGender.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'Gender'"));
            //m_listSourceChannel.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'SourceChannel'"));
            //m_listMaritalStatus.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'MaritalStatus'"));
            //m_listContactType.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'ContactType'"));
            //m_listDedType.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'DeductibleType'"));
            //m_listLimitationType.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'LimitationType'"));
            //m_listProfessionClass.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'ProfessionClasses'"));
            //m_listAuthorityLevel.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'AuthorityLevels'"));
            m_listAuthorityLevel.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'AuthorityLevels'"));
            m_listProfessionClass.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'ProfessionClasses'"));
            m_listGender.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'Gender'"));
            m_listSourceChannel.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'SourceChannel'"));
            m_listMaritalStatus.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'MaritalStatus'"));
            m_listContactType.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'ContactType'"));
            m_listDedType.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'DeductibleType'"));
            m_listLimitationType.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC" + (_uiLanguage == "EN" ? "" : "_" + _uiLanguage) + " AS TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'LimitationType'"));
            // end giorgio 16-July-2025 VIA-68

            //m_listBRuleAction.AddRange(dbConn.Query<CORE_TYPE>("SELECT ID, TYPE_CODE, TYPE_DESC FROM CORE_TYPE WHERE TYPE_TABLE = 'BRulesActions'"));

            //05-OCT-2022
            var szAuthRole =
                " SELECT ID, ROLE_NAME " +
                " FROM SEC_ROLE " +
                " WHERE SEC_ROLE.IS_FOR_BA = 0 " +
                (m_CompanyId > 0 ? " AND COMPANY_ID = " + m_CompanyId : "");
            //" ORDER BY ROLE_NAME";

            // m_listAuthorizerRole.AddRange(dbConn.Query<SEC_ROLE>(szAuthRole));

            m_listUserRole.AddRange(dbConn.Query<SEC_ROLE>(szAuthRole));
            //05-OCT-2022

            //TONY 26-NOV-2021
            //var adCore = dbConn.QueryFirstOrDefault<CORE_AD>("SELECT * FROM CORE_AD");
            //var UsesSharedSetting = Convert.ToBoolean(adCore.uses_shared_setting);


            m_listDed.AddRange(dbConn.Query<UW_DEDUCTIBLE>(
                     " SELECT " +
                     " ID, DED_DESC" +
                     " FROM UW_DEDUCTIBLE "));


            var szSqlFract =
                   " SELECT" +
                   " UW_COMPUT_FRACTION.ID" +
                   ",UW_COMPUT_FRACTION.COMPUT_GROUP_ID" +
                   ",UW_COMPUT_FRACTION.COMPUT_FRACTION_DESC" +
                   ",UW_COMPUT_FRACTION.SORTING" +
                   ",UW_COMPUT_GROUP.COMPUT_GROUP_DESC" +
                   ",UW_COMPUT_FRACTION.TAX_REFUND_PERIOD" +
                   ",UW_COMPUT_FRACTION.BASE_CUR_ROUNDED_TO" +
                   ",UW_COMPUT_FRACTION.IS_INACTIVE" +
                   ",UW_COMPUT_FRACTION.INACTIVATION_DATE " +
                   " FROM UW_COMPUT_FRACTION" +
                   " LEFT JOIN UW_COMPUT_GROUP ON UW_COMPUT_FRACTION.COMPUT_GROUP_ID = UW_COMPUT_GROUP.ID ";

            //TODO id we need to enable business rules in broking companies
            //if (Convert.ToInt32(_objData["BusinessModel"].ToString()) == (int)Enums.BusinessType.Brokerage || Convert.ToInt32(_objData["BusinessModel"].ToString()) == == (int)Enums.BusinessType.BrokerageIntegrated)
            //{
            //    szSqlFract += UsesSharedSetting ? " WHERE COMPANY_ID IS NULL " : " WHERE COMPANY_ID = " + Convert.ToInt32(_objData["CompanyID"].ToString()) +
            //                "AND COMPUT_GROUP_ID NOT IN ( " + (int)Enums.ComputationsGroups.Commissions + "," +
            //                                                  (int)Enums.ComputationsGroups.TaxesOnCommissions + "," +
            //                                                  (int)Enums.ComputationsGroups.ReCommissions +
            //                                            ")";
            //}
            //else
            //{
            szSqlFract +=
                " WHERE COMPANY_ID IS NULL " +
                " AND COMPUT_GROUP_ID NOT IN ( " + (int)Enums.ComputationsGroups.Commissions + "," +
                                                          (int)Enums.ComputationsGroups.TaxesOnCommissions + "," +
                                                          (int)Enums.ComputationsGroups.ReCommissions + "," +
                                                          (int)Enums.ComputationsGroups.ReTotalPremium + "," +
                                                          (int)Enums.ComputationsGroups.ReDiscount + "," +
                                                          (int)Enums.ComputationsGroups.ReGrandTotal +
                                                    ")";
            //}

            szSqlFract += " ORDER BY UW_COMPUT_FRACTION.ID";
            m_listComputFraction.AddRange(dbConn.Query<UW_COMPUT_FRACTION>(szSqlFract));
            //TONY 26-NOV-2021


            if (p_isLife)
            {
                m_LifeListQuestion.AddRange(dbConn.Query<Pixel.iris5.models.LF_QUESTION>("SELECT ID, QUESTION_DESC FROM LF_QUESTION"));
                m_LifeListEndType.AddRange(dbConn.Query<Pixel.iris5.models.LF_END_TYPE>("SELECT END_TYPE, END_TYPE_DESC FROM LF_END_TYPE"));
                m_LifeListEndSubType.AddRange(dbConn.Query<Pixel.iris5.models.LF_END_SUB_TYPE>("SELECT ID, END_SUB_TYPE_CODE, END_SUB_TYPE_DESC FROM LF_END_SUB_TYPE"));

                if (_products != "")
                {
                    m_LifeListCover.AddRange(dbConn.Query<Pixel.iris5.models.LF_COVER>(
                       " SELECT " +
                       "   LF_PRODUCT_COVER.COVER_ID AS ID, " +
                       "   COVER_CODE, " +
                       "   COVER_DESC " +
                       " FROM LF_PRODUCT_COVER " +
                       " LEFT JOIN LF_COVER ON LF_PRODUCT_COVER.COVER_ID = LF_COVER.ID " +
                       " WHERE PRODUCT_ID IN (" + _products + ")"));
                }
                else //TONY 21-NOV-2023 stopped the condition of COB if (_cobId > 0)
                {
                    m_LifeListCover.AddRange(dbConn.Query<Pixel.iris5.models.LF_COVER>(
                        " SELECT " +
                        "   ID, " +
                        "   COVER_CODE, " +
                        "   COVER_DESC " +
                        " FROM LF_COVER "));
                }
            }
            else
            {
                m_listQuestion.AddRange(dbConn.Query<UW_QUESTION>("SELECT ID, QUESTION_DESC FROM UW_QUESTION"));
                m_listCoIns.AddRange(dbConn.Query<UW_COINS>("SELECT ID, CO_INS_CODE, CO_INS_DESC FROM UW_COINS"));
                m_listDisease.AddRange(dbConn.Query<UW_DISEASE>("SELECT ID, DISEASE_CODE, DISEASE_DESC FROM UW_DISEASE "));
                m_listAssessmentType.AddRange(dbConn.Query<CL_ASSESSMENT_TYPE>("SELECT ID, ASSESSMENT_TYPE_CODE, ASSESSMENT_TYPE_DESC FROM CL_ASSESSMENT_TYPE"));
                m_listEndType.AddRange(dbConn.Query<UW_END_TYPE>("SELECT END_TYPE, END_TYPE_DESC FROM UW_END_TYPE"));
                m_listEndSubType.AddRange(dbConn.Query<UW_END_SUB_TYPE>("SELECT ID, END_SUB_TYPE_CODE, END_SUB_TYPE_DESC FROM UW_END_SUB_TYPE"));
                m_listFlag.AddRange(dbConn.Query<UW_FLAG>("SELECT ID, FLAG_DESC FROM UW_FLAG"));
                m_listFlagValue.AddRange(dbConn.Query<UW_FLAG_VALUE>("SELECT * FROM UW_FLAG_VALUE"));

                m_listClause.AddRange(dbConn.Query<UW_CLAUSE>(" SELECT CLAUSE_CODE, CLAUSE_DESC FROM UW_CAUSE")); //AN 2026-03-26

                //TONY 26-NOV-2021
                //m_listComputationItem.AddRange(dbConn.Query<UW_COMPUTATION_ITEM>(
                // @"SELECT 
                //        UW_COMPUTATION_ITEM.ID, 
                //        COMPUT_FRACTION_DESC 
                //    FROM UW_COMPUTATION_ITEM 
                //    LEFT JOIN UW_COMPUT_FRACTION ON UW_COMPUTATION_ITEM.COMPUT_FRACTION_ID = UW_COMPUT_FRACTION.ID 
                //    WHERE COMPUTATION_ID = " + _computationId));

                //ProductCoverDto
                if (_products != "")
                {
                    m_listCover.AddRange(dbConn.Query<UW_COVER>(
                        " SELECT " +
                        "   UW_PROD_CVR.COVER_ID AS ID, " +
                        "   COVER_CODE, " +
                        "   COVER_DESC " +
                        " FROM UW_PROD_CVR " +
                        " LEFT JOIN UW_COVER ON UW_PROD_CVR.COVER_ID = UW_COVER.ID " +
                        " WHERE PROD_ID IN (" + _products + ")"));
                }
                else if (_cobId > 0)
                {
                    m_listCover.AddRange(dbConn.Query<UW_COVER>(
                        " SELECT " +
                        "   UW_COB_COVER.COVER_ID AS ID, " +
                        "   COVER_CODE, " +
                        "   COVER_DESC " +
                        " FROM UW_COB_COVER " +
                        " LEFT JOIN UW_COVER ON UW_COB_COVER.COVER_ID = UW_COVER.ID " +
                        " WHERE UW_COB_COVER.COB_ID =" + _cobId));
                }
                else//TONY 21-NOV-2023
                {
                    m_listCover.AddRange(dbConn.Query<UW_COVER>(
                                           " SELECT " +
                                           "   UW_COB_COVER.COVER_ID AS ID, " +
                                           "   COVER_CODE, " +
                                           "   COVER_DESC " +
                                           " FROM UW_COB_COVER " +
                                           " LEFT JOIN UW_COVER ON UW_COB_COVER.COVER_ID = UW_COVER.ID "));
                }//TONY 21-NOV-2023
            }

            m_listSmaProperty.AddRange(dbConn.Query<OB_OBJECT_PROPERTY>(
                " SELECT" +
                " OB_OBJECT_PROPERTY.ID" +
                ",OB_OBJECT_PROPERTY.PROPERTY_DESC" +
                ",OB_OBJECT_PROPERTY.PROPERTY_ID" +
                ",OB_PROPERTY.PROPERTY_TYPE_ID" +
                ",OB_PROPERTY.PROPERTY_TABLE_ID" +
                ",OB_PROPERTY.CONTACT_FIELD" +
                ",OB_PROPERTY.CONTACT_FIELD_PROPERTY_TYPE_ID" +
                ",OB_PROPERTY.SYSTEM_TABLE" +
                " FROM OB_OBJECT_PROPERTY" +
                " LEFT JOIN OB_PROPERTY ON OB_OBJECT_PROPERTY.PROPERTY_ID = OB_PROPERTY.ID" +
               ////TODO RALPH 26-03-2026 BNK-529 
               // (_objectId > 0 ? " WHERE OBJECT_ID = " + _objectId : "")));
               (_objects != "" ? " WHERE OBJECT_ID IN ( " + _objects + " )" : "")));

            m_listSmaPropertyTableValue.AddRange(dbConn.Query<OB_PROPERTY_TABLE_VALUE>("SELECT ID, PROPERTY_TABLE_ID, PROPERTY_VALUE_CODE, THE_VALUE FROM OB_PROPERTY_TABLE_VALUE"));

        }

        #region  Helper Routines

        private bool _checkSyntax(string _text)
        {
            int iCount1 = 0;
            int iCount2 = 0;
            int iCount3 = 0;

            for (var i = 0; i < _text.Length; i++)
            {
                switch (_text.Substring(i, 1))
                {
                    case "{":
                        iCount1 += 1;
                        break;
                    case "}":
                        iCount1 -= 1;
                        break;
                    case "[":
                        iCount2 += 1;
                        break;
                    case "]":
                        iCount2 -= 1;
                        break;
                    case "(":
                        iCount3 += 1;
                        break;
                    case ")":
                        iCount3 -= 1;
                        break;
                }
            }

            if (iCount1 != 0 | iCount2 != 0 | iCount3 != 0)
            {
                this.m_errorMessage = "Incorrect syntax";
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool _checkLogic(string _text, ChunkType _previousChunkType, bool _checkOnly, bool _isFunctionAttribute, string _functionName)//, string _previousTblName)
        {
            string szChunk = null;
            m_errorMessage = "";

            // C# added by tony to send it as ref istead of 1
            int _iCloseStartNo = 1;

            if (_text.Trim().Length != 0)
            {
                _text = _text.TrimStart(' ');
                if (_text.Trim().IndexOf(" ") != -1)
                {
                    szChunk = _text.Substring(0, _text.IndexOf(" "));
                }
                else if (_text.IndexOf("\n") != -1)
                {
                    szChunk = _text.Substring(0, _text.IndexOf(" "));
                }
                else
                {
                    szChunk = _text;
                }

                switch (szChunk.Substring(0, 1))
                {
                    case "{":
                        szChunk = _text.Substring(0, _text.IndexOf("}") + 1);
                        break;
                    case "[":
                        szChunk = _text.Substring(0, _text.IndexOf("]") + 1);
                        break;
                    case "(":
                    case "=":
                    case "<":
                    case ">":
                    case ")":
                        szChunk = _text.Substring(0, 1);
                        break;
                }
                //TODO Ralph Maalouf 07/12/2020 Addition of Length > 3 & length > 2.
                if (_text.Length >= 3 && _text.Substring(0, 3) == "AND")
                {
                    szChunk = _text.Substring(0, 3);
                }
                else if (_text.Length >= 2 && (_text.Substring(0, 2) == "OR" ||
                        _text.Substring(0, 2) == "<=" ||
                        _text.Substring(0, 2) == ">=" ||
                        _text.Substring(0, 2) == "<>"))
                {
                    szChunk = _text.Substring(0, 2);
                }

                //END TODO
                if (szChunk.Length > 0 && szChunk.IndexOf("(") != -1)
                {
                    switch (szChunk.Substring(0, szChunk.IndexOf("(")).ToUpper())
                    {
                        case "PROFILEISSANCTIONEDADDRESSORRESIDENCYORNATIONALITY":
                        case "PROFILEISSANCTIONEDADDRESS":
                        case "PROFILEISSANCTIONEDNATIONALITY":
                        case "INSUREDISSANCTIONEDADDRESSORRESIDENCYORNATIONALITY":
                        case "INSUREDISSANCTIONEDADDRESS":
                        case "INSUREDISSANCTIONEDNATIONALITY":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PROFILETAGINCLUDE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ADD":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ISNULL":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "DIVIDE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "SCOREOF":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "DATEDIFF":
                        case "OLDVALUE":
                        case "MULTIPLY":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "OSCLAIMS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "ISCLEANTQ":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "SUBSTRACT":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "DISEASEOF":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "UNPAIDPREMIUMS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "POLICYUNPAIDDUEPREMIUM":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "TOTALCLAIMS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PAIDCLAIMS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "ISGROUPPOLICY":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "GRDATEBYCOVER":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "NUMBEROFCLAIMS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PRINCIPALVALUE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "TECHNICALSCORE":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "OSCLAIMSBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "CLAIMACTUALSTAY":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "GRBALANCEBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ISLINKEDTOPOLICY":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "LOSSRATIOBYPOLICY":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PAIDCLAIMSBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "CLAIMFINALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "CLAIMAPPROVEDSTAY":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "SUMINSUREDBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "LOSSRATIOBYINSURED":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "ISGRGRANTEDBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "COINSURANCEBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "POLICYCOVERINCLUDE": 
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "POLICYCOVERDEDUCTIBLEINCLUDE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "COMPETITORSMA":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "CLAIMINITIALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PREVIOUSPOLICYACTION":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "NUMBEROFOSASSESSMENTS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "OSCLAIMBYFINALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFRELATEDINSURED":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ISLINKEDTOACTIVEPOLICY":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "CLAIMACTUALSTAYBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFCLAIMSPERCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PREVIOUSCOVERLIMITCOUNT":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFPAIDASSESSMENTS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PAIDCLAIMBYFINALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "OSCLAIMBYINITIALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFPREVIOUSPOLICIES":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PARENTPOLICYSTATUSREASON":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "CLAIMAPPROVEDSTAYBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ISNULLUSERAUTHORITYLEVEL":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "ISPARENTPOLICYBLACKLISTED":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PAIDCLAIMBYINITIALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PREVIOUSCOINSURANCEBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PREVIOUSCOVERLIMITPERCLAIM":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PREVIOUSPOLICYCOVERINCLUDE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ISSAMEINSUREDASLINKEDPOLICY":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFCLAIMSPERASSESSMENT":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFREJECTEDASSESSMENTS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PRINCIPALHASRELATEDINSUREDS":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PRINCIPALCOINSURANCEBYCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PRINCIPALPOLICYCOVERINCLUDE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFEXGRATIAASSESSEMENTS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "EXISTCLAIMAFTEREFFECTIVEDATE":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "EXISTCLAIMBEFOREEFFECTIVEDATE":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;                        
                        case "POLICYHASFACSHARE":// HAMZA 27/11/2025 LAX-5361
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PARENTCERTIFICATESTATUSREASON":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFCLAIMSPERFINALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ISPARENTCERTIFICATEBLACKLISTED":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PREVIOUSCOVERDISEASELIMITCOUNT":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFCLAIMSPERINITIALDISEASE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFREIMBURSEMENTASSESSMENTS":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "NUMBEROFACTIVEPOLICIESFORALLLINES":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "PREVIOUSCOVERDEDUCTIBLEPERCENTAGE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "PREVIOUSCOVERDISEASELIMITPERCLAIM":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ADHERENTSWITCHEDFROMANOTHERSUBLINE":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "NUMBEROFACTIVEPOLICIESFORTHESAMELINE":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "NUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCT":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "LIFENUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCT":
                        case "LIFENUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCTSAMECHILD":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "NUMBEROFACTIVEPOLICIESFORTHESAMESUBLINE":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "HIGHERAPPROVEDDISCHARGEDATEBYASSESSMENTTYPE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "EXISTNULLACTUALDISCHARGEDATEBYASSESSMENTTYPE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "LEASTPERIODBETWEENTWOOCCURRENCECLAIMSPERCOVER":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFADHERENTSINPREVIOUSPOLICYATEXPIRYDATE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "NUMBEROFADHERENTSINPREVIOUSPOLICYATEFFECTIVEDATE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "ISSANCTIONEDNATIONALITY":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;                        
                        case "ISSUBBROKERCOMGREATERTHANBROKERCOM":// giorgio 20/6/2024 LIA-3493
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;                        
                        case "ISSANCTIONEDSMACOUNTRY":
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "ISSANCTIONEDSMANATIONALITY":// EliasN 24-05-2023 DEVIRIS5-11970
                            szChunk = _text.Substring(0, _text.IndexOf(")") + 1);
                            break;
                        case "ISPOLICYEXISTFORSAMEPROPERTY":
                        case "ISPOLICYEXISTFORSAMEPROPERTYSAMECOB":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;                        
                        case "POLICYHOLDERTAGINCLUDE":// CHARBELAS 26-11-2024 AXA-4647
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        
                        //AN  2026-03-26 copied from IRIS 2
                        case "POLICYCLAUSEINCLUDE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "POLICYCOVERRATE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "POLICYCOVERSUMINSURED":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "POLICYCOVERPREMIUM":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                        case "POLICYSUBCOVERINCLUDE":
                            szChunk = _getChunkText(_text.IndexOf("(") + 1, ref _iCloseStartNo, _text);
                            break;
                            //AN  2026-03-26 copied from IRIS 2
                    }
                }

                // giorgio 15-dec-2025 SFA-1180
                //m_ChunkType = _getChunkType(szChunk, _checkOnly, _isFunctionAttribute);
                m_ChunkType = _getChunkType(szChunk, _checkOnly, _isFunctionAttribute, _functionName);

                switch (m_ChunkType)
                {
                    case ChunkType.Operators:
                        if (_previousChunkType != ChunkType.SmaProperties &&
                            _previousChunkType != ChunkType.PolicyFields &&
                            _previousChunkType != ChunkType.LinkedToFields &&
                            _previousChunkType != ChunkType.CompetitorPolicy &&
                            _previousChunkType != ChunkType.SystemValues &&
                            _previousChunkType != ChunkType.UwQuestions &&
                            _previousChunkType != ChunkType.ConditionalFlags &&
                            _previousChunkType != ChunkType.Functions &&
                            _previousChunkType != ChunkType.PolicyInsured &&
                            _previousChunkType != ChunkType.PolicyBroker &&
                            _previousChunkType != ChunkType.PolicyAction &&
                            _previousChunkType != ChunkType.PolicyComputation &&
                            _previousChunkType != ChunkType.ScopeProfile &&
                            _previousChunkType != ChunkType.ScopeCollection)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;
                    case ChunkType.LogicalOperators:
                        if (_previousChunkType != ChunkType.DimensionValue &&
                            _previousChunkType != ChunkType.RightParenthesis)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;
                    case ChunkType.DimensionValue:
                        if (_previousChunkType != ChunkType.Operators)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;
                    case ChunkType.SmaProperties:
                        //TONY 26-NOV-2021
                        //if (_PreviousChunkType != ChunkType.Undefined &&
                        //    _PreviousChunkType != ChunkType.LeftParenthesis &&
                        //    _PreviousChunkType != ChunkType.LogicalOperators &&
                        //    _PreviousChunkType != ChunkType.Functions &&
                        //    _PreviousChunkType != ChunkType.PolicyFields &&
                        //    _PreviousChunkType != ChunkType.LinkedToFields &&
                        //    _PreviousChunkType != ChunkType.CompetitorPolicy &&
                        //    _PreviousChunkType != ChunkType.SmaProperties
                        //    )
                        //{
                        //    m_errorMessage += "Incorrect syntax near: " + szChunk;
                        //}

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            // giorgio 15-dec-2025 SFA-1180
                            //if (_functionName.ToUpper() != "DATEDIFF" &&
                            //    _functionName.ToUpper() != "OLDVALUE" &&
                            //    _functionName.ToUpper() != "PRINCIPALVALUE" &&
                            //    _functionName.ToUpper() != "ISNULL" &&
                            //    _functionName.ToUpper() != "COMPETITORSMA" &&
                            //    _functionName.ToUpper() != "ADD" &&
                            //    _functionName.ToUpper() != "SUBSTRACT" &&
                            //    _functionName.ToUpper() != "MULTIPLY" &&
                            //    _functionName.ToUpper() != "DIVIDE" &&
                            //    _functionName.ToUpper() != "ISPOLICYEXISTFORSAMEPROPERTY" &&
                            //    _functionName.ToUpper() != "ISPOLICYEXISTFORSAMEPROPERTYSAMECOB")
                            if (smaSupporttedFunctions.Contains(_functionName.ToUpper()) == false)
                            { // end giorgio 15-dec-2025 SFA-1180
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.PolicyFields:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions &&
                            _previousChunkType != ChunkType.LinkedToFields &&
                            _previousChunkType != ChunkType.CompetitorPolicy &&
                            _previousChunkType != ChunkType.PolicyFields &&
                            _previousChunkType != ChunkType.SmaProperties)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "DATEDIFF" &&
                                _functionName.ToUpper() != "OLDVALUE" &&
                                _functionName.ToUpper() != "ISNULL")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.LinkedToFields:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions &&
                            _previousChunkType != ChunkType.PolicyFields &&
                            _previousChunkType != ChunkType.LinkedToFields &&
                            _previousChunkType != ChunkType.CompetitorPolicy &&
                            _previousChunkType != ChunkType.SmaProperties)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "DATEDIFF" &&
                                _functionName.ToUpper() != "OLDVALUE" &&
                                _functionName.ToUpper() != "ISNULL")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.CompetitorPolicy:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;
                    case ChunkType.PolicyComputation:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "ADD" &&
                                _functionName.ToUpper() != "SUBSTRACT" &&
                                _functionName.ToUpper() != "MULTIPLY" &&
                                _functionName.ToUpper() != "DIVIDE")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.SystemValues:
                        if (_previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "DATEDIFF")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.UwQuestions:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "SCOREOF" && _functionName.ToUpper() != "DISEASEOF")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.ConditionalFlags:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions)
                        {

                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;
                    case ChunkType.PolicyInsured:
                        //_PreviousChunkType <> ChunkType.Undefined And _
                        //_PreviousChunkType <> ChunkType.LeftParenthesis And _
                        //_PreviousChunkType <> ChunkType.LogicalOperators And _
                        //if (_PreviousChunkType != ChunkType.Functions)
                        //{
                        //    m_errorMessage += "Incorrect syntax near: " + szChunk;
                        //}

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "NUMBEROFRELATEDINSURED" &&
                                _functionName.ToUpper() != "ISNULL")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.PolicyAction:
                        //_PreviousChunkType <> ChunkType.Undefined And _
                        //_PreviousChunkType <> ChunkType.LeftParenthesis And _
                        if (_previousChunkType != ChunkType.Functions)
                        {

                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "PREVIOUSPOLICYACTION")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.Covers:
                        if (_previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "OSCLAIMSBYCOVER" &&
                                _functionName.ToUpper() != "PAIDCLAIMSBYCOVER" &&
                                _functionName.ToUpper() != "ISGRGRANTEDBYCOVER" &&
                                _functionName.ToUpper() != "COINSURANCEBYCOVER" &&
                                _functionName.ToUpper() != "PREVIOUSCOINSURANCEBYCOVER" &&
                                _functionName.ToUpper() != "POLICYCOVERINCLUDE" &&
                                _functionName.ToUpper() != "POLICYCOVERDEDUCTIBLEINCLUDE" &&
                                _functionName.ToUpper() != "GRBALANCEBYCOVER" &&
                                _functionName.ToUpper() != "GRDATEBYCOVER" &&
                                _functionName.ToUpper() != "CLAIMACTUALSTAYBYCOVER" &&
                                _functionName.ToUpper() != "PREVIOUSCOVERLIMITPERCLAIM" &&
                                _functionName.ToUpper() != "NUMBEROFCLAIMSPERCOVER" &&
                                _functionName.ToUpper() != "PREVIOUSCOVERLIMITCOUNT" &&
                                _functionName.ToUpper() != "LEASTPERIODBETWEENTWOOCCURRENCECLAIMSPERCOVER" &&
                                _functionName.ToUpper() != "PREVIOUSPOLICYCOVERINCLUDE" &&
                                _functionName.ToUpper() != "CLAIMAPPROVEDSTAYBYCOVER" &&
                                _functionName.ToUpper() != "PRINCIPALCOINSURANCEBYCOVER" &&
                                _functionName.ToUpper() != "PRINCIPALPOLICYCOVERINCLUDE" &&
                                _functionName.ToUpper() != "SUMINSUREDBYCOVER" &&

                                // AN 2026-03-27
                                _functionName.ToUpper() != "POLICYCLAUSEINCLUDE" &&
                                _functionName.ToUpper() != "POLICYCOVERRATE" &&
                                _functionName.ToUpper() != "POLICYCOVERSUMINSURED" &&
                                _functionName.ToUpper() != "POLICYCOVERPREMIUM" &&
                                _functionName.ToUpper() != "POLICYSUBCOVERINCLUDE" 
                                )
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.AssessmentTypes:
                        if (_previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "NUMBEROFCLAIMSPERASSESSMENT" &&
                                _functionName.ToUpper() != "HIGHERAPPROVEDDISCHARGEDATEBYASSESSMENTTYPE" &&
                                _functionName.ToUpper() != "EXISTNULLACTUALDISCHARGEDATEBYASSESSMENTTYPE")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;
                    case ChunkType.Diseases:
                        if (_previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "CLAIMFINALDISEASE" &&
                                _functionName.ToUpper() != "CLAIMINITIALDISEASE" &&
                                _functionName.ToUpper() != "NUMBEROFCLAIMSPERFINALDISEASE" &&
                                _functionName.ToUpper() != "NUMBEROFCLAIMSPERINITIALDISEASE" &&
                                _functionName.ToUpper() != "PREVIOUSCOVERDISEASELIMITCOUNT" &&
                                _functionName.ToUpper() != "PREVIOUSCOVERDISEASELIMITPERCLAIM" &&
                                _functionName.ToUpper() != "DISEASEOF" &&
                                _functionName.ToUpper() != "PAIDCLAIMBYFINALDISEASE" &&
                                _functionName.ToUpper() != "PAIDCLAIMBYINITIALDISEASE" &&
                                _functionName.ToUpper() != "OSCLAIMBYFINALDISEASE" &&
                                _functionName.ToUpper() != "OSCLAIMBYINITIALDISEASE")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }

                        break;

                    case ChunkType.LeftParenthesis:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LogicalOperators)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;
                    case ChunkType.RightParenthesis:
                        if (_previousChunkType != ChunkType.DimensionValue &&
                            _previousChunkType != ChunkType.Functions &&
                            _previousChunkType != ChunkType.RightParenthesis)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;
                    case ChunkType.Functions:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions &&
                            _previousChunkType != ChunkType.Operators)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        break;

                    case ChunkType.ScopeProfile:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        if (_previousChunkType == ChunkType.Functions)
                        {
                            if (_functionName.ToUpper() != "DATEDIFF" &&
                                _functionName.ToUpper() != "OLDVALUE" &&
                                _functionName.ToUpper() != "ISNULL")
                            {
                                m_errorMessage += "Function " + _functionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                            }
                        }
                        break;

                    case ChunkType.ScopeCollection:
                        if (_previousChunkType != ChunkType.Undefined &&
                            _previousChunkType != ChunkType.LeftParenthesis &&
                            _previousChunkType != ChunkType.LogicalOperators &&
                            _previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }

                        //////if (_PreviousChunkType == ChunkType.Functions)
                        //////{
                        //////    if (_FunctionName.ToUpper() != "DATEDIFF" &&
                        //////        _FunctionName.ToUpper() != "OLDVALUE" &&
                        //////        _FunctionName.ToUpper() != "ISNULL")
                        //////    {
                        //////        m_errorMessage += "Function " + _FunctionName + " does not accept parameter: " + szChunk + ". " + System.Environment.NewLine;
                        //////    }
                        //////}

                        break;
                    //TODO ELIASN &CHARBELAS 11 - JUNE - 2024 LIA - 3374
                    case ChunkType.Number:
                        if (_previousChunkType != ChunkType.Functions)
                        {
                            m_errorMessage += "Incorrect syntax near: " + szChunk;
                        }
                        break;
                    //TODO ELIASN &CHARBELAS 11 - JUNE - 2024 LIA - 3374
                    case ChunkType.Undefined:
                        m_errorMessage += szChunk + " is not defined" + System.Environment.NewLine;
                        break;
                }

                if (m_errorMessage != "") //Exit Function
                {
                    return false;
                }

                string szText = _text;
                szText = szText.Remove(0, szChunk.Length);
                szText = szText.TrimStart();

                if (szText != "")
                {
                    return _checkLogic(szText, m_ChunkType, _checkOnly, _isFunctionAttribute, _functionName);
                }
                else
                {
                    if (_isFunctionAttribute)
                    {
                        return true;
                    }
                    else
                    {
                        if (m_ChunkType == ChunkType.DimensionValue
                            || m_ChunkType == ChunkType.RightParenthesis
                            || m_ChunkType == ChunkType.SmaProperties//TONY 26-NOV-2021
                            || m_ChunkType == ChunkType.Number //TODO ELIASN 11-JUNE-2024 LIA-3374
                            || m_ChunkType == ChunkType.Functions//TONY 02-MAY-2024 DEVIRIS5-12747
                            )
                        {
                            return true;
                        }
                        else
                        {
                            m_errorMessage += "Incorrect syntax after: " + szChunk;
                            return false;
                        }
                    }
                }
            }
            return false;
        }

        private bool _checkFunction(string _chunk, bool _checkOnly)
        {
            if (_chunk.Substring(_chunk.Length - 1, 1) != ")")
            {
                return false;
            }

            if (_chunk != "" && _chunk.IndexOf("(") != -1)
            {
                switch (_chunk.Substring(0, _chunk.IndexOf("(")).ToUpper())
                {
                    case "PROFILEISSANCTIONEDADDRESSORRESIDENCYORNATIONALITY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ProfileIsSanctionedAddressOrResidencyOrNationality");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PROFILEISSANCTIONEDADDRESSORRESIDENCYORNATIONALITY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "PROFILEISSANCTIONEDADDRESS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ProfileIsSanctionedAddress");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PROFILEISSANCTIONEDADDRESS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "PROFILEISSANCTIONEDNATIONALITY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ProfileIsSanctionedNationality");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PROFILEISSANCTIONEDNATIONALITY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;

                    case "INSUREDISSANCTIONEDADDRESSORRESIDENCYORNATIONALITY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "InsuredIsSanctionedAddressOrResidencyOrNationality");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_INSUREDISSANCTIONEDADDRESSORRESIDENCYORNATIONALITY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "INSUREDISSANCTIONEDADDRESS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "InsuredIsSanctionedAddress");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_INSUREDISSANCTIONEDADDRESS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "INSUREDISSANCTIONEDNATIONALITY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "InsuredIsSanctionedNationality");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_INSUREDISSANCTIONEDNATIONALITY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "PROFILETAGINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ProfileTagInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "ProfileTagInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PROFILETAGINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;

                    case "ADD":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "Add");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(
                                Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(),
                                ChunkType.Functions, _checkOnly, true, "Add"))
                            {
                            }

                            this.m_LinkedToPropertyID = -1;
                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "Add")) //_szChunk.IndexOf(")") - 1
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ADD");
                                }
                            }
                            this.m_LinkedToPropertyID = -1;
                            return true;
                        }

                        break;
                    case "ISNULL":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsNull");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "IsNull"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISNULL");
                                }
                                return true;
                            }
                        }

                        break;
                    case "DIVIDE":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "Divide");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(),
                                ChunkType.Functions, _checkOnly, true, "Divide"))
                            {
                            }

                            this.m_LinkedToPropertyID = -1;
                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "Divide")) //_szChunk.IndexOf(")") - 1
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_DIVIDE");
                                }
                            }
                            this.m_LinkedToPropertyID = -1;
                            return true;
                        }

                        break;
                    case "SCOREOF":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ScoreOf");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "ScoreOf"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_SCOREOF");
                                }
                                return true;
                            }
                        }

                        break;
                    case "DATEDIFF":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "DateDiff");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            switch (Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper())
                            {
                                case "{DATE_ISSUE}":
                                case "{DATE_EFFECTIVE}":
                                case "{DATE_EXPIRY}":
                                //case "{DATE_PROPOSAL}":
                                case "{DATE_ELIGIBILITY}":
                                case "{MASTER_POLICY_EFFECTIVE}":
                                case "{MASTER_POLICY_EXPIRY}":
                                case "{MOTHER_POLICY_EFFECTIVE}":
                                case "{MOTHER_POLICY_EXPIRY}":
                                case "{PREMIUM_CURRENCY}":
                                case "{SUM_INSURED_CURRENCY}":
                                case "{POLICY_SUM_INSURED}":
                                case "{POLICY_PREMIUM}":
                                case "{END_TYPE}":
                                case "{END_SUB_TYPE}":
                                case "{SUB_BROKER}":
                                    //Create parameter node for predefined functions
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionPolicy", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                    }
                                    break;

                                case "{COMPETITOR_DATE_EFFECTIVE}":
                                case "{COMPETIROR_DATE_EXPIRY}":
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionCompetitorPolicy", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                    }
                                    break;

                                case "{LINKED_DATE_ISSUE}":
                                case "{LINKED_DATE_EFFECTIVE}":
                                case "{LINKED_DATE_EXPIRY}":
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionLinkedTo", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                    }
                                    break;

                                // AN 2026-02-02
                                case "{INSURED_DOB}":
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionPolicyInsured", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                    }
                                    break;
                                // AN 2026-02-02

                                default:
                                    if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(),
                                        ChunkType.Functions, _checkOnly, true, "DateDiff"))
                                    {
                                    }
                                    break;
                            }

                            this.m_LinkedToPropertyID = -1;
                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            switch (_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper()) //_szChunk.IndexOf(")") - 1))))
                            {
                                case "{DATE_ISSUE}":
                                case "{DATE_EFFECTIVE}":
                                case "{DATE_EXPIRY}":
                                case "{MASTER_POLICY_EFFECTIVE}":
                                case "{MASTER_POLICY_EXPIRY}":
                                case "{MOTHER_POLICY_EFFECTIVE}":
                                case "{MOTHER_POLICY_EXPIRY}":
                                case "{DATE_PROPOSAL}":
                                case "{DATE_ELIGIBILITY}":
                                    //STOPPED BY TONY on 13-MAY-2020
                                    //case "{PREMIUM_CURRENCY}":
                                    //case "{SUM_INSURED_CURRENCY}":
                                    //Create parameter node for predefined functions
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionPolicy", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                        _writeXML_Node("pre_EndFunction", "END_DATEDIFF");
                                    }
                                    return true;

                                case "{COMPETITOR_DATE_EFFECTIVE}":
                                case "{COMPETITOR_DATE_EXPIRY}":
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionCompetitorPolicy", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                        _writeXML_Node("pre_EndFunction", "END_DATEDIFF");
                                    }
                                    return true;

                                //AN 2026-02-02
                                case "{INSURED_DOB}":
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionPolicyInsured", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                        _writeXML_Node("pre_EndFunction", "END_DATEDIFF");
                                    }
                                    return true;
                                //AN 2026-02-02

                                case "{LINKED_DATE_ISSUE}":
                                case "{LINKED_DATE_EFFECTIVE}":
                                case "{LINKED_DATE_EXPIRY}":
                                    if (!_checkOnly)
                                    {
                                        _writeXML_Node("pre_DimensionLinkedTo", Convert.ToString(Convert.ToString(_chunk.Substring(2, _chunk.IndexOf("}") - 2)).TrimStart(' ')).TrimEnd(' '));
                                        _writeXML_Node("pre_EndFunction", "END_DATEDIFF");
                                    }
                                    return true;

                                default:
                                    if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "DateDiff")) //_szChunk.IndexOf(")") - 1
                                    {
                                        if (!_checkOnly)
                                        {
                                            _writeXML_Node("pre_EndFunction", "END_DATEDIFF");
                                        }
                                    }
                                    this.m_LinkedToPropertyID = -1;
                                    return true;
                            }
                        }

                        break;
                    case "OLDVALUE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "OldValue");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "OldValue"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_OLDVALUE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "OSCLAIMS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "OSClaims");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_OSCLAIMS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "MULTIPLY":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "Multiply");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(),
                                ChunkType.Functions, _checkOnly, true, "Multiply"))
                            {
                            }

                            this.m_LinkedToPropertyID = -1;
                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "Multiply"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_MULTIPLY");
                                }
                            }
                            this.m_LinkedToPropertyID = -1;
                            return true;
                        }

                        break;
                    case "ISCLEANTQ":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsCleanTQ");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISCLEANTQ");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "SUBSTRACT":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "Substract");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(), ChunkType.Functions, _checkOnly, true, "Substract"))
                            {
                            }

                            this.m_LinkedToPropertyID = -1;
                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "Substract"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_SUBSTRACT");
                                }
                            }
                            this.m_LinkedToPropertyID = -1;
                            return true;
                        }

                        break;
                    case "DISEASEOF":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "DiseaseOf");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(), ChunkType.Functions, _checkOnly, true, "DiseaseOf"))
                            {
                            }

                            //Me.iLinkedToPropertyID = -1
                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "DiseaseOf"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_DISEASEOF");
                                }
                            }
                            //Me.iLinkedToPropertyID = -1
                            return true;
                        }
                        break;
                    case "UNPAIDPREMIUMS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "UnpaidPremiums");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_UNPAIDPREMIUMS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;

                    case "POLICYUNPAIDDUEPREMIUM":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyUnpaidDuePremium");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYUNPAIDDUEPREMIUM");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;

                    case "TOTALCLAIMS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "TotalClaims");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_TOTALCLAIMS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case "PAIDCLAIMS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PaidClaims");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PAIDCLAIMS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "ISGROUPPOLICY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsGroupPolicy");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISGROUPPOLICY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "GRDATEBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "GRDateByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "GRDateByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_GRDATEBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFCLAIMS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfClaims");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFCLAIMS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PRINCIPALVALUE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PrincipalValue");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PrincipalValue"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PRINCIPALVALUE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "TECHNICALSCORE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "TechnicalScore");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_TECHNICALSCORE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "OSCLAIMSBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "OSClaimsByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "OSClaimsByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_OSCLAIMSBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "CLAIMACTUALSTAY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ClaimActualStay");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_CLAIMACTUALSTAY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "GRBALANCEBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "GRBalanceByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "GRBalanceByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_GRBALANCEBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "ISLINKEDTOPOLICY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsLinkedToPolicy");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISLINKEDTOPOLICY");
                                }
                                return true;
                            }
                        }

                        break;
                    case "LOSSRATIOBYPOLICY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "LossRatioByPolicy");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_LOSSRATIOBYPOLICY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PAIDCLAIMSBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PaidClaimsByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PaidClaimsByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PAIDCLAIMSBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "CLAIMFINALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ClaimFinalDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "ClaimFinalDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_CLAIMFINALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "CLAIMAPPROVEDSTAY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ClaimApprovedStay");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_CLAIMAPPROVEDSTAY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "SUMINSUREDBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "SumInsuredByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "SumInsuredByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_SUMINSUREDBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "LOSSRATIOBYINSURED":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "LossRatioByInsured");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_LOSSRATIOBYINSURED");
                                }
                            }
                            else
                            {
                                return false;
                            }

                            return true;
                        }

                        break;
                    case "ISGRGRANTEDBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsGRGrantedByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "IsGRGrantedByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISGRGRANTEDBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "COINSURANCEBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "CoInsuranceByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "CoInsuranceByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_COINSURANCEBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "POLICYCOVERINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyCoverInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicyCoverInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYCOVERINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "POLICYCOVERDEDUCTIBLEINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyCoverDeductibleInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicyCoverDeductibleInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYCOVERDEDUCTIBLEINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;

                    case "COMPETITORSMA":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "CompetitorSma");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "CompetitorSma"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_COMPETITORSMA");
                                }
                                return true;
                            }
                        }

                        break;
                    case "CLAIMINITIALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ClaimInitialDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "ClaimInitialDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_CLAIMINITIALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "PREVIOUSPOLICYACTION":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousPolicyAction");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PreviousPolicyAction"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSPOLICYACTION");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFOSASSESSMENTS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfOSAssessments");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFOSASSESSMENTS");
                                }
                                return true;
                            }
                        }

                        break;
                    case "OSCLAIMBYFINALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "OSClaimByFinalDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "OSClaimByFinalDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_OSCLAIMBYFINALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFRELATEDINSURED":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfRelatedInsured");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "NumberOfRelatedInsured"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFRELATEDINSURED");
                                }
                                return true;
                            }
                        }

                        break;
                    case "ISLINKEDTOACTIVEPOLICY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsLinkedToActivePolicy");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISLINKEDTOACTIVEPOLICY");
                                }
                                return true;
                            }
                        }

                        break;
                    case "CLAIMACTUALSTAYBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ClaimActualStayByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "ClaimActualStayByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_CLAIMACTUALSTAYBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFCLAIMSPERCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfClaimsPerCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "NumberOfClaimsPerCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFCLAIMSPERCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "PREVIOUSCOVERLIMITCOUNT":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousCoverLimitCount");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PreviousCoverLimitCount"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSCOVERLIMITCOUNT");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFPAIDASSESSMENTS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfPaidAssessments");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFPAIDASSESSMENTS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PAIDCLAIMBYFINALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PaidClaimByFinalDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PaidClaimByFinalDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PAIDCLAIMBYFINALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "OSCLAIMBYINITIALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "OSClaimByInitialDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "OSClaimByInitialDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_OSCLAIMBYINITIALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFPREVIOUSPOLICIES":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfPreviousPolicies");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFPREVIOUSPOLICIES");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PARENTPOLICYSTATUSREASON":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ParentPolicyStatusReason");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PARENTPOLICYSTATUSREASON");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "CLAIMAPPROVEDSTAYBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ClaimApprovedStayByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "ClaimApprovedStayByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_CLAIMAPPROVEDSTAYBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "ISNULLUSERAUTHORITYLEVEL":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "isNullUserAuthorityLevel");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISNULLUSERAUTHORITYLEVEL");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "ISPARENTPOLICYBLACKLISTED":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "isParentPolicyBlackListed");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISPARENTPOLICYBLACKLISTED");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PAIDCLAIMBYINITIALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PaidClaimByInitialDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PaidClaimByInitialDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PAIDCLAIMBYINITIALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "PREVIOUSCOINSURANCEBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousCoInsuranceByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PreviousCoInsuranceByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSCOINSURANCEBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "PREVIOUSCOVERLIMITPERCLAIM":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousCoverLimitPerClaim");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PreviousCoverLimitPerClaim"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSCOVERLIMITPERCLAIM");
                                }
                                return true;
                            }
                        }

                        break;
                    case "PREVIOUSPOLICYCOVERINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousPolicyCoverInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PreviousPolicyCoverInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSPOLICYCOVERINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "ISSAMEINSUREDASLINKEDPOLICY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsSameInsuredAsLinkedPolicy");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISSAMEINSUREDASLINKEDPOLICY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "NUMBEROFCLAIMSPERASSESSMENT":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfClaimsPerAssessment");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "NumberOfClaimsPerAssessment"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFCLAIMSPERASSESSMENT");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFREJECTEDASSESSMENTS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfRejectedAssessments");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFREJECTEDASSESSMENTS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PRINCIPALHASRELATEDINSUREDS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PrincipalHasRelatedInsureds");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PRINCIPALHASRELATEDINSUREDS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PRINCIPALCOINSURANCEBYCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PrincipalCoinsuranceByCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PrincipalCoinsuranceByCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PRINCIPALCOINSURANCEBYCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "PRINCIPALPOLICYCOVERINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PrincipalPolicyCoverInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PrincipalPolicyCoverInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PRINCIPALPOLICYCOVERINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFEXGRATIAASSESSEMENTS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfExgratiaAssessements");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFEXGRATIAASSESSEMENTS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "EXISTCLAIMAFTEREFFECTIVEDATE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ExistClaimAfterEffectiveDate");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_EXISTCLAIMAFTEREFFECTIVEDATE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "EXISTCLAIMBEFOREEFFECTIVEDATE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ExistClaimBeforeEffectiveDate");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_EXISTCLAIMBEFOREEFFECTIVEDATE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    //HAMZA 27/11/2025 LAX-5361
                    case "POLICYHASFACSHARE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyHasFacShare");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYHASFACSHARE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    //END HAMZA 27/11/2025 LAX-5361
                    case "PARENTCERTIFICATESTATUSREASON":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ParentCertificateStatusReason");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PARENTCERTIFICATESTATUSREASON");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "NUMBEROFCLAIMSPERFINALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfClaimsPerFinalDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "NumberOfClaimsPerFinalDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFCLAIMSPERFINALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "ISPARENTCERTIFICATEBLACKLISTED":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "isParentCertificateBlackListed");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISPARENTCERTIFICATEBLACKLISTED");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PREVIOUSCOVERDISEASELIMITCOUNT":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousCoverDiseaseLimitCount");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDiseaseLimitCount"))
                            {
                            }

                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length + 1);
                            if (_checkLogic(_chunk.Substring(0, _chunk.IndexOf(",")).TrimStart(' ').TrimEnd(' ').ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDiseaseLimitCount"))
                            {
                            }

                            //Me.iLinkedToPropertyID = -1
                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDiseaseLimitCount"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSCOVERDISEASELIMITCOUNT");
                                }
                            }
                            //Me.iLinkedToPropertyID = -1
                            return true;
                        }

                        break;
                    case "NUMBEROFCLAIMSPERINITIALDISEASE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfClaimsPerInitialDisease");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "NumberOfClaimsPerInitialDisease"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFCLAIMSPERINITIALDISEASE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFREIMBURSEMENTASSESSMENTS":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfReimbursementAssessments");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFREIMBURSEMENTASSESSMENTS");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "NUMBEROFACTIVEPOLICIESFORALLLINES":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfActivePoliciesForAllLines");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFACTIVEPOLICIESFORALLLINES");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "PREVIOUSCOVERDEDUCTIBLEPERCENTAGE":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousCoverDeductiblePercentage");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDeductiblePercentage"))
                            {
                            }

                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDeductiblePercentage"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSCOVERDEDUCTIBLEPERCENTAGE");
                                }
                            }
                            return true;
                        }

                        break;
                    case "PREVIOUSCOVERDISEASELIMITPERCLAIM":
                        //Create Name Node for predefined functions
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PreviousCoverDiseaseLimitPerClaim");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);

                            if (_checkLogic(Convert.ToString(Convert.ToString(Convert.ToString(_chunk.Substring(1, _chunk.IndexOf(",") - 1)).TrimStart(' ')).TrimEnd(' ')).ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDiseaseLimitPerClaim"))
                            {
                            }

                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length + 1);
                            if (_checkLogic(_chunk.Substring(0, _chunk.IndexOf(",")).TrimStart(' ').TrimEnd(' ').ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDiseaseLimitPerClaim"))
                            {
                            }

                            _chunk = _chunk.Remove(0, _chunk.Substring(1, _chunk.IndexOf(",")).Length);

                            //if (this.CheckLogic(Convert.ToString(Convert.ToString(Convert.ToString(_szChunk.Substring(1, Convert.ToString(_szChunk.Length - 2).Trim(' '))).TrimStart(' ')).TrimEnd(' ')).ToUpper(), ChunkType.Functions, _CheckOnly, true, "PreviousCoverDiseaseLimitPerClaim"))
                            if (_checkLogic(_chunk.Substring(1, _chunk.Length - 2).Trim().ToUpper(), ChunkType.Functions, _checkOnly, true, "PreviousCoverDiseaseLimitPerClaim"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_PREVIOUSCOVERDISEASELIMITPERCLAIM");
                                }
                            }
                            return true;
                        }

                        break;
                    case "ADHERENTSWITCHEDFROMANOTHERSUBLINE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "AdherentSwitchedFromAnotherSubLine");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ADHERENTSWITCHEDFROMANOTHERSUBLINE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "NUMBEROFACTIVEPOLICIESFORTHESAMELINE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfActivePoliciesForTheSameLine");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFACTIVEPOLICIESFORTHESAMELINE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "NUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCT":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfActivePoliciesForTheSameProduct");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCT");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "LIFENUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCT":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "LifeNumberOfActivePoliciesForTheSameProduct");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_LIFENUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCT");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "LIFENUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCTSAMECHILD":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "LifeNumberOfActivePoliciesForTheSameProductSameChild");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_LIFENUMBEROFACTIVEPOLICIESFORTHESAMEPRODUCTSAMECHILD");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;


                    case "NUMBEROFACTIVEPOLICIESFORTHESAMESUBLINE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfActivePoliciesForTheSameSubLine");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFACTIVEPOLICIESFORTHESAMESUBLINE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "HIGHERAPPROVEDDISCHARGEDATEBYASSESSMENTTYPE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "HigherApprovedDischargeDateByAssessmentType");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "HigherApprovedDischargeDateByAssessmentType"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_HIGHERAPPROVEDDISCHARGEDATEBYASSESSMENTTYPE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "EXISTNULLACTUALDISCHARGEDATEBYASSESSMENTTYPE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "ExistNullActualDischargeDateByAssessmentType");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "ExistNullActualDischargeDateByAssessmentType"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_EXISTNULLACTUALDISCHARGEDATEBYASSESSMENTTYPE");
                                }
                                return true;
                            }
                        }

                        break;
                    case "LEASTPERIODBETWEENTWOOCCURRENCECLAIMSPERCOVER":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "LeastPeriodBetweenTwoOccurrenceClaimsPerCover");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "LeastPeriodBetweenTwoOccurrenceClaimsPerCover"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_LEASTPERIODBETWEENTWOOCCURRENCECLAIMSPERCOVER");
                                }
                                return true;
                            }
                        }

                        break;
                    case "NUMBEROFADHERENTSINPREVIOUSPOLICYATEXPIRYDATE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfAdherentsInPreviousPolicyAtExpiryDate");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFADHERENTSINPREVIOUSPOLICYATEXPIRYDATE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;
                    case "NUMBEROFADHERENTSINPREVIOUSPOLICYATEFFECTIVEDATE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "NumberOfAdherentsInPreviousPolicyAtEffectiveDate");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_NUMBEROFADHERENTSINPREVIOUSPOLICYATEFFECTIVEDATE");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        break;

                    case "ISSANCTIONEDNATIONALITY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsSanctionedNationality");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISSANCTIONEDNATIONALITY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;

                    // todo giorgio 20/6/2024 LIA-3493
                    case "ISSUBBROKERCOMGREATERTHANBROKERCOM":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsSubBrokerComGreaterThanBrokerCom");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISSUBBROKERCOMGREATERTHANBROKERCOM");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    // end todo giorgio 20/6/2024 LIA-3493

                    case "ISSANCTIONEDSMACOUNTRY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsSanctionedSMACountry");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISSANCTIONEDSMACOUNTRY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case "ISSANCTIONEDSMANATIONALITY"://TODO EliasN 24-05-2023 DEVIRIS5-11970
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsSanctionedSMANationality");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_chunk.Trim().Replace(" ", "") == "")
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISSANCTIONEDSMANATIONALITY");
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;

                    case "ISPOLICYEXISTFORSAMEPROPERTY":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsPolicyExistForSameProperty");
                        }

                        //if (_szChunk.IndexOf("(") != -1)
                        //{
                        //    _szChunk = _szChunk.Remove(0, _szChunk.Substring(0, _szChunk.IndexOf("(")).Length + 1);
                        //    _szChunk = _szChunk.Remove(_szChunk.Length - 1, 1);
                        //    if (this.CheckLogic(_szChunk + " ", ChunkType.Functions, _CheckOnly, true, "IsPolicyExistForSameProperty"))
                        //    {
                        //        if (!_CheckOnly)
                        //        {
                        //            this.WriteXML_Node("pre_EndFunction", "END_ISPOLICYEXISTFORSAMEPROPERTY");
                        //        }
                        //        return true;
                        //    }
                        //}

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);
                            _chunk = _chunk.Remove(0, 1); // to remove the first (
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1); // to remove the last )

                            var passed = false;

                            if (_chunk.Contains(","))
                            {
                                var prp = _chunk.Split(",");

                                for (int i = 0; i < prp.Length; i++)
                                {
                                    passed = _checkLogic(prp[i].ToUpper(), ChunkType.Functions, _checkOnly, true, "IsPolicyExistForSameProperty");
                                    if (passed == false)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                passed = _checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "IsPolicyExistForSamePropertySameCob");
                            }

                            if (passed)
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISPOLICYEXISTFORSAMEPROPERTY");
                                }
                            }

                            return true;
                        }
                        break;

                    case "ISPOLICYEXISTFORSAMEPROPERTYSAMECOB":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "IsPolicyExistForSamePropertySameCob");
                        }

                        //if (_szChunk.IndexOf("(") != -1)
                        //{
                        //    _szChunk = _szChunk.Remove(0, _szChunk.Substring(0, _szChunk.IndexOf("(")).Length + 1);
                        //    _szChunk = _szChunk.Remove(_szChunk.Length - 1, 1);
                        //    if (this.CheckLogic(_szChunk + " ", ChunkType.Functions, _CheckOnly, true, "IsPolicyExistForSamePropertySameCob"))
                        //    {
                        //        if (!_CheckOnly)
                        //        {
                        //            this.WriteXML_Node("pre_EndFunction", "END_ISPOLICYEXISTFORSAMEPROPERTYSAMECOB");
                        //        }
                        //        return true;
                        //    }
                        //}

                        // VALID FOR 2 properties only currently
                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length);
                            _chunk = _chunk.Remove(0, 1); // to remove the first (
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1); // to remove the last )

                            var passed = false;
                            if (_chunk.Contains(","))
                            {
                                var prp = _chunk.Split(",");

                                for (int i = 0; i < prp.Length; i++)
                                {
                                    passed = _checkLogic(prp[i].ToUpper(), ChunkType.Functions, _checkOnly, true, "IsPolicyExistForSamePropertySameCob");
                                    if (passed == false)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                passed = _checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "IsPolicyExistForSamePropertySameCob");
                            }

                            if (passed)
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_ISPOLICYEXISTFORSAMEPROPERTYSAMECOB");
                                }
                            }

                            return true;
                        }

                        break;

                    // TODO CHARBELAS 26-11-2024 AXA-4647
                    case "POLICYHOLDERTAGINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyHolderTagInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicyHolderTagInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYHOLDERTAGINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;
                    // TODO CHARBELAS 26-11-2024 AXA-4647


                    //AN  2026-03-26 copied from IRIS 2
                    case "POLICYCLAUSEINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyClauseInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicyClauseInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYCLAUSEINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;

                    case "POLICYCOVERRATE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyCoverRate");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicyCoverRate"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYCOVERRATE");
                                }
                                return true;
                            }
                        }

                        break;

                    case "POLICYCOVERSUMINSURED":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyCoverSumInsured");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicyCoverSumInsured"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYCOVERSUMINSURED");
                                }
                                return true;
                            }
                        }

                        break;

                    case "POLICYCOVERPREMIUM":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicyCoverSumPremium");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicyCoverSumPremium"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYCOVERPREMIUM");
                                }
                                return true;
                            }
                        }

                        break;

                    case "POLICYSUBCOVERINCLUDE":
                        if (!_checkOnly)
                        {
                            _writeXML_Node("pre_StartFunction", "PolicySubCoverInclude");
                        }

                        if (_chunk.IndexOf("(") != -1)
                        {
                            _chunk = _chunk.Remove(0, _chunk.Substring(0, _chunk.IndexOf("(")).Length + 1);
                            _chunk = _chunk.Remove(_chunk.Length - 1, 1);

                            if (_checkLogic(_chunk + " ", ChunkType.Functions, _checkOnly, true, "PolicySubCoverInclude"))
                            {
                                if (!_checkOnly)
                                {
                                    _writeXML_Node("pre_EndFunction", "END_POLICYSUBCOVERINCLUDE");
                                }
                                return true;
                            }
                        }

                        break;
                        //AN  2026-03-26 copied from IRIS 2

                }
            }
                    
            return false;
        }

        private string _getChunkText(int _iOpenStart, ref int _iCloseStartNo, string _text)
        {
            string szChunk = "";
            if (_text.IndexOf("(", _iOpenStart) != -1)
            {

                if (_text.IndexOf("(", _iOpenStart) < _text.IndexOf(")", _iOpenStart))
                {
                    int temp_iCloseStartNo1 = _iCloseStartNo + 1;
                    szChunk = _getChunkText(_text.IndexOf("(", _iOpenStart) + 1, ref temp_iCloseStartNo1, _text);
                }
                else
                {
                    int iOpenNo = 0;
                    int iCloseNo = 0;
                    for (int i = 0; i <= _text.IndexOf("(", _iOpenStart); i++)
                    {
                        if (_text[i] == '(')
                        {
                            iOpenNo += 1;
                        }
                        else if (_text[i] == ')')
                        {
                            iCloseNo += 1;
                        }
                    }

                    if (iOpenNo > _iCloseStartNo && iOpenNo - iCloseNo > 1)
                    {
                        int temp_iCloseStartNo2 = _iCloseStartNo + 1;
                        szChunk = _getChunkText(_text.IndexOf("(", _iOpenStart) + 1, ref temp_iCloseStartNo2, _text);
                    }
                    else
                    {
                        int iIndex = 0;
                        for (int i = 0; i < _iCloseStartNo; i++)
                        {
                            iIndex = _text.IndexOf(")", iIndex + 1);
                        }
                        szChunk = _text.Substring(0, iIndex + 1);
                    }
                }
            }
            else
            {
                int iIndex = 0;
                for (int i = 0; i < _iCloseStartNo; i++)
                {
                    iIndex = _text.IndexOf(")", iIndex + 1);
                }
                szChunk = _text.Substring(0, iIndex + 1);
            }
            return szChunk;
        }

        // giorgio 15-dec-2025 SFA-1180
        //private ChunkType _getChunkType(string _szChunk, bool _CheckOnly, bool _FunctionAttribute)//, ref string m_tableName)
        private ChunkType _getChunkType(string _szChunk, bool _CheckOnly, bool _FunctionAttribute, string _functionName)
        {
            string szPrefix = null;
            if (_FunctionAttribute)
            {
                szPrefix = "pre_";
            }
            else
            {
                szPrefix = "p_";
            }

            switch (_szChunk.Trim().ToUpper())
            {
                case "=":
                case ">":
                case ">=":
                case "<":
                case "<=":
                case "<>":
                    if (!_CheckOnly)
                    {
                        _writeXML_Node(szPrefix + "Operator", _szChunk.ToUpper());
                    }
                    return ChunkType.Operators;

                case "(":
                    if (!_CheckOnly)
                    {
                        _writeXML_Node(szPrefix + "LeftParenthesis", _szChunk.ToUpper());
                    }
                    return ChunkType.LeftParenthesis;

                case ")":
                    if (!_CheckOnly)
                    {
                        _writeXML_Node(szPrefix + "RightParenthesis", _szChunk.ToUpper());
                    }
                    return ChunkType.RightParenthesis;

                case "AND":
                case "OR":
                    if (!_CheckOnly)
                    {
                        _writeXML_Node(szPrefix + "LogOperator", _szChunk.ToUpper());
                    }
                    return ChunkType.LogicalOperators;

                default:
                    switch (_szChunk.TrimStart().Substring(0, 1))
                    {
                        case "{":
                            var szFieldToCheck = _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2).ToUpper();

                            switch (szFieldToCheck)
                            {
                                case "DATE_ISSUE":
                                case "DATE_EFFECTIVE":
                                case "DATE_EXPIRY":
                                case "MASTER_POLICY_EFFECTIVE":
                                case "MASTER_POLICY_EXPIRY":
                                case "MOTHER_POLICY_EFFECTIVE":
                                case "MOTHER_POLICY_EXPIRY":
                                case "DATE_PROPOSAL":
                                case "DATE_ELIGIBILITY":
                                case "AUTHORITY_LEVEL":
                                case "BRANCH":
                                case "PRODUCT":
                                case "PACKAGE":
                                case "BROKER":
                                case "END_TYPE":
                                case "END_SUB_TYPE":
                                case "PREMIUM_CURRENCY":
                                case "SUM_INSURED_CURRENCY":
                                case "POLICY_SUM_INSURED":
                                case "POLICY_PREMIUM":
                                case "LOB":
                                case "COB":
                                case "PAYER_NATIONALITY":
                                case "SOURCE_CHANNEL":
                                case "IS_GROUP":
                                case "IS_FACULTATIVE":
                                case "IS_TAX_EXEMPTED":
                                case "USER_ROLE":
                                case "SUB_BROKER":
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionPolicy", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }

                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.PolicyFields;

                                case "COMPETITOR_DATE_EFFECTIVE":
                                case "COMPETITOR_DATE_EXPIRY":
                                case "COMPETITOR_CO_INS":
                                case "COMPETITOR_GR":
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionCompetitorPolicy", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }

                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.CompetitorPolicy;

                                case "LINKED_DATE_ISSUE":
                                case "LINKED_DATE_EFFECTIVE":
                                case "LINKED_DATE_EXPIRY":
                                case "LINKED_BRANCH":
                                case "LINKED_PRODUCT":
                                case "LINKED_BROKER":
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionLinkedTo", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }

                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.LinkedToFields;

                                case "DATETODAY":
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionSystem", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }

                                    return ChunkType.SystemValues;

                                case "INSURED_AGE":
                                case "INSURED_DOB": // AN 2026-02-02
                                case "CHILD_AGE":
                                case "INSURED_GENDER":
                                case "INSURED_MARITAL_STATUS":
                                case "INSURED_CONTACT_TYPE":
                                case "INSURED_CATEGORY":
                                case "INSURED_TAG":
                                case "INSURED_RELATION":
                                case "INSURED_NATIONALITY":
                                case "INSURED_SECOND_NATIONALITY":
                                case "INSURED_COUNTRY_OF_RESIDENCE":
                                case "INSURED_PROFESSION":
                                case "INSURED_PROFESSION_CLASS":
                                case "INSURED_SPOUSE_PROFESSION":
                                case "INSURED_SPOUSE_NAME":
                                case "INSURED_CODIFICATION_COMPLETED":
                                case "INSURED_IS_USA_RESIDENT":
                                case "INSURED_USA_TIN":
                                case "INSURED_IDENTITY_NO":
                                case "INSURED_TRN":
                                case "INSURED_FRN":
                                case "INSURED_CRN":
                                case "INSURED_PASSPORT_NO":
                                case "INSURED_RESIDENCY_TIN":
                                case "INSURED_TAX_RESIDENCY":

                                case "INSURED_DRIVING_LICENSE_ID":
                                case "INSURED_DRIVING_LICENSE_ISSUED_ON":
                                case "INSURED_DRIVING_LICENSE_EXPIRES_ON":

                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionPolicyInsured", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }

                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.PolicyInsured;

                                case "BROKER_NATIONALITY":
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionPolicyBroker", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }

                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.PolicyBroker;

                                case "PROFILE_TYPE":
                                case "PROFILE_GENDER":
                                case "PROFILE_AGE":
                                case "PROFILE_MARITAL_STATUS":
                                case "PROFILE_CATEGORY":
                                case "PROFILE_TAG":
                                case "PROFILE_CONTACT_TYPE":
                                case "PROFILE_NATIONALITY":
                                case "PROFILE_SECOND_NATIONALITY":
                                case "PROFILE_COUNTRY_OF_RESIDENCE":
                                case "PROFILE_PROFESSION":
                                case "PROFILE_SPOUSE_PROFESSION":
                                case "PROFILE_SPOUSE_NAME":
                                case "PROFILE_CODIFICATION_COMPLETED":
                                case "PROFILE_IS_USA_RESIDENT":
                                case "PROFILE_USA_TIN":
                                case "PROFILE_IDENTITY_NO":
                                case "PROFILE_PASSPORT_NO":
                                case "PROFILE_TAX_RESIDENCY":
                                case "PROFILE_RESIDENCY_TIN":
                                case "PROFILE_PREVIOUS_PROFESSION":
                                case "PROFILE_MONTHLY_INCOME":
                                case "PROFILE_OTHER_SOURCE_OF_FUNDS":
                                case "PROFILE_USED_AS_PAYER":
                                //TODO ELIASN 18-MAR-2024 AXA-4395
                                case "PROFILE_TRN":
                                case "PROFILE_FRN":
                                case "PROFILE_CRN":
                                    //TODO ELIASN 18-MAR-2024 AXA-4395
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionScopeProfile", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }
                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.ScopeProfile;

                                case "COLLECTION_AMOUNT":
                                case "COLLECTION_CURRENCY":
                                case "COLLECTION_PROFILE_NATIONALITY":
                                case "COLLECTION_PROFILE_COUNTRY_OF_RESIDENCE":
                                case "COLLECTION_PROFILE_PROFESSION":
                                case "COLLECTION_PROFILE_TYPE":
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionScopeCollection", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }
                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.ScopeCollection;

                                case "CLAIM_PO_AMOUNT":
                                case "CLAIM_PO_CURRENCY":
                                case "CLAIM_PO_PROFILE_NATIONALITY":
                                case "CLAIM_PO_PROFILE_COUNTRY_OF_RESIDENCE":
                                case "CLAIM_PO_PROFILE_PROFESSION":
                                case "CLAIM_PO_PROFILE_TYPE":
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionScopeClaimPO", _szChunk.TrimStart().Substring(1, _szChunk.TrimStart().Length - 2));
                                    }
                                    m_fieldToValidate = szFieldToCheck;

                                    return ChunkType.ScopeCollection;

                                default:

                                    //TODO: TONY Attention we are looping thru all the tables including diseases
                                    //if (szTony.Substring(0, 4) == "SMA_")
                                    //{
                                    //}      

                                    m_fieldToValidate = szFieldToCheck;

                                    //TODO ELIASN 11-JUNE-2024 LIA-3374
                                    double output;
                                    if (double.TryParse(m_fieldToValidate, out output))
                                    {
                                        if (!_CheckOnly)
                                        {
                                            _writeXML_Node(szPrefix + "Number", m_fieldToValidate);
                                        }
                                        return ChunkType.Number;
                                    }
                                    //TODO ELIASN 11-JUNE-2024 LIA-3374

                                    // giorgio 15-dec-2025 SFA-1180 added the if statement
                                    if (_functionName == string.Empty || smaSupporttedFunctions.Contains(_functionName.ToUpper()))
                                    {
                                        for (var i = 0; i < m_listSmaProperty.Count; i++)
                                        {
                                            if (_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2).ToUpper() == m_listSmaProperty.ElementAt(i).property_desc.ToUpper())
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionSma", m_listSmaProperty.ElementAt(i).id.ToString());
                                                }
                                                this.m_LinkedToPropertyID = m_listSmaProperty.ElementAt(i).id;

                                                return ChunkType.SmaProperties;
                                            }
                                        }
                                    }

                                    //if (_szChunk.TrimStart().Substring(0, 9).ToUpper() == "QUESTION_")
                                    //{
                                    //}
                                    for (int i = 0; i < m_listQuestion.Count; i++)
                                    {
                                        if (_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2).ToUpper() == m_listQuestion.ElementAt(i).question_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionTechnicalQuestions", m_listQuestion.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.UwQuestions;
                                        }
                                    }

                                    //if (_szChunk.TrimStart().Substring(0, 5).ToUpper() == "FLAG_")
                                    //{
                                    //}
                                    for (var i = 0; i < m_listFlag.Count; i++)
                                    {
                                        if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listFlag.ElementAt(i).flag_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionConditionalFlags", m_listFlag.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.ConditionalFlags;
                                        }
                                    }

                                    //if (_szChunk.TrimStart().Substring(0, 7).ToUpper() == "COMPUT_")
                                    //{
                                    //}

                                    for (int i = 0; i < m_listComputFraction.Count; i++)
                                    {
                                        if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listComputFraction.ElementAt(i).comput_fraction_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionPolicyComputation", m_listComputFraction.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.PolicyComputation;
                                        }
                                    }
                                                                    

                                    //for (var i = 0; i < m_listBRuleAction.Count; i++)
                                    //{
                                    //    if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listBRuleAction.ElementAt(i).type_desc.ToUpper())
                                    //    {
                                    //        if (!_CheckOnly)
                                    //        {
                                    //            this.WriteXML_Node(szPrefix + "DimensionPolicyAction", m_listBRuleAction.ElementAt(i).id.ToString());
                                    //        }

                                    //        return ChunkType.PolicyAction;
                                    //    }
                                    //}

                                    //if (_szChunk.TrimStart().Substring(0, 6).ToUpper() == "COVER_")
                                    //{
                                    //}

                                    if (this.p_isLife)
                                    {
                                        for (var i = 0; i < m_LifeListCover.Count; i++)
                                        {
                                            if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_LifeListCover.ElementAt(i).cover_desc.ToUpper())
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionCovers", m_LifeListCover.ElementAt(i).id.ToString());
                                                }

                                                return ChunkType.Covers;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (var i = 0; i < m_listCover.Count; i++)
                                        {
                                            if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listCover.ElementAt(i).cover_desc.ToUpper())
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionCovers", m_listCover.ElementAt(i).id.ToString());
                                                }

                                                return ChunkType.Covers;
                                            }
                                        }

                                        for (var i = 0; i < m_listDed.Count; i++)
                                        {
                                            if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listDed.ElementAt(i).ded_desc.ToUpper())
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionCovers", m_listDed.ElementAt(i).id.ToString());
                                                }

                                                return ChunkType.Covers;
                                            }
                                        }
                                    }

                                    // AN 2026-03-26
                                    if (this.p_isLife)
                                    {
                                        for (var i = 0; i < m_lifeListClause.Count; i++)
                                        {
                                            if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_lifeListClause.ElementAt(i).clause_code.ToUpper())
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionClauses", m_lifeListClause.ElementAt(i).id.ToString());
                                                }

                                                return ChunkType.Covers;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        for (var i = 0; i < m_listClause.Count; i++)
                                        {
                                            if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listClause.ElementAt(i).clause_code.ToUpper())
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionClauses", m_listClause.ElementAt(i).id.ToString());
                                                }

                                                return ChunkType.Covers;
                                            }
                                        }
                                    }
                                    // AN 2026-03-26

                                    // giorgio 15-dec-2025 SFA-1180 - duplicated code
                                    //for (var i = 0; i < m_listSmaProperty.Count; i++)
                                    //{
                                    //    if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listSmaProperty.ElementAt(i).property_desc.ToUpper())
                                    //    {
                                    //        if (!_CheckOnly)
                                    //        {
                                    //            _writeXML_Node(szPrefix + "DimensionSma", m_listSmaProperty.ElementAt(i).id.ToString());
                                    //        }

                                    //        return ChunkType.SmaProperties;
                                    //    }
                                    //}
                                    // end giorgio 15-dec-2025 SFA-1180 - duplicated code

                                    //if (_szChunk.TrimStart().Substring(0, 8).ToUpper() == "DISEASE_")
                                    //{
                                    //}
                                    for (var i = 0; i < m_listDisease.Count; i++)
                                    {
                                        if (_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2).ToUpper() == m_listDisease.ElementAt(i).disease_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionDiseases", m_listDisease.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.Diseases;
                                        }
                                    }

                                    //if (_szChunk.TrimStart().Substring(0, 8).ToUpper() == "ASSTYPE_")
                                    //{
                                    //}
                                    for (var i = 0; i < m_listAssessmentType.Count; i++)
                                    {
                                        if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listAssessmentType.ElementAt(i).assessment_type_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionAssessmentTypes", m_listAssessmentType.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.AssessmentTypes;
                                        }
                                    }

                                    //if (_szChunk.TrimStart().Substring(0, 8).ToUpper() == "DEDTYPE_")
                                    //{
                                    //}
                                    for (var i = 0; i < m_listDedType.Count; i++)
                                    {
                                        if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listDedType.ElementAt(i).type_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionDeductibleType", m_listDedType.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.DeductibleType;
                                        }
                                    }

                                    //if (_szChunk.TrimStart().Substring(0, 8).ToUpper() == "LIMTYPE_")
                                    //{
                                    //}
                                    for (var i = 0; i < m_listLimitationType.Count; i++)
                                    {
                                        if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listLimitationType.ElementAt(i).type_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionLimitationType", m_listLimitationType.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.LimitationType;
                                        }
                                    }

                                    //END TODO: TONY Attention we are looping thru all the tables including diseases

                                    for (var i = 0; i < m_listTag.Count; i++)
                                    {
                                        if (Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper() == m_listTag.ElementAt(i).tag_desc.ToUpper())
                                        {
                                            if (!_CheckOnly)
                                            {
                                                _writeXML_Node(szPrefix + "DimensionTags", m_listTag.ElementAt(i).id.ToString());
                                            }

                                            return ChunkType.ProfileTags;
                                        }
                                    }

                                    //TODO: ADD System TABLES LIST

                                    return ChunkType.Undefined;
                            }
                        //break;
                        case "[":
                            var szValueUC = _szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2).ToUpper();
                            var szValue = _szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2);

                            if (this.m_LinkedToPropertyID != -1)
                            {
                                bool blValueFound = false;
                                if (szValueUC != "TRUE" && szValueUC != "FALSE")
                                {
                                    #region Sma

                                    for (var i = 0; i < m_listSmaProperty.Count; i++)
                                    {
                                        if (m_listSmaProperty.ElementAt(i).id == this.m_LinkedToPropertyID)
                                        {
                                            switch (m_listSmaProperty.ElementAt(i).property_type_id)
                                            {
                                                case (int)Enums.SMAPropertyType.String:
                                                    if (!_CheckOnly)
                                                    {
                                                        _writeXML_Node(szPrefix + "DimensionValue", szValue);
                                                    }
                                                    blValueFound = true;
                                                    break;
                                                case (int)Enums.SMAPropertyType.Age:
                                                case (int)Enums.SMAPropertyType.Integer:
                                                    if (!_CheckOnly)
                                                    {
                                                        Int32 num1;
                                                        if (Int32.TryParse(szValue, out num1))
                                                        {
                                                            _writeXML_Node(szPrefix + "DimensionValue", Convert.ToInt32(szValue).ToString());
                                                            blValueFound = true;
                                                        }
                                                    }

                                                    break;
                                                case (int)Enums.SMAPropertyType.Decimal:
                                                    if (!_CheckOnly)
                                                    {
                                                        decimal num1;
                                                        if (decimal.TryParse(szValue, out num1))
                                                        {
                                                            _writeXML_Node(szPrefix + "DimensionValue", Convert.ToDecimal(szValue).ToString());
                                                            blValueFound = true;
                                                        }
                                                    }

                                                    break;
                                                case (int)Enums.SMAPropertyType.Bit:
                                                    if (!_CheckOnly)
                                                    {
                                                        bool num11;
                                                        if (bool.TryParse(szValue, out num11))
                                                        {
                                                            _writeXML_Node(szPrefix + "DimensionValue", Convert.ToBoolean(szValue).ToString());
                                                            blValueFound = true;
                                                        }
                                                    }

                                                    break;
                                                case (int)Enums.SMAPropertyType.ReadFromTable:
                                                    {
                                                        var query = (from a in m_listSmaPropertyTableValue
                                                                     where a.property_table_id == m_listSmaProperty.ElementAt(i).property_table_id
                                                                     select new { a.id, a.the_value });

                                                        for (int ii = 0; ii < query.Count(); ii++)
                                                        {
                                                            if (szValueUC == query.ElementAt(ii).the_value.Trim(' ').ToUpper())
                                                            {
                                                                if (!_CheckOnly)
                                                                {
                                                                    _writeXML_Node(szPrefix + "DimensionValue", query.ElementAt(ii).id.ToString());
                                                                }

                                                                blValueFound = true;

                                                                break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                case (int)Enums.SMAPropertyType.ReadFromContact:
                                                    switch (m_listSmaProperty.ElementAt(i).contact_field_property_type_id)
                                                    {
                                                        case (int)Enums.SMAPropertyType.String:
                                                            if (!_CheckOnly)
                                                            {
                                                                _writeXML_Node(szPrefix + "DimensionValue", szValue);
                                                            }
                                                            blValueFound = true;
                                                            break;
                                                        case (int)Enums.SMAPropertyType.Age:
                                                        case (int)Enums.SMAPropertyType.Integer:
                                                            if (!_CheckOnly)
                                                            {
                                                                Int32 num3;
                                                                if (Int32.TryParse(szValue, out num3))
                                                                {
                                                                    _writeXML_Node(szPrefix + "DimensionValue", Convert.ToInt32(szValue).ToString());
                                                                    blValueFound = true;
                                                                }
                                                            }

                                                            break;
                                                        case (int)Enums.SMAPropertyType.Decimal:
                                                            if (!_CheckOnly)
                                                            {
                                                                decimal num2;
                                                                if (decimal.TryParse(_szChunk.Substring(1, _szChunk.Length - 2), out num2))
                                                                {
                                                                    _writeXML_Node(szPrefix + "DimensionValue", Convert.ToDecimal(szValue).ToString());
                                                                    blValueFound = true;
                                                                }
                                                            }

                                                            break;
                                                        case (int)Enums.SMAPropertyType.Bit:
                                                            if (!_CheckOnly)
                                                            {
                                                                bool num12;
                                                                if (bool.TryParse(szValue, out num12))
                                                                {
                                                                    _writeXML_Node(szPrefix + "DimensionValue", Convert.ToBoolean(szValue).ToString());
                                                                    blValueFound = true;
                                                                }
                                                            }

                                                            break;
                                                        case (int)Enums.SMAPropertyType.ReadFromTable:
                                                            {
                                                                var query = (from a in m_listSmaPropertyTableValue
                                                                             where a.property_table_id == m_listSmaProperty.ElementAt(i).property_table_id
                                                                             select new { a.id, a.the_value });

                                                                for (int ii = 0; ii < query.Count(); ii++)
                                                                {
                                                                    if (szValueUC == query.ElementAt(ii).the_value.Trim(' ').ToUpper())
                                                                    {
                                                                        if (!_CheckOnly)
                                                                        {
                                                                            _writeXML_Node(szPrefix + "DimensionValue", query.ElementAt(ii).id.ToString());
                                                                        }
                                                                        blValueFound = true;

                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        case (int)Enums.SMAPropertyType.Date:
                                                            if (!_CheckOnly)
                                                            {
                                                                DateTime num122;
                                                                if (DateTime.TryParse(szValue, out num122))
                                                                {
                                                                    _writeXML_Node(szPrefix + "DimensionValue", Convert.ToDateTime(szValue).ToString());
                                                                    blValueFound = true;
                                                                }
                                                            }

                                                            break;
                                                        case (int)Enums.SMAPropertyType.ReadFromSystemTable:
                                                            {
                                                                var query = (from a in m_listSystemTable
                                                                             where a.table_type == m_listSmaProperty.ElementAt(i).system_table
                                                                             select new { a.id, a.the_desc });

                                                                for (var ii = 0; ii < query.Count(); ii++)
                                                                {
                                                                    if (query.ElementAt(ii).the_desc != null)
                                                                    {
                                                                        if (szValueUC == query.ElementAt(ii).the_desc.Trim(' ').ToUpper())
                                                                        {
                                                                            if (!_CheckOnly)
                                                                            {
                                                                                _writeXML_Node(szPrefix + "DimensionValue", query.ElementAt(ii).id.ToString());
                                                                            }
                                                                            blValueFound = true;

                                                                            break;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                    }
                                                    break;
                                                case (int)Enums.SMAPropertyType.Date:
                                                    if (!_CheckOnly)
                                                    {
                                                        DateTime num221;
                                                        if (DateTime.TryParse(szValue, out num221))
                                                        {
                                                            _writeXML_Node(szPrefix + "DimensionValue", Convert.ToDateTime(szValue).ToString());
                                                            blValueFound = true;
                                                        }
                                                    }

                                                    break;
                                                case (int)Enums.SMAPropertyType.ReadFromSystemTable:
                                                    {
                                                        var query = (from a in m_listSystemTable
                                                                     where a.table_type == m_listSmaProperty.ElementAt(i).system_table
                                                                     select new { a.id, a.the_desc });

                                                        for (int ii = 0; ii < query.Count(); ii++)
                                                        {
                                                            if (query.ElementAt(ii).the_desc != null)
                                                            {
                                                                if (szValueUC == query.ElementAt(ii).the_desc.Trim(' ').ToUpper())
                                                                {
                                                                    if (!_CheckOnly)
                                                                    {
                                                                        _writeXML_Node(szPrefix + "DimensionValue", query.ElementAt(ii).id.ToString());
                                                                    }
                                                                    blValueFound = true;

                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }

                                            this.m_LinkedToPropertyID = -1;
                                            if (blValueFound == false)
                                            {
                                                return ChunkType.Undefined;
                                            }
                                            return ChunkType.DimensionValue;
                                        }
                                    }

                                    #endregion Sma
                                }
                                else
                                {
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionValue", szValue);
                                    }
                                    blValueFound = true;
                                    this.m_LinkedToPropertyID = -1;

                                    return ChunkType.DimensionValue;
                                }
                            } ///////
                            else // NOT m_LinkedToPropertyID
                            {
                                //TONY 17-MAR-2023 added  m_fieldToValidate != "BROKER" && m_fieldToValidate != "SUB_BROKER" because the system was saving the code not the id
                                if (Pixel.Core.Lib.CoreHelper.IsNumeric(szValue) &&
                                    m_fieldToValidate != "BROKER" &&
                                    m_fieldToValidate != "SUB_BROKER" &&
                                    m_fieldToValidate != "LOB" &&
                                    m_fieldToValidate != "COB")
                                {
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionValue", szValueUC);
                                    }
                                    return ChunkType.DimensionValue;
                                }
                                else if (Pixel.Core.Lib.CoreHelper.IsDate(szValue))
                                {
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionValue", Convert.ToDateTime(szValue).ToString());
                                    }
                                    return ChunkType.DimensionValue;
                                }
                                //else if (szValueUC == "TRUE" || szValueUC == "FALSE")
                                //{
                                //    if (!_CheckOnly)
                                //    {
                                //        this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                //    }
                                //    return ChunkType.DimensionValue;
                                //}
                                else if (szValueUC == "TRUE")
                                {
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionValue", "1");
                                    }
                                    return ChunkType.DimensionValue;
                                }
                                else if (szValueUC == "FALSE")
                                {
                                    if (!_CheckOnly)
                                    {
                                        _writeXML_Node(szPrefix + "DimensionValue", "0");
                                    }
                                    return ChunkType.DimensionValue;
                                }
                                else
                                {
                                    //if (_szChunk.TrimStart().Substring(0, 8).ToUpper() == "RELATION_")
                                    //{
                                    //}

                                    if (m_fieldToValidate == "PROFILE_TYPE" ||
                                        m_fieldToValidate == "CLAIM_PO_PROFILE_TYPE")
                                    {
                                        for (var i = 0; i < m_listProfileType.Count; i++)
                                        {
                                            if (m_listProfileType.ElementAt(i).profile_type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listProfileType.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "INSURED_NATIONALITY" ||
                                        m_fieldToValidate == "INSURED_SECOND_NATIONALITY" ||
                                        m_fieldToValidate == "BROKER_NATIONALITY" ||
                                        m_fieldToValidate == "PAYER_NATIONALITY" ||
                                        m_fieldToValidate == "PROFILE_NATIONALITY" ||
                                        m_fieldToValidate == "PROFILE_SECOND_NATIONALITY" ||
                                        m_fieldToValidate == "COLLECTION_PROFILE_NATIONALITY" ||
                                        m_fieldToValidate == "CLAIM_PO_PROFILE_NATIONALITY")
                                    {
                                        for (var i = 0; i < m_listNationality.Count; i++)
                                        {
                                            if (m_listNationality.ElementAt(i).nationality_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listNationality.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "PROFILE_TAX_RESIDENCY")
                                    {
                                        for (var i = 0; i < m_listCountry.Count; i++)
                                        {
                                            if (m_listCountry.ElementAt(i).country_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listCountry.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "GENDER" ||
                                        m_fieldToValidate == "INSURED_GENDER" ||
                                        m_fieldToValidate == "PROFILE_GENDER")
                                    {
                                        for (var i = 0; i < m_listGender.Count; i++)
                                        {
                                            if (m_listGender.ElementAt(i).type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listGender.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "SOURCE_CHANNEL")
                                    {
                                        for (var i = 0; i < m_listSourceChannel.Count; i++)
                                        {
                                            if (m_listSourceChannel.ElementAt(i).type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listSourceChannel.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "MARITAL_STATUS" ||
                                        m_fieldToValidate == "INSURED_MARITAL_STATUS" ||
                                        m_fieldToValidate == "PROFILE_MARITAL_STATUS")
                                    {
                                        for (var i = 0; i < m_listMaritalStatus.Count; i++)
                                        {
                                            if (m_listMaritalStatus.ElementAt(i).type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listMaritalStatus.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "INSURED_CONTACT_TYPE" ||
                                         m_fieldToValidate == "PROFILE_CONTACT_TYPE")
                                    {
                                        for (var i = 0; i < m_listContactType.Count; i++)
                                        {
                                            if (m_listContactType.ElementAt(i).type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listContactType.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "INSURED_CATEGORY" ||
                                        m_fieldToValidate == "PROFILE_CATEGORY")
                                    {
                                        for (var i = 0; i < m_listCategory.Count; i++)
                                        {
                                            if (m_listCategory.ElementAt(i).category_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listCategory.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "INSURED_TAG" ||
                                        m_fieldToValidate == "PROFILE_TAG")
                                    {
                                        for (var i = 0; i < m_listTag.Count; i++)
                                        {
                                            if (m_listTag.ElementAt(i).tag_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    //this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listTag.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "RELATION" ||
                                        m_fieldToValidate == "INSURED_RELATION")
                                    {
                                        for (var i = 0; i < m_listRelation.Count; i++)
                                        {
                                            if (m_listRelation.ElementAt(i).relation_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listRelation.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "INSURED_COUNTRY_OF_RESIDENCE" ||
                                        m_fieldToValidate == "INSURED_TAX_RESIDENCY" ||
                                        m_fieldToValidate == "PROFILE_COUNTRY_OF_RESIDENCE" ||
                                        m_fieldToValidate == "COLLECTION_PROFILE_COUNTRY_OF_RESIDENCE" ||
                                        m_fieldToValidate == "PROFILE_TAX_RESIDENCE" ||
                                        m_fieldToValidate == "CLAIM_PO_PROFILE_COUNTRY_OF_RESIDENCE")
                                    {
                                        for (var i = 0; i < m_listCountry.Count; i++)
                                        {
                                            if (m_listCountry.ElementAt(i).country_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listCountry.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "INSURED_PROFESSION" ||
                                        m_fieldToValidate == "PROFILE_PROFESSION" ||
                                        m_fieldToValidate == "INSURED_SPOUSE_PROFESSION" ||
                                        m_fieldToValidate == "PROFILE_SPOUSE_PROFESSION" ||
                                        m_fieldToValidate == "COLLECTION_PROFILE_PROFESSION" ||
                                        m_fieldToValidate == "PROFILE_PREVIOUS_PROFESSION" ||
                                        m_fieldToValidate == "CLAIM_PO_PROFILE_PROFESSION")
                                    {
                                        for (var i = 0; i < m_listProfession.Count; i++)
                                        {
                                            if (m_listProfession.ElementAt(i).profession_code.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listProfession.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "INSURED_PROFESSION_CLASS")
                                    {
                                        for (var i = 0; i < m_listProfessionClass.Count; i++)
                                        {
                                            if (m_listProfessionClass.ElementAt(i).type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listProfessionClass.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "COLLECTION_PROFILE_TYPE" ||
                                        m_fieldToValidate == "CLAIM_PO_PROFILE_TYPE")
                                    {
                                        for (var i = 0; i < m_listProfession.Count; i++)
                                        {
                                            if (m_listProfileType.ElementAt(i).profile_type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listProfileType.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "PROFILE_USA_TIN" ||
                                        m_fieldToValidate == "PROFILE_MONTHLY_INCOME" ||
                                        m_fieldToValidate == "PROFILE_OTHER_SOURCE_OF_FUNDS" ||
                                        m_fieldToValidate == "PROFILE_IDENTITY_NO" ||
                                        m_fieldToValidate == "PROFILE_PASSPORT_NO" ||
                                        m_fieldToValidate == "PROFILE_RESIDENCY_TIN" ||
                                        m_fieldToValidate == "PROFILE_TAX_RESIDENCY" ||
                                        m_fieldToValidate == "PROFILE_SPOUSE_NAME" ||
                                        m_fieldToValidate == "INSURED_SPOUSE_NAME" ||
                                        //TODO ELIASN 18-MAR-2024 AXA-4395
                                        m_fieldToValidate == "PROFILE_CRN" ||
                                        m_fieldToValidate == "PROFILE_FRN" ||
                                        m_fieldToValidate == "PROFILE_TRN"
                                        //TODO ELIASN 18-MAR-2024 AXA-4395
                                        )
                                    {
                                        return ChunkType.DimensionValue;
                                    }

                                    if (m_fieldToValidate == "COB")
                                    {
                                        for (var i = 0; i < m_listCob.Count; i++)
                                        {
                                            //if (m_listCob.ElementAt(i).cob_desc.ToUpper() == szValueUC)
                                            if (m_listCob.ElementAt(i).cob_code.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listCob.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "LOB")
                                    {
                                        for (var i = 0; i < m_listLob.Count; i++)
                                        {
                                            if (m_listLob.ElementAt(i).lob_code.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listLob.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "BRANCH" ||
                                        m_fieldToValidate == "LINKED_BRANCH")
                                    {
                                        for (var i = 0; i < m_listBranch.Count; i++)
                                        {
                                            //if (m_listBranch.ElementAt(i).branch_desc.ToUpper() == Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper())
                                            if (m_listBranch.ElementAt(i).branch_code.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listBranch.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "PRODUCT" ||
                                        m_fieldToValidate == "LINKED_PRODUCT")
                                    {
                                        for (var i = 0; i < m_listProduct.Count; i++)
                                        {
                                            if (m_listProduct.ElementAt(i).prod_code.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listProduct.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "PACKAGE")
                                    {
                                        for (var i = 0; i < m_listPackage.Count; i++)
                                        {
                                            if (m_listPackage.ElementAt(i).pkg_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listPackage.ElementAt(i).id.ToString());
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "BROKER" ||
                                        m_fieldToValidate == "LINKED_BROKER")
                                    {
                                        for (var i = 0; i < m_listBroker.Count; i++)
                                        {
                                            if (m_listBroker.ElementAt(i).profile_code.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listBroker.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "QUESTION")
                                    {
                                        for (var i = 0; i < m_listQuestion.Count; i++)
                                        {
                                            if (m_listQuestion.ElementAt(i).question_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listQuestion.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "FLAG")
                                    {
                                        for (var i = 0; i < m_listFlagValue.Count; i++)
                                        {
                                            if (m_listFlagValue.ElementAt(i).flag_value.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listFlagValue.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "COMPETITOR_CO_INS")
                                    {
                                        for (var i = 0; i < m_listCoIns.Count; i++)
                                        {
                                            if (m_listCoIns.ElementAt(i).co_ins_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listCoIns.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "PREMIUM_CURRENCY" ||
                                        m_fieldToValidate == "SUM_INSURED_CURRENCY" ||
                                        m_fieldToValidate == "COLLECTION_CURRENCY" ||
                                         m_fieldToValidate == "CLAIM_PO_CURRENCY")

                                    {
                                        for (var i = 0; i < m_listCurrency.Count; i++)
                                        {
                                            // giorgio 12-jan-2026 AIAW-452
                                            //if (m_listCurrency.ElementAt(i).cur_desc.ToUpper() == szValueUC)
                                            if (m_listCurrency.ElementAt(i).cur_code.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listCurrency.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "DISEASE")
                                    {
                                        for (var i = 0; i < m_listDisease.Count; i++)
                                        {
                                            if (m_listDisease.ElementAt(i).disease_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listDisease.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "ASSESSMENT_TYPE")
                                    {
                                        for (int i = 0; i < m_listAssessmentType.Count; i++)
                                        {
                                            //if (m_listAssessmentType.ElementAt(i).assessment_type_desc.ToUpper() == Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper())
                                            if (m_listAssessmentType.ElementAt(i).assessment_type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listAssessmentType.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.AssessmentTypes;
                                            }
                                        }
                                    }

                                    if (m_fieldToValidate == "STATUS_REASON")
                                    {
                                        for (int i = 0; i < m_listStatusReason.Count; i++)
                                        {
                                            //if (m_listStatusReason.ElementAt(i).status_reason_desc.ToUpper() == Convert.ToString(_szChunk.Trim().Substring(1, _szChunk.Trim().Length - 2)).ToUpper())
                                            if (m_listStatusReason.ElementAt(i).status_reason_desc.ToUpper() == szValue)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", m_listStatusReason.ElementAt(i).id.ToString());// szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }

                                    //05-OCT-2022
                                    //if (m_fieldToValidate == "AUTHORIZER_ROLE")
                                    //{
                                    //    for (int i = 0; i < m_listAuthorizerRole.Count; i++)
                                    //    {
                                    //        if (m_listAuthorizerRole.ElementAt(i).role_name.ToUpper() == szValueUC)
                                    //        {
                                    //            if (!_CheckOnly)
                                    //            {
                                    //                this.WriteXML_Node(szPrefix + "DimensionValue", szValue);
                                    //            }
                                    //            return ChunkType.DimensionValue;
                                    //        }
                                    //    }
                                    //}
                                    if (m_fieldToValidate == "USER_ROLE")
                                    {
                                        for (int i = 0; i < m_listUserRole.Count; i++)
                                        {
                                            if (m_listUserRole.ElementAt(i).role_name.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    //05-OCT-2022

                                    if (m_fieldToValidate == "AUTHORITY_LEVEL")
                                    {
                                        for (int i = 0; i < m_listAuthorityLevel.Count; i++)
                                        {
                                            if (m_listAuthorityLevel.ElementAt(i).type_desc.ToUpper() == szValueUC)
                                            {
                                                if (!_CheckOnly)
                                                {
                                                    _writeXML_Node(szPrefix + "DimensionValue", szValue);
                                                }
                                                return ChunkType.DimensionValue;
                                            }
                                        }
                                    }
                                    if (m_fieldToValidate == "END_TYPE")
                                    {
                                        if (p_isLife)
                                        {
                                            for (int i = 0; i < m_LifeListEndType.Count; i++)
                                            {
                                                if (m_LifeListEndType.ElementAt(i).end_type_desc.ToUpper() == szValueUC)
                                                {
                                                    if (!_CheckOnly)
                                                    {
                                                        _writeXML_Node(szPrefix + "DimensionValue", m_LifeListEndType.ElementAt(i).end_type.ToString());
                                                    }
                                                    return ChunkType.DimensionValue;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int i = 0; i < m_listEndType.Count; i++)
                                            {
                                                if (m_listEndType.ElementAt(i).end_type_desc.ToUpper() == szValueUC)
                                                {
                                                    if (!_CheckOnly)
                                                    {
                                                        _writeXML_Node(szPrefix + "DimensionValue", m_listEndType.ElementAt(i).end_type.ToString());
                                                    }
                                                    return ChunkType.DimensionValue;
                                                }
                                            }
                                        }

                                    }

                                    if (m_fieldToValidate == "END_SUB_TYPE")
                                    {
                                        if (p_isLife)
                                        {
                                            for (int i = 0; i < m_LifeListEndSubType.Count; i++)
                                            {
                                                if (m_LifeListEndSubType.ElementAt(i).end_sub_type_desc.ToUpper() == szValueUC)
                                                {
                                                    if (!_CheckOnly)
                                                    {
                                                        _writeXML_Node(szPrefix + "DimensionValue", m_LifeListEndSubType.ElementAt(i).id.ToString());
                                                    }
                                                    return ChunkType.DimensionValue;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            for (int i = 0; i < m_listEndSubType.Count; i++)
                                            {
                                                if (m_listEndSubType.ElementAt(i).end_sub_type_desc.ToUpper() == szValueUC)
                                                {
                                                    if (!_CheckOnly)
                                                    {
                                                        _writeXML_Node(szPrefix + "DimensionValue", m_listEndSubType.ElementAt(i).id.ToString());// szValue);
                                                    }
                                                    return ChunkType.DimensionValue;
                                                }
                                            }
                                        }

                                    }

                                    //ADDED BY TONY ON 17-JAN-2018
                                    // NEEDED FOR PROFILE SCOPE
                                    if (m_fieldToValidate == "GEO_ZONE" ||
                                        m_fieldToValidate == "GEO_AREA" ||
                                        m_fieldToValidate == "GEO_CITY" ||
                                        m_fieldToValidate == "GEO_STREET")
                                    {
                                        var query = (from a in m_listSystemTable
                                                     where a.table_type == m_fieldToValidate
                                                     select new { a.id, a.the_code, a.the_desc });

                                        var blValueFound = false;
                                        for (int ii = 0; ii < query.Count(); ii++)
                                        {
                                            //if (query.ElementAt(ii).desc != null)
                                            if (query.ElementAt(ii).the_code != null)
                                            {
                                                if (szValueUC == query.ElementAt(ii).the_code.Trim(' ').ToUpper())
                                                //if (szValueUC == query.ElementAt(ii).desc.Trim(' ').ToUpper())
                                                {
                                                    if (!_CheckOnly)
                                                    {
                                                        _writeXML_Node(szPrefix + "DimensionValue", query.ElementAt(ii).id.ToString());
                                                    }
                                                    blValueFound = true;

                                                    break;
                                                }
                                            }
                                        }
                                        if (blValueFound)
                                        {
                                            return ChunkType.DimensionValue;
                                        }
                                    }
                                    //END ADDED BY TONY ON 17-JAN-2018

                                    return ChunkType.Undefined;
                                }
                            } ///////
                            break;
                        default:
                            if (_checkFunction(_szChunk, _CheckOnly) == true)
                            {
                                //If Not _CheckOnly Then Me.WriteXML_Node("p_Function", UCase(_szChunk))
                                return ChunkType.Functions;
                            }
                            else
                            {
                                return ChunkType.Undefined;
                            }
                    }

                    break;
            }

            return ChunkType.Undefined; // null;
        }

        #endregion  Helper Routines

        #region XML      

        public XDocument p_XDocument;
        private XElement m_XElement;

        void _writeXML_Header()
        {
            this.p_XDocument = new XDocument();

            var xElt1 = new XElement("xml");
            this.p_XDocument.Add(xElt1);

            var xElt2 = new XElement("RuleName");
            xElt1.Add(xElt2);

            this.m_XElement = new XElement("Dimensions");
            xElt2.Add(m_XElement);
        }

        void _writeXML_Node(string _strName, string _strValue)
        {
            var xElt = new XElement(_strName, _strValue);
            m_XElement.Add(xElt);
        }


        // AN 2026-03-26 NOT USED
        //public string ParseXmlToText(string _connStr, string _xmlCondition, string _uiLanguage)
        //{
        //    using (var dbConn = Pixel.Core.Dapper.My.ConnectionFactory())
        //    {
        //        dbConn.ConnectionString = _connStr;
        //        dbConn.Open();

        //        //_loadLists(dbConn);
        //        _loadLists(dbConn, _uiLanguage: _uiLanguage);
        //    }

        //    int Parameter = 0;
        //    int FnParameter = 0;

        //    var stream = new System.IO.StringReader(_xmlCondition);
        //    this.p_XDocument = XDocument.Load(stream);

        //    System.Text.StringBuilder parsedText = new System.Text.StringBuilder();

        //    foreach (XElement element in this.p_XDocument.Descendants())//document.Descendants("Dimensions"))
        //    {
        //        _formulateOutputText(element, ref Parameter, ref FnParameter, ref parsedText);
        //    }

        //    return parsedText.ToString();
        //}

        
        //AN 2026-03-26 NOT USED
        //public string MigrateRules(string _connStr, string _uiLanguage)
        //{
        //    using (var dbConn = Pixel.Core.Dapper.My.ConnectionFactory())
        //    {
        //        dbConn.ConnectionString = _connStr;
        //        dbConn.Open();

        //        // giorgio 16-July-2025 VIA-68
        //        //_loadLists(dbConn);
        //        _loadLists(dbConn, _uiLanguage: _uiLanguage);

        //        var rules = dbConn.Query<CORE_RULE>("SELECT * FROM CORE_RULE WHERE RULE_HTML IS NULL").ToList();
        //        foreach (var rule in rules)
        //        {
        //            int Parameter = 0;
        //            int FnParameter = 0;

        //            var stream = new System.IO.StringReader(rule.rule_condition.ToString());
        //            this.p_XDocument = XDocument.Load(stream);

        //            System.Text.StringBuilder parsedText = new System.Text.StringBuilder();

        //            foreach (XElement element in this.p_XDocument.Descendants())
        //            {
        //                _formulateOutputText(element, ref Parameter, ref FnParameter, ref parsedText);
        //            }

        //            try
        //            {
        //                dbConn.ExecuteScalar("UPDATE CORE_RULE SET RULE_HTML = '" + parsedText.ToString().Replace("'", "''") + "' WHERE ID = " + rule.id);
        //            }
        //            catch (Exception ex)
        //            {
        //            }
        //        }

        //        dbConn.Close();
        //    }

        //    return "Migrated Successfully";
        //}

        //private void _formulateOutputText(XElement _XElement, ref int _Parameter, ref int _FnParameter, ref System.Text.StringBuilder _parsedText)
        //{
        //    var szElementName = _XElement.Name.ToString();

        //    if (szElementName.IndexOf("_") == -1) return;

        //    string xmlText = "";

        //    using (var dbConn = Pixel.Core.Dapper.My.ConnectionFactory())
        //    {
        //        dbConn.ConnectionString = connStr;
        //        dbConn.Open();

        //        switch (szElementName.Substring(0, szElementName.IndexOf("_") + 1))
        //        {
        //            case "p_":
        //                _Parameter = 0;
        //                _FnParameter = 0;
        //                szElementName = szElementName.Remove(0, szElementName.IndexOf("_") + 1);

        //                switch (szElementName)
        //                {
        //                    case "Operator":
        //                    case "LogOperator":
        //                    case "LeftParenthesis":
        //                    case "RightParenthesis":
        //                        //this.RichTextBox1.ChangeTextForeColor(Color.Blue);
        //                        //this.RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(_XElement.Value + " ");
        //                        //this.RichTextBox1.ChangeTextForeColor(Color.Black);

        //                        break;
        //                    case "DimensionSma":
        //                    case "DimensionInterest":
        //                        for (var i = 0; i < m_listSmaProperty.Count; i++)
        //                        {
        //                            if (m_listSmaProperty.ElementAt(i).id != Convert.ToInt32(_XElement.Value))
        //                            {
        //                                continue;
        //                            }

        //                            xmlText = "{" + m_listSmaProperty.ElementAt(i).property_desc + "}" + " ";
        //                            this.m_LinkedToPropertyID = m_listSmaProperty.ElementAt(i).id;
        //                            break;
        //                        }

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Red);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        break;
        //                    case "DimensionCovers":
        //                        if (this.p_isLife)
        //                        {
        //                            for (var i = 0; i < m_LifeListCover.Count; i++)
        //                            {
        //                                if (m_LifeListCover.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_LifeListCover.ElementAt(i).cover_desc + "}" + " ";
        //                                break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            for (var i = 0; i < m_listCover.Count; i++)
        //                            {
        //                                if (m_listCover.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_listCover.ElementAt(i).cover_desc + "}" + " ";
        //                                break;
        //                            }
        //                        }
        //                        _parsedText.Append(xmlText);

        //                        break;

        //                    //AN 2026-03-26
        //                    case "DimensionClauses":
        //                        if (this.p_isLife)
        //                        {
        //                            for (var i = 0; i < m_lifeListClause.Count; i++)
        //                            {
        //                                if (m_lifeListClause.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_lifeListClause.ElementAt(i).clause_code + "}" + " ";
        //                                break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            for (var i = 0; i < m_listClause.Count; i++)
        //                            {
        //                                if (m_listClause.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_listClause.ElementAt(i).clause_code + "}" + " ";
        //                                break;
        //                            }
        //                        }
        //                        _parsedText.Append(xmlText);

        //                        break;
        //                    //AN 2026-03-26

        //                    case "DimensionDiseases":
        //                        if (m_listDisease.Count == 0)
        //                        {
        //                            m_listDisease.AddRange(dbConn.Query<UW_DISEASE>("SELECT * FROM UW_DISEASE"));//, (r11) => this.ReadXML(), null);
        //                        }
        //                        //else
        //                        //{
        //                        for (var i = 0; i < m_listDisease.Count; i++)
        //                        {
        //                            if (m_listDisease.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listDisease.ElementAt(i).disease_desc + "}" + " ";
        //                            break;
        //                        }
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //this.RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        //}
        //                        break;
        //                    case "DimensionAssessmentTypes":
        //                        if (m_listAssessmentType.Count == 0)
        //                        {
        //                            m_listAssessmentType.AddRange(dbConn.Query<CL_ASSESSMENT_TYPE>("SELECT * FROM CL_ASSESSMENT_TYPE"));//, (r11) => this.ReadXML(), null);
        //                                                                                                                                //oContextCL.Load(oContextCL.GetCL_AdmissionsTypesQuery(
        //                                                                                                                                //    Pixel.iris5.shared.Options.Instance.UsesSharedSettings,
        //                                                                                                                                //    Pixel.iris5.shared.Options.Instance.UsesSharedTechnicalSettings,
        //                                                                                                                                //    Pixel.iris5.shared.Options.Instance.ActiveCompanyID), (Resaa) =>
        //                                                                                                                                //    this.ReadXML(),
        //                                                                                                                                //    null);
        //                        }
        //                        //else
        //                        //{
        //                        for (var i = 0; i < m_listAssessmentType.Count; i++)
        //                        {
        //                            if (m_listAssessmentType.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listAssessmentType.ElementAt(i).assessment_type_desc + "}" + " ";
        //                            break;
        //                        }
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //this.RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        //}

        //                        break;
        //                    case "DimensionTechnicalQuestions":
        //                        if (m_listQuestion.Count == 0)
        //                        {
        //                            m_listQuestion.AddRange(dbConn.Query<UW_QUESTION>("SELECT * FROM UW_QUESTION"));
        //                            //oContextUW.Load(oContextUW.GetUW_QuestionsQuery(
        //                            //        Pixel.iris5.shared.Options.Instance.UsesSharedSettings,
        //                            //        Pixel.iris5.shared.Options.Instance.UsesSharedTechnicalSettings,
        //                            //        Pixel.iris5.shared.Options.Instance.ActiveCompanyID), (result11) => this.ReadXML(), null);
        //                        }
        //                        //else
        //                        //{
        //                        for (var i = 0; i < m_listQuestion.Count; i++)
        //                        {
        //                            if (m_listQuestion.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listQuestion.ElementAt(i).question_desc + "}" + " ";
        //                            break;
        //                        }
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Red);
        //                        //this.RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        //}
        //                        break;
        //                    case "DimensionConditionalFlags":
        //                        if (m_listFlag.Count == 0)
        //                        {
        //                            m_listFlag.AddRange(dbConn.Query<UW_FLAG>("SELECT * FROM UW_FLAG"));
        //                            //oContextUW.Load(oContextUW.GetUW_FlagsQuery(Pixel.iris5.shared.Options.Instance.ActiveCompanyID), (r122) => this.ReadXML(), null);
        //                        }
        //                        //else
        //                        //{
        //                        for (var i = 0; i < m_listFlag.Count; i++)
        //                        {
        //                            if (m_listFlag.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listFlag.ElementAt(i).flag_desc + "}" + " ";
        //                            break;
        //                        }
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        //}
        //                        break;
        //                    case "DimensionPolicyComputation":
        //                        //TONY 26-NOV-2021
        //                        //if (m_listComputFraction.Count == 0)
        //                        //{
        //                        //    m_listComputationItem.AddRange(dbConn.Query<UW_COMPUTATION_ITEM>("SELECT * FROM UW_COMPUTATION_ITEM WHERE ID = " + m_ComputationId));
        //                        //}

        //                        for (var i = 0; i < m_listComputFraction.Count; i++)
        //                        {
        //                            if (m_listComputFraction.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listComputFraction.ElementAt(i).comput_fraction_desc + "}" + " ";
        //                            break;
        //                        }
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        //}
        //                        break;
        //                    case "DimensionLinkedTo":
        //                    case "DimensionCompetitorPolicy":
        //                    case "DimensionPolicy":
        //                    case "DimensionSystem":
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert("{" + _XElement.Value + "}" + " ");
        //                        _parsedText.Append("{" + _XElement.Value + "}" + " ");
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;
        //                    case "DimensionValue":
        //                        if (this.m_LinkedToPropertyID != -1)
        //                        {
        //                            if (_XElement.Value.ToUpper() != "TRUE" && _XElement.Value.ToUpper() != "FALSE")
        //                            {
        //                                #region Sma

        //                                for (var i = 0; i < m_listSmaProperty.Count; i++)
        //                                {
        //                                    if (m_listSmaProperty.ElementAt(i).id != this.m_LinkedToPropertyID) continue;

        //                                    switch (m_listSmaProperty.ElementAt(i).property_type_id)
        //                                    {
        //                                        case (int)Enums.SMAPropertyType.Age:
        //                                        case (int)Enums.SMAPropertyType.Bit:
        //                                        case (int)Enums.SMAPropertyType.Decimal:
        //                                        case (int)Enums.SMAPropertyType.Integer:
        //                                        case (int)Enums.SMAPropertyType.String:

        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                            //this.RichTextBox1.Insert("[" + _XElement.Value + "]" + " ");
        //                                            _parsedText.Append("[" + _XElement.Value + "]" + " ");
        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                            break;

        //                                        case (int)Enums.SMAPropertyType.ReadFromTable:
        //                                            {
        //                                                if (m_listSmaPropertyTableValue.Count(w => w.property_table_id == Convert.ToInt32(m_listSmaProperty.ElementAt(i).property_table_id)) == 0)
        //                                                {
        //                                                    m_listSmaPropertyTableValue.AddRange(dbConn.Query<OB_PROPERTY_TABLE_VALUE>("SELECT * FROM OB_PROPERTY_TABLE_VALUE WHERE PROPERTY_TABLE_ID = " + m_listSmaProperty.ElementAt(i).property_table_id));
        //                                                    //oContextSma.Load(oContextSma.GetOB_PropertiesTablesValuesByPropertyTableIDQuery(
        //                                                    //    Convert.ToInt32(m_listSmaProperty.ElementAt(i).property_table_id), true), (r1) =>
        //                                                    //        this.ReadXML(),
        //                                                    //    null);
        //                                                }
        //                                                else
        //                                                {
        //                                                    var lstPropertiesTablesValues =
        //                                                        this.m_listSmaPropertyTableValue.Where(w => w.property_table_id == m_listSmaProperty.ElementAt(i).property_table_id).ToList();

        //                                                    for (var ii = 0; ii < lstPropertiesTablesValues.Count(); ii++)
        //                                                    {
        //                                                        if (Convert.ToInt32(_XElement.Value) != lstPropertiesTablesValues.ElementAt(ii).id) continue;
        //                                                        //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                                        //RichTextBox1.Insert("[" + lstPropertiesTablesValues.ElementAt(ii).TheValue + "]" + " ");
        //                                                        _parsedText.Append("[" + lstPropertiesTablesValues.ElementAt(ii).the_value + "]" + " ");
        //                                                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                                                        this.m_LinkedToPropertyID = -1;
        //                                                        break;
        //                                                    }
        //                                                }
        //                                            }
        //                                            break;
        //                                        case (int)Enums.SMAPropertyType.ReadFromContact:
        //                                            switch (m_listSmaProperty.ElementAt(i).contact_field_property_type_id)
        //                                            {
        //                                                case (int)Enums.SMAPropertyType.Age:
        //                                                case (int)Enums.SMAPropertyType.Bit:
        //                                                case (int)Enums.SMAPropertyType.Decimal:
        //                                                case (int)Enums.SMAPropertyType.Integer:
        //                                                case (int)Enums.SMAPropertyType.String:
        //                                                    //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                                    //RichTextBox1.Insert("[" + _XElement.Value + "]" + " ");
        //                                                    _parsedText.Append("[" + _XElement.Value + "]" + " ");
        //                                                    //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                                    break;
        //                                                case (int)Enums.SMAPropertyType.Date:
        //                                                    //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                                    //RichTextBox1.Insert("[" + Convert.ToDateTime(_XElement.Value) + "]" + " ");
        //                                                    _parsedText.Append("[" + Convert.ToDateTime(_XElement.Value) + "]" + " ");
        //                                                    //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                                    break;
        //                                                case (int)Enums.SMAPropertyType.ReadFromSystemTable:
        //                                                    var sysTable = this.m_listSystemTable
        //                                                        .FirstOrDefault(w => w.table_type == m_listSmaProperty.ElementAt(i).system_table &&
        //                                                                             w.id.ToString() == _XElement.Value);
        //                                                    if (sysTable != null)
        //                                                    {
        //                                                        //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                                        //RichTextBox1.Insert("[" + sysTable.Desc + "]" + " ");
        //                                                        _parsedText.Append("[" + sysTable.the_desc + "]" + " ");
        //                                                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                                    }

        //                                                    break;
        //                                            }
        //                                            break;

        //                                        case (int)Enums.SMAPropertyType.Date:
        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                            //RichTextBox1.Insert("[" + Convert.ToDateTime(_XElement.Value) + "]" + " ");
        //                                            _parsedText.Append("[" + Convert.ToDateTime(_XElement.Value) + "]" + " ");
        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                            break;

        //                                        case (int)Enums.SMAPropertyType.ReadFromSystemTable:

        //                                            var sysTable2 = this.m_listSystemTable
        //                                                        .FirstOrDefault(w => w.table_type == m_listSmaProperty.ElementAt(i).system_table &&
        //                                                                             w.id.ToString() == _XElement.Value);
        //                                            if (sysTable2 != null)
        //                                            {
        //                                                //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                                //RichTextBox1.Insert("[" + sysTable2.Desc + "]" + " ");
        //                                                _parsedText.Append("[" + sysTable2.the_desc + "]" + " ");
        //                                                //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                            }

        //                                            break;
        //                                    }
        //                                    this.m_LinkedToPropertyID = -1;
        //                                    break;
        //                                }
        //                                #endregion Sma
        //                            }
        //                            else
        //                            {
        //                                //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                //RichTextBox1.Insert("[" + Convert.ToString(_XElement.Value) + "]" + " ");
        //                                _parsedText.Append("[" + Convert.ToString(_XElement.Value) + "]" + " ");
        //                                //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                this.m_LinkedToPropertyID = -1;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                            //RichTextBox1.Insert("[" + Convert.ToString(_XElement.Value) + "]" + " ");
        //                            _parsedText.Append("[" + Convert.ToString(_XElement.Value) + "]" + " ");
        //                            //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        }

        //                        break;

        //                } // end of switch

        //                break;

        //            case "pre_": //Predefined Function Case
        //                szElementName = szElementName.Remove(0, szElementName.IndexOf("_") + 1);

        //                switch (szElementName)
        //                {
        //                    case "StartFunction":
        //                        if (_FnParameter > 1)
        //                        {
        //                            //Clipboard.SetText(", " + _XElement.Value + "(");
        //                            xmlText = ", " + _XElement.Value + "(";
        //                            _Parameter = 0;
        //                        }
        //                        else
        //                        {
        //                            xmlText = _XElement.Value + "(";
        //                        }
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Blue);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        _FnParameter += 1;

        //                        break;
        //                    case "EndFunction":
        //                        xmlText = ") ";
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Blue);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        if (_FnParameter != 0)
        //                            _FnParameter -= 1;
        //                        else
        //                            _Parameter = 0;

        //                        if (_XElement.Value.ToUpper() == "END_DATEDIFF")
        //                            this.m_LinkedToPropertyID = -1;

        //                        break;
        //                    case "Operator":
        //                    case "LogOperator":
        //                        xmlText = _XElement.Value + " ";
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Blue);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        break;
        //                    case "DimensionSma":
        //                    case "DimensionInterest":
        //                        for (var i = 0; i < m_listSmaProperty.Count; i++)
        //                        {
        //                            if (m_listSmaProperty.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            if (_Parameter != 0 && _FnParameter > 1)
        //                            {
        //                                xmlText = ",{" + m_listSmaProperty.ElementAt(i).property_desc + "}";
        //                            }
        //                            else
        //                            {
        //                                xmlText = "{" + m_listSmaProperty.ElementAt(i).property_desc + "}";
        //                            }
        //                            _Parameter += 1;
        //                            _FnParameter += 1;

        //                            this.m_LinkedToPropertyID = m_listSmaProperty.ElementAt(i).id;
        //                            break;
        //                        }
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Red);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        break;
        //                    case "DimensionCovers":
        //                        if (m_listCover.Count == 0)
        //                        {
        //                            m_listCover.AddRange(dbConn.Query<UW_COVER>(
        //                                " SELECT UW_PROD_CVR.COVER_ID AS ID , COVER_CODE, COVER_DESC " +
        //                                " FROM UW_PROD_CVR " +
        //                                " LEFT JOIN UW_COVER ON UW_PROD_CVR.COVER_ID = UW_COVER.ID" +
        //                                " WHERE PROD_ID IN (" + m_Products + ")"));
        //                            //oContextUW.Load(oContextUW.GetQ_ListCoversByProductIDQuery(this.m_ProductID), (r1) =>
        //                            //{
        //                            //    lstCovers = this.oContextUW.UwCovers.ToList();
        //                            //    this.ReadXML();

        //                            //}, null);
        //                        }
        //                        //else
        //                        //{
        //                        for (var i = 0; i < m_listCover.Count; i++)
        //                        {
        //                            if (m_listCover.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listCover.ElementAt(i).cover_desc + "}";
        //                            break;
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        // }

        //                        break;

        //                        // START AN 2026-03-26
        //                    case "DimensionClauses":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                        {
        //                            if (m_listClause.Count == 0)
        //                            {
        //                                m_listClause.AddRange(dbConn.Query<UW_CLAUSE>("SELECT * FROM UW_CLAUSE"));                                        
        //                            }
                                    
        //                            for (var i = 0; i < m_listClause.Count; i++)
        //                            {
        //                                if (m_listClause.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = ",{" + m_listClause.ElementAt(i).clause_code + "}";
        //                                break;
        //                            } 
        //                        }
        //                        else
        //                        {
        //                            if (m_listClause.Count == 0)
        //                            {
        //                                m_listClause.AddRange(dbConn.Query<UW_CLAUSE>("SELECT * FROM UW_CLAUSE"));
        //                            }
        //                            for (var i = 0; i < m_listClause.Count; i++)
        //                            {
        //                                if (m_listClause.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_listClause.ElementAt(i).clause_code + "}";
        //                                break;
        //                            } 
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        break;

        //                    // END AN 2026-03-26

        //                    case "DimensionDiseases":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                        {
        //                            if (m_listDisease.Count == 0)
        //                            {
        //                                m_listDisease.AddRange(dbConn.Query<UW_DISEASE>("SELECT * FROM UW_DISEASE"));
        //                                //oContextSys.Load(oContextSys.GetSI_DiseasesQuery(), (ResDisea) =>
        //                                //{
        //                                //    this.ReadXML();
        //                                //}, null);
        //                            }
        //                            //else
        //                            //{
        //                            for (var i = 0; i < m_listDisease.Count; i++)
        //                            {
        //                                if (m_listDisease.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = ",{" + m_listDisease.ElementAt(i).disease_desc + "}";
        //                                break;
        //                            }
        //                            //}
        //                        }
        //                        else
        //                        {
        //                            if (m_listDisease.Count == 0)
        //                            {
        //                                m_listDisease.AddRange(dbConn.Query<UW_DISEASE>("SELECT * FROM UW_DISEASE"));//, (r11) => this.ReadXML(), null);
        //                                //oContextSys.Load(oContextSys.GetSI_DiseasesQuery(), (ResDisea) =>
        //                                //{
        //                                //    this.ReadXML();
        //                                //}, null);
        //                            }
        //                            //else
        //                            //{
        //                            for (var i = 0; i < m_listDisease.Count; i++)
        //                            {
        //                                if (m_listDisease.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_listDisease.ElementAt(i).disease_desc + "}";
        //                                break;
        //                            }
        //                            //}
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        break;
        //                    case "DimensionAssessmentTypes":
        //                        if (m_listAssessmentType.Count == 0)
        //                        {
        //                            m_listAssessmentType.AddRange(dbConn.Query<CL_ASSESSMENT_TYPE>("SELECT * FROM CL_ASSESSMENT_TYPE"));
        //                            //oContextCL.Load(oContextCL.GetCL_AssessmentsTypesQuery(Pixel.iris5.shared.Options.Instance.UsesSharedSettings,
        //                            //                                                       Pixel.iris5.shared.Options.Instance.UsesSharedTechnicalSettings,
        //                            //                                                       Pixel.iris5.shared.Options.Instance.ActiveCompanyID), (ResAss) =>
        //                            //                                                       {
        //                            //                                                           this.ReadXML();
        //                            //                                                       }, null);
        //                        }
        //                        //  else
        //                        //                                {
        //                        for (var i = 0; i < m_listAssessmentType.Count; i++)
        //                        {
        //                            if (m_listAssessmentType.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listAssessmentType.ElementAt(i).assessment_type_desc + "}";
        //                            break;
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        // }
        //                        break;
        //                    case "DimensionPolicyComputation":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                        {
        //                            //TONY 26-NOV-2021
        //                            //if (m_listComputFraction.Count == 0)
        //                            //{
        //                            //    m_listComputationItem.AddRange(dbConn.Query<UW_COMPUTATION_ITEM>("SELECT * FROM UW_COMPUTATION_ITEM WHERE COMPUTATION_ID = " + m_ComputationId));
        //                            //    //oContextUW.Load(oContextUW.GetUW_ComputationRulesByComputationIDQuery(this.m_ComputationID), (ResComp) =>
        //                            //    //{
        //                            //    //    this.ReadXML();
        //                            //    //}, null);
        //                            //}

        //                            for (var i = 0; i < m_listComputFraction.Count; i++)
        //                            {
        //                                if (m_listComputFraction.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = ",{" + m_listComputFraction.ElementAt(i).comput_fraction_desc + "}";
        //                                break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            //TONY 26-NOV-2021
        //                            //if (m_listComputFraction.Count == 0)
        //                            //{
        //                            //    m_listComputationItem.AddRange(dbConn.Query<UW_COMPUTATION_ITEM>("SELECT * FROM UW_COMPUTATION_ITEM WHERE COMPUTATION_ID = " + m_ComputationId));
        //                            //    //oContextUW.Load(oContextUW.GetUW_ComputationRulesByComputationIDQuery(this.m_ComputationID), (ResComp) =>
        //                            //    //{
        //                            //    //    this.ReadXML();
        //                            //    //}, null);
        //                            //} 
        //                            for (var i = 0; i < m_listComputFraction.Count; i++)
        //                            {
        //                                if (m_listComputFraction.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_listComputFraction.ElementAt(i).comput_fraction_desc + "}";
        //                                break;
        //                            }
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                        break;

        //                    case "DimensionTechnicalQuestions":
        //                        if (m_listQuestion.Count == 0)
        //                        {
        //                            m_listQuestion.AddRange(dbConn.Query<UW_QUESTION>("SELECT * FROM UW_QUESTION"));
        //                            //oContextUW.Load(oContextUW.GetUW_QuestionsQuery(Pixel.iris5.shared.Options.Instance.UsesSharedSettings,
        //                            //                                                Pixel.iris5.shared.Options.Instance.UsesSharedTechnicalSettings,
        //                            //                                                Pixel.iris5.shared.Options.Instance.ActiveCompanyID), (ResQues) =>
        //                            //                                                {
        //                            //                                                    this.ReadXML();
        //                            //                                                }, null);
        //                        }
        //                        //else
        //                        //{
        //                        for (var i = 0; i < m_listQuestion.Count; i++)
        //                        {
        //                            if (m_listQuestion.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                            xmlText = "{" + m_listQuestion.ElementAt(i).question_desc + "}";
        //                            break;
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Red);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        //}
        //                        break;

        //                    case "DimensionPolicy":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                            xmlText = ",{" + _XElement.Value + "}";
        //                        else
        //                            xmlText = "{" + _XElement.Value + "}";
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionLinkedTo":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                            xmlText = ",{" + _XElement.Value + "}";
        //                        else
        //                            xmlText = "{" + _XElement.Value + "}";
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionCompetitorPolicy":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                            xmlText = ",{" + _XElement.Value + "}";
        //                        else
        //                            xmlText = "{" + _XElement.Value + "}";
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionPolicyAction":
        //                        //for (var i = 0; i < m_ListBRuleAction.Count; i++)
        //                        //{
        //                        //    if (m_ListBRuleAction.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                        //    xmlText = "{" + m_ListBRuleAction.ElementAt(i).type_desc + "}" + " ";
        //                        //    break;
        //                        //}
        //                        ////RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        ////RichTextBox1.Insert(xmlText.ToString());
        //                        //_parsedText.Append(xmlText);
        //                        ////RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionDeductibleType":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                        {
        //                            for (var i = 0; i < m_listDedType.Count; i++)
        //                            {
        //                                if (m_listDedType.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = ",{" + m_listDedType.ElementAt(i).type_desc + "}";
        //                                break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            for (var i = 0; i < m_listDedType.Count; i++)
        //                            {
        //                                if (m_listDedType.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_listDedType.ElementAt(i).type_desc + "}";
        //                                break;
        //                            }
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionLimitationType":
        //                        if (_Parameter != 0 && _FnParameter > 1)
        //                        {
        //                            for (var i = 0; i < m_listLimitationType.Count; i++)
        //                            {
        //                                if (m_listLimitationType.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = ",{" + m_listLimitationType.ElementAt(i).type_desc + "}";
        //                                break;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            for (var i = 0; i < m_listLimitationType.Count; i++)
        //                            {
        //                                if (m_listLimitationType.ElementAt(i).id != Convert.ToInt32(_XElement.Value)) continue;
        //                                xmlText = "{" + m_listLimitationType.ElementAt(i).type_desc + "}";
        //                                break;
        //                            }
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionSystem":
        //                        if (_Parameter != 0 && _FnParameter == 0)
        //                        {
        //                            xmlText = ",{" + _XElement.Value + "}";
        //                        }
        //                        else
        //                        {
        //                            xmlText = "{" + _XElement.Value + "}";
        //                        }
        //                        _Parameter += 1;
        //                        _FnParameter += 1;

        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionPolicyInsured":
        //                    case "DimensionPolicyBroker":
        //                        xmlText = "{" + _XElement.Value + "} ";
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionScopeProfile":
        //                        xmlText = "{" + _XElement.Value + "} ";
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionScopeCollection":
        //                        xmlText = "{" + _XElement.Value + "} ";
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionScopeClaimPO":
        //                        xmlText = "{" + _XElement.Value + "} ";
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Purple);
        //                        //RichTextBox1.Insert(xmlText.ToString());
        //                        _parsedText.Append(xmlText);
        //                        //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        break;

        //                    case "DimensionValue":
        //                        if (this.m_LinkedToPropertyID != -1)
        //                        {
        //                            if (_XElement.Value.ToUpper() != "TRUE" && _XElement.Value.ToUpper() != "FALSE")
        //                            {
        //                                #region Sma
        //                                for (var i = 0; i < m_listSmaProperty.Count; i++)
        //                                {
        //                                    if (m_listSmaProperty.ElementAt(i).id != this.m_LinkedToPropertyID) continue;

        //                                    switch (m_listSmaProperty.ElementAt(i).property_type_id)
        //                                    {
        //                                        case (int)Enums.SMAPropertyType.Age:
        //                                        case (int)Enums.SMAPropertyType.Bit:
        //                                        case (int)Enums.SMAPropertyType.Decimal:
        //                                        case (int)Enums.SMAPropertyType.Integer:
        //                                        case (int)Enums.SMAPropertyType.String:
        //                                            xmlText = "[" + _XElement.Value + "]" + " ";
        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                            //RichTextBox1.Insert(xmlText.ToString());
        //                                            _parsedText.Append(xmlText);
        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                            break;
        //                                        case (int)Enums.SMAPropertyType.ReadFromTable:
        //                                            {
        //                                                var lstPropertiesTablesValues = m_listSmaPropertyTableValue.Where(
        //                                                        w => w.property_table_id == m_listSmaProperty.ElementAt(i).property_table_id).ToList();

        //                                                for (var ii = 0; ii < lstPropertiesTablesValues.Count(); ii++)
        //                                                {
        //                                                    if (Convert.ToInt32(_XElement.Value) != lstPropertiesTablesValues.ElementAt(ii).id) continue;
        //                                                    xmlText = "[" + lstPropertiesTablesValues.ElementAt(ii).the_value + "]" + " ";
        //                                                    //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                                    //this.RichTextBox1.Insert(xmlText.ToString());
        //                                                    _parsedText.Append(xmlText);
        //                                                    //RichTextBox1.ChangeTextForeColor(Colors.Black);

        //                                                    this.m_LinkedToPropertyID = -1;
        //                                                    break;
        //                                                }
        //                                            }
        //                                            break;
        //                                        case (int)Enums.SMAPropertyType.ReadFromContact:
        //                                            break;
        //                                        case (int)Enums.SMAPropertyType.Date:
        //                                            xmlText = "[" + Convert.ToDateTime(_XElement.Value) + "]" + " ";
        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                            //RichTextBox1.Insert(xmlText.ToString());
        //                                            _parsedText.Append(xmlText);
        //                                            //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                            break;
        //                                        case (int)Enums.SMAPropertyType.ReadFromSystemTable:
        //                                            switch (m_listSmaProperty.ElementAt(i).system_table)
        //                                            {
        //                                                case "CORE_GEO_ZONE":

        //                                                    break;
        //                                                case "CORE_GEO_ARE":

        //                                                    break;
        //                                                case "CORE_GEO_CITY":

        //                                                    break;
        //                                            }
        //                                            break;
        //                                    }
        //                                    this.m_LinkedToPropertyID = -1;
        //                                    break;
        //                                }
        //                                #endregion Sma
        //                            }
        //                            else
        //                            {
        //                                xmlText = "[" + Convert.ToString(_XElement.Value) + "]" + " ";
        //                                //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                                //RichTextBox1.Insert(xmlText.ToString());
        //                                _parsedText.Append(xmlText);
        //                                //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                                this.m_LinkedToPropertyID = -1;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            xmlText = "[" + Convert.ToString(_XElement.Value) + "]" + " ";
        //                            //RichTextBox1.ChangeTextForeColor(Colors.Green);
        //                            //RichTextBox1.Insert(xmlText.ToString());
        //                            _parsedText.Append(xmlText);
        //                            //RichTextBox1.ChangeTextForeColor(Colors.Black);
        //                        }

        //                        break;

        //                } //end switch (szElementName)

        //                break;
        //        } // end of switch

        //    }

        //} // end of function

        #endregion XML

    }
}
