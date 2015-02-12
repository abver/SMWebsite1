using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.EnterpriseManagement;
using Microsoft.EnterpriseManagement.Common;
using Microsoft.EnterpriseManagement.Configuration;
using System.Data;

/// <summary>
/// Summary description for IncidentHelper
/// </summary>
public class IncidentHelper
{
	public IncidentHelper()
	{
    }
		public string CreateIncident(string title, string description)
        {
            string workitemId = String.Empty;
            EnterpriseManagementConnectionSettings emcs = new EnterpriseManagementConnectionSettings("smxsetupav");

            char[] password = { 'C','a','e','l','u','m','#','0','1'};
            emcs.UserName = "asttest";
            
            emcs.Password = new System.Security.SecureString();
            foreach (char c in password)
                emcs.Password.AppendChar(c); 

            emcs.Domain ="smx.net";

                //First, create a connection to the management group.
            EnterpriseManagementGroup emg = new EnterpriseManagementGroup(emcs);

            //First, create a connection to the management group.
            ManagementPackCriteria mpCr = new ManagementPackCriteria("Name = 'System.WorkItem.Incident.Library'");
            IList<ManagementPack> mps = emg.ManagementPacks.GetManagementPacks(mpCr);

            ManagementPackClassCriteria classCr = new ManagementPackClassCriteria("Name = 'System.WorkItem.Incident'");
            IList<ManagementPackClass> classes = emg.EntityTypes.GetClasses(classCr);

            ManagementPackClass classIncident = classes[0];
            ManagementPack mpIncidentLibrary = mps[0];

            ManagementPack mpWorkItemLibrary = emg.GetManagementPack("System.WorkItem.Library", mpIncidentLibrary.KeyToken, mpIncidentLibrary.Version); 

            //Also get the Medium urgency and impact enums 
            //since they are required values to create a new incident
            //More information on working with enums here: 
            //http://blogs.technet.com/b/servicemanager/archive/2010/05/25/programmatically-working-with-enumerations.aspx 
            ManagementPackEnumeration mpenumIncidentUrgencyHigh = emg.EntityTypes.GetEnumeration("System.WorkItem.TroubleTicket.UrgencyEnum.High", mpWorkItemLibrary);
            ManagementPackEnumeration mpenumIncidentImpactyHigh = emg.EntityTypes.GetEnumeration("System.WorkItem.TroubleTicket.ImpactEnum.High", mpWorkItemLibrary); 

            //Now create a CreatableEnterpriseManagementObject so we can create a 
            //new incident object and populate its properties and Commit() it.
            CreatableEnterpriseManagementObject cemoIncident = 
                new CreatableEnterpriseManagementObject(emg,classIncident);
            //Just doing this for demo purposes.  Obviously a GUID is not a good display name!
            String strTestID = Guid.NewGuid().ToString();  
            //Set some property values
            cemoIncident[classIncident, "DisplayName"].Value = title;
            cemoIncident[classIncident, "Title"].Value = title;
            cemoIncident[classIncident, "Urgency"].Value = mpenumIncidentUrgencyHigh;
            cemoIncident[classIncident, "Impact"].Value = mpenumIncidentImpactyHigh;
            //And submit...
            cemoIncident.Commit();
            Console.WriteLine("Incident object created named: " + title);

            workitemId = cemoIncident[classIncident, "Id"].Value.ToString();
            
            //======================================================
            //Example #2 - Creating a Relationship between to Objects - Incidnet and User via the Affected User relationship
            //======================================================
            //Get the incident
            Console.WriteLine("Creating an relationship between the incidena and a user....");
            String strIncidentByTitleCriteria = 
                String.Format(@"<Criteria xmlns=""http://Microsoft.EnterpriseManagement.Core.Criteria/"">" + 
                      "<Expression>" +
                        "<SimpleExpression>" +
                          "<ValueExpressionLeft>" +
                            "<Property>$Target/Property[Type='System.WorkItem.Incident']/Title$</Property>" + 
                          "</ValueExpressionLeft>" + 
                          "<Operator>Equal</Operator>" +
                          "<ValueExpressionRight>" +
                            "<Value>" + title + "</Value>" +
                          "</ValueExpressionRight>" +
                        "</SimpleExpression>" +
                      "</Expression>" +
                    "</Criteria>");


            //Get the incident using the criteria from above...
            EnterpriseManagementObjectCriteria emocIncidnetByTitle = 
                new EnterpriseManagementObjectCriteria((String)strIncidentByTitleCriteria,classIncident,mpIncidentLibrary,emg);
            IObjectReader<EnterpriseManagementObject> readerEmoIncidentAffectedUsers = 
                emg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(emocIncidnetByTitle,ObjectQueryOptions.Default);
            EnterpriseManagementObject emoIncident = readerEmoIncidentAffectedUsers.ElementAt(0);

            //Get a user..
            //System.Domain.User class
            ManagementPackClass classUser = 
                emg.EntityTypes.GetClass(new Guid("ECA3C52A-F273-5CDC-F165-3EB95A2B26CF")); 
            IObjectReader<EnterpriseManagementObject> readerEmoDomainUsers = 
                emg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(classUser,ObjectQueryOptions.Default);
            //Just getting whatever the first user is that comes back for demo purposes
            EnterpriseManagementObject emoDomainUser = readerEmoDomainUsers.ElementAt(0); 
            
            //Create a Relationship between the service and the user
            //System.WorkItemAfectedUser relationship type
            ManagementPackRelationship relIncidentAffectedUser = 
                emg.EntityTypes.GetRelationshipClass(new Guid("DFF9BE66-38B0-B6D6-6144-A412A3EBD4CE"));
            CreatableEnterpriseManagementRelationshipObject cemroIncidentAffectedUser = 
                new CreatableEnterpriseManagementRelationshipObject(emg, relIncidentAffectedUser);
            //Set the source and target...
            cemroIncidentAffectedUser.SetSource(emoIncident);
            cemroIncidentAffectedUser.SetTarget(emoDomainUser);
            //And submit...
            cemroIncidentAffectedUser.Commit();
            Console.WriteLine("Relationship created.");

            return workitemId;
        }

        public DataTable GetListOfIncidents()
    {

        DataTable myDataTable = new DataTable();

        DataColumn myDataColumn;

        myDataColumn = new DataColumn();
        myDataColumn.DataType = Type.GetType("System.String");
        myDataColumn.ColumnName = "Id";
        myDataTable.Columns.Add(myDataColumn);

        myDataColumn = new DataColumn();
        myDataColumn.DataType = Type.GetType("System.String");
        myDataColumn.ColumnName = "Title";
        myDataTable.Columns.Add(myDataColumn);

        myDataColumn = new DataColumn();
        myDataColumn.DataType = Type.GetType("System.String");
        myDataColumn.ColumnName = "Description";
        myDataTable.Columns.Add(myDataColumn);

        myDataColumn = new DataColumn();
        myDataColumn.DataType = Type.GetType("System.String");
        myDataColumn.ColumnName = "Status";
        myDataTable.Columns.Add(myDataColumn);
      
        string strCriteria = @"<Criteria xmlns='http://Microsoft.EnterpriseManagement.Core.Criteria/'> 
        <Reference Id='System.WorkItem.Library' PublicKeyToken='{0}' Version='{1}' Alias='WorkItem' /> 
        <Expression> 
            <SimpleExpression> 
               <ValueExpressionLeft>
                   <Property>$Context/Path[Relationship='WorkItem!System.WorkItemAssignedToUser' TypeConstraint='System!System.Domain.User']/Property[Type='System!System.Domain.User']/UserName$</Property>
              </ValueExpressionLeft>
              <Operator>Equal</Operator>
              <ValueExpressionRight>
                <Value>[me]</Value>
              </ValueExpressionRight>
            </SimpleExpression> 
        </Expression> 
        </Criteria>";

        EnterpriseManagementConnectionSettings emcs = new EnterpriseManagementConnectionSettings("smxsetupav");

        char[] password = { 'C', 'a', 'e', 'l', 'u', 'm', '#', '0', '1' };
        emcs.UserName = "asttest";

        emcs.Password = new System.Security.SecureString();
        foreach (char c in password)
            emcs.Password.AppendChar(c);

        emcs.Domain = "smx.net";

        //First, create a connection to the management group.
        EnterpriseManagementGroup mg = new EnterpriseManagementGroup(emcs);
        ManagementPackCriteria mpCr = new ManagementPackCriteria("Name = 'System.WorkItem.Incident.Library'"); 
        IList<ManagementPack> mps = mg.ManagementPacks.GetManagementPacks(mpCr); 

        ManagementPackClassCriteria classCr = new ManagementPackClassCriteria("Name = 'System.WorkItem.Incident'"); 
        IList<ManagementPackClass> classes = mg.EntityTypes.GetClasses(classCr); 

 
        if (mps != null && mps.Count > 0 && classes != null && classes.Count > 0) 
        { 
            ManagementPack mp = mps[0]; 
            ManagementPackClass cl = classes[0]; 

            // For all system management packs version and KeyToken are same. 
            EnterpriseManagementObjectCriteria cr = new EnterpriseManagementObjectCriteria(string.Format(strCriteria, mp.KeyToken, mp.Version),cl, mp, mg); 
            
            IObjectReader<EnterpriseManagementObject> reader = mg.EntityObjects.GetObjectReader<EnterpriseManagementObject>(cr, ObjectQueryOptions.Default); 

            if (reader != null && reader.Count > 0) 
            { 
                foreach (EnterpriseManagementObject obj in reader) 
                { 
                    // do something 
                    DataRow row;

                    row = myDataTable.NewRow();

                    row["Id"] = obj[cl, "Id"].Value;
                    row["Title"] = obj[cl, "Title"].Value;
                    row["Description"] = obj[cl, "DisplayName"].Value;
                    row["Status"] = obj[cl, "Status"].Value;
                    

                    myDataTable.Rows.Add(row);

                } 
            } 
        }

        return myDataTable;
    }

}