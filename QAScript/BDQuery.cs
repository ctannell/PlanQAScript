using System;
using System.Windows;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Data.SqlClient;

namespace QAScript
{
    class DBQuery
    {
        public static void RunDBQuery(Patient patient, Course course, PlanSetup plan)
        {
            // Every new class needs to do these same first steps which is to load in the results message and the datatable from their propertes and write them back at the end of the code.
            string msg = SomeProperties.MsgString;
            DataTable table = SomeProperties.MsgDataTable;
            DataRow row;

            string Output = "";
            List<String> PatientInfoList = new List<String>();

            // Build connection string
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = SomeProperties.DBDataSource,
                UserID = SomeProperties.DBUserId,
                Password = SomeProperties.DBPassword
            };

            // Create new connection to the DB and test it
            SqlConnection connection = new SqlConnection(builder.ConnectionString);
            try
            {
                // Try to connect to SQL server
                using (connection)
                {
                    connection.Open();
                }
            }
            catch (SqlException e)
            {
                MessageBox.Show("Error: For some reason the script was not able to connect to the SQL server. The Error message was:\n" + e.ToString());
            }

            // Open connection again as the "using" command in the connection test above closed it. By including "USE VARIAN", future connections remember what DB is used.
            connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            new SqlCommand("USE VARIAN;", connection).ExecuteReader().Close();


            ////////////////////////////////
            // Begin to do actual tests here
            ////////////////////////////////
            SqlDataReader dataReader;
            SqlCommand SQLcmd;
            string sql;
            bool emptydatareader;
            // Get plan normalization value and check that it is within 10 percent of 100 if it is an arc delivery (values outside this range can indicate poor delivery accuracy). 
            row = table.NewRow();
            row["Item"] = "The plan normalization value is within 10 percent of 100 if it is an arc delivery (values outside this range can indicate poor delivery accuracy)";
            var listofbeams = plan.Beams;
            bool foundarc = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Equals("SRS ARC"))
                {
                    foundarc = true;
                }
            }

            if (foundarc == true)
            {

                sql = "SELECT PlanSetup.PlanNormFactor" +
                " FROM PlanSetup, Course, Patient" +
                " WHERE PlanSetup.CourseSer = Course.CourseSer AND Course.PatientSer = Patient.PatientSer AND" +
                " Patient.PatientId='" + SomeProperties.PatientId + "' AND Course.CourseId='" + SomeProperties.CourseId + "' AND PlanSetup.PlanSetupId='" + SomeProperties.PlanId + "'";

                SQLcmd = new SqlCommand(sql, connection);
                dataReader = SQLcmd.ExecuteReader();

                if (dataReader.HasRows)
                {
                    while (dataReader.Read())
                    {
                        Output = dataReader.GetValue(0).ToString();
                    }
                    dataReader.Close();

                    double normval = double.Parse(Output);
                    normval = 100 * (1 / normval);

                    if (normval < 90 || normval > 110)
                    {
                        msg += "\n\nPlan normalization value deviates from 100% by more than 10%. The actual value is " + Math.Round(normval, 1) + "%. Values outside this range can indicate poor delivery accuracy, possibly caused by changing the dose per fraction after an optimization, or normalizing the plan far away from the target objective in the optimizer.";
                        row["Result"] = "Fail";
                    }
                    else
                    {
                        row["Result"] = "Pass";
                    }
                }
                else
                {
                    // The data reader is empty (no normalization was returned) and the details window will show the test result as "unknown"
                }


            }
            else
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            //The following query gets multiple columns of data from each field in the plan. The data includes:
            // Get couch and imager positions for each field and check that they are the expected defaults.
            // Also check that the setup note begins with the course and plan name.
            // Also get the Tolerance table
            sql = "SELECT ExternalFieldCommon.CouchVrt, ExternalFieldCommon.CouchLng, ExternalFieldCommon.CouchLat, ExternalFieldCommon.IDUPosVrt, ExternalFieldCommon.IDUPosLng, ExternalFieldCommon.IDUPosLat, Radiation.SetupNote, Tolerance.ToleranceId" +
                " FROM PlanSetup, Course, Patient, ExternalFieldCommon, Radiation, Tolerance" +
                " WHERE PlanSetup.CourseSer = Course.CourseSer AND Course.PatientSer = Patient.PatientSer AND PlanSetup.PlanSetupSer = Radiation.PlanSetupSer AND Radiation.RadiationSer = ExternalFieldCommon.RadiationSer AND ExternalFieldCommon.ToleranceSer = Tolerance.ToleranceSer AND" +
                " Patient.PatientId='" + SomeProperties.PatientId + "' AND Course.CourseId='" + SomeProperties.CourseId + "' AND PlanSetup.PlanSetupId='" + SomeProperties.PlanId + "'";
            SQLcmd = new SqlCommand(sql, connection);
            dataReader = SQLcmd.ExecuteReader();
            bool foundwrongcouchvalue = false;
            bool foundwrongimagervalue = false;
            bool foundwrongsetupnote = false;
            bool foundwrongtolerance = false;

            emptydatareader = false;
            if (!dataReader.HasRows)
            {
                emptydatareader = true;
            }

            while (dataReader.Read())
            {
                string CouchVrt = dataReader.GetValue(0).ToString();
                string CouchLng = dataReader.GetValue(1).ToString();
                string CouchLat = dataReader.GetValue(2).ToString();
                string ImagerVrt = dataReader.GetValue(3).ToString();
                string ImagerLng = dataReader.GetValue(4).ToString();
                string ImagerLat = dataReader.GetValue(5).ToString();
                string SetupNote = dataReader.GetValue(6).ToString();
                string ToleranceTable = dataReader.GetValue(7).ToString();

                // Check couch and imager vert positions for each field and check that they are the expected defaults. Note that the vert positions have the sign reversed in the DB vs how they appear in Eclipse.
                if (CouchVrt != Convert.ToString(-10) || CouchLng != Convert.ToString(110) || CouchLat != Convert.ToString(0))
                {
                    foundwrongcouchvalue = true;
                }
                if (ImagerVrt != Convert.ToString(-50))
                {
                    foundwrongimagervalue = true;
                }
                // Check that the setup note begins with the course id, followed by a space, followed by the plan name.
                if (SetupNote.StartsWith(course.Id.ToString() + " " + plan.Id.ToString()) == false)
                {
                    foundwrongsetupnote = true;
                }
                // Check that the tolerance table name is appropriate depending on the machine selected
                string Machine = plan.Beams.First().TreatmentUnit.Id; //v15
                if (Machine.StartsWith(ToleranceTable) == false)
                {
                    foundwrongtolerance = true;
                }
            }
            dataReader.Close();

            // Check couch and imager values are set
            row = table.NewRow();
            row["Item"] = "Couch vrt, lng, lat and imager vrt values are set to 10, 110, 0 and 50 respectivly";
            if (foundwrongcouchvalue == true)
            {
                msg += "\n\nAt least one beam has a couch value that is not the expected 10, 110, 0 cm for vrt, lng and lat respectivly.";
                row["Result"] = "Fail";
            }
            if (foundwrongimagervalue == true)
            {
                msg += "\n\nAt least one beam has a imager vrt value that is not the expected 50 cm.";
                row["Result"] = "Fail";
            }
            if (foundwrongimagervalue == false && foundwrongcouchvalue == false && emptydatareader == false)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Plan setup note for each field begins with the course name followed by the plan name.
            row = table.NewRow();
            row["Item"] = "Plan setup note for each field begins with the course name followed by the plan name";
            if (foundwrongsetupnote == true)
            {
                msg += "\n\nAt least one beam has a setup note that is not the expected course name, followed by a space, followed by the plan name.";
                row["Result"] = "Fail";
            }
            else if (emptydatareader == false)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Tolerance table matches the selected machine
            row = table.NewRow();
            row["Item"] = "Tolerance table matches the selected machine";
            if (foundwrongtolerance == true)
            {
                msg += "\n\nAt least one beam has a tolerance table value that does not match the machine used.";
                row["Result"] = "Fail";
            }
            else if (emptydatareader == false)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check ref point stuff
            sql = "SELECT RadiationRefPoint.FieldDose, RefPoint.RefPointId, RefPoint.TotalDoseLimit, RefPoint.DailyDoseLimit, RefPoint.SessionDoseLimit, RefPoint.RefPointType" +
                " FROM PlanSetup, Course, Patient, RTPlan, RadiationRefPoint, RefPoint " +
                " WHERE PlanSetup.CourseSer = Course.CourseSer AND Course.PatientSer = Patient.PatientSer AND PlanSetup.PlanSetupSer = RTPlan.PlanSetupSer AND RTPlan.RTPlanSer = RadiationRefPoint.RTPlanSer AND" +
                " RadiationRefPoint.RefPointSer = RefPoint.RefPointSer AND" +
                " Patient.PatientId='" + SomeProperties.PatientId + "' AND Course.CourseId='" + SomeProperties.CourseId + "' AND PlanSetup.PlanSetupId='" + SomeProperties.PlanId + "' ORDER BY RefPoint.RefPointId ASC";
            SQLcmd = new SqlCommand(sql, connection);
            dataReader = SQLcmd.ExecuteReader();

            DataTable dt = new DataTable();
            dt.Columns.Add("FieldDose");
            dt.Columns.Add("RefPointID");
            dt.Columns.Add("TotalDoseLimit");
            dt.Columns.Add("DailyDoseLimit");
            dt.Columns.Add("SessionDoseLimit");

            emptydatareader = false;
            if (!dataReader.HasRows)
            {
                emptydatareader = true;
            }

            while (dataReader.Read())
            {
                string FieldDose = dataReader.GetValue(0).ToString();
                string RefPointID = dataReader.GetValue(1).ToString();
                string TotalDoseLimit = dataReader.GetValue(2).ToString();
                string DailyDoseLimit = dataReader.GetValue(3).ToString();
                string SessionDoseLimit = dataReader.GetValue(4).ToString();
                string RefPointType = dataReader.GetValue(5).ToString();
                dt.Rows.Add(FieldDose, RefPointID, TotalDoseLimit, DailyDoseLimit, SessionDoseLimit);
            }
            dataReader.Close();

            // Check dose limits (total, day and session) for the primary reference point (name found from the API)
            row = table.NewRow();
            row["Item"] = "Dose limits (total, day and session) for the primary reference point match the dose and fractionation assuming one fraction max per day";
            string PrimRefPointName = plan.PrimaryReferencePoint.Id;
            foreach (DataRow dtrow in dt.Rows)
            {
                if (PrimRefPointName.Equals(dtrow[1]))
                {
                    if (dtrow[2].Equals("") || dtrow[3].Equals("") || dtrow[4].Equals(""))
                    {
                        msg += "\n\nThe primary reference point \"" + PrimRefPointName + "\" is missing one or more of its dose limits";
                        row["Result"] = "Fail";
                    }
                    else if (Convert.ToDouble(plan.TotalDose.ValueAsString) != Convert.ToDouble(dtrow[2])
                        || (plan.DosePerFraction.ValueAsString) != Convert.ToDecimal(dtrow[3]).ToString("0.000")
                        || (plan.DosePerFraction.ValueAsString) != Convert.ToDecimal(dtrow[4]).ToString("0.000"))
                    {
                        msg += "\n\nThe primary reference point \"" + PrimRefPointName + "\" limits for at least one of Total, Daily or Session max dose does not seem correct or is missing (assuming only one fraction per day). " +
                            "They are currently set to " + Convert.ToDouble(dtrow[2]).ToString("0.000") + ", " + Convert.ToDouble(dtrow[3]).ToString("0.000") + " and " + Convert.ToDouble(dtrow[4]).ToString("0.000") + " respectively.";
                        row["Result"] = "Fail";
                    }
                    else if (emptydatareader == false)
                    {
                        row["Result"] = "Pass";
                    }
                    break;
                }

            }
            table.Rows.Add(row);

            // Now to check the radcalc point (if it's found) dose limits. This requres a bit more math because we can't get the expected values from the API.
            // It requires a DB query to get what is called the "field dose" to the point for each field. Sum up the field dose and you've got your actual delivered session dose. Multiply by # of fractions and you've got your total dose limit.
            row = table.NewRow();
            row["Item"] = "Dose limits (total, day and session) for the RADCALC reference point (if it's found) match the dose recorded to that point in Eclipse assuming one fraction max per day";
            decimal sumradcalcfielddose = 0;
            bool foundradcalcpoint = false;
            string radcalcname = "";

            //Loop through all points in each field and add up the field doses to any point that has radc in the name. Hopefully there are not multiple radcalc points...
            foreach (DataRow dtrow in dt.Rows)
            {
                if (dtrow[1].ToString().ToLower().Contains("rad") && dtrow[0].ToString() != "")
                {
                    sumradcalcfielddose = sumradcalcfielddose + Convert.ToDecimal(dtrow[0]);
                    foundradcalcpoint = true;
                    radcalcname = dtrow[1].ToString();
                }
            }
            // Here we'll complain if there was no RadCalc point found and the plan contains arcs.
            if (foundradcalcpoint == false)
            {
                bool foundarcs = false;
                foreach (Beam beams in plan.Beams)
                {
                    if (beams.IsSetupField == false && beams.Technique.Id.ToLower().Contains("arc"))
                    {
                        foundarcs = true;
                    }
                }
                if (foundarcs == true)
                {
                    msg += "\n\nThe plan contains arcs, but no RadCalc point was found. Did you mean to add one?";
                }
            }


            decimal totalradcalcdose = sumradcalcfielddose * Convert.ToDecimal(plan.NumberOfFractions.Value);
            if (foundradcalcpoint == true)
            {
                // Loop through the points again, but this time stop on the first radcalc point in order to get the limits from RT chart to comapare against the calculated values
                foreach (DataRow dtrow in dt.Rows)
                {
                    if (dtrow[1].ToString().ToLower().Contains("rad"))
                    {
                        if (dtrow[2].Equals("") || dtrow[3].Equals("") || dtrow[4].Equals(""))
                        {
                            msg += "\n\nThe Radcalc point \"" + radcalcname + "\" is missing one or more of its dose limits";
                            row["Result"] = "Fail";
                        }
                        else if (totalradcalcdose.ToString("0.000") != Convert.ToDecimal(dtrow[2]).ToString("0.000")
                            || (sumradcalcfielddose.ToString("0.000")) != Convert.ToDecimal(dtrow[3]).ToString("0.000")
                            || (sumradcalcfielddose.ToString("0.000")) != Convert.ToDecimal(dtrow[4]).ToString("0.000"))
                        {
                            msg += "\n\nThe Radcalc point \"" + dtrow[1].ToString() + "\" limits for at least one of Total, Daily or Session max dose does not seem correct or is missing (assuming only one fraction per day and there is only one point with \"radc\" in the name). " +
                                "They are currently set to " + Convert.ToDouble(dtrow[2]).ToString("0.000") + ", " + Convert.ToDouble(dtrow[3]).ToString("0.000") + " and " + Convert.ToDouble(dtrow[4]).ToString("0.000") + " respectively.";
                            row["Result"] = "Fail";
                        }
                        else if (emptydatareader == false)
                        {
                            row["Result"] = "Pass";
                        }
                        break;
                    }
                }
            }
            table.Rows.Add(row);

            // If there is a radcalc point, we'd also want to check that it is in a dose region that is at a minimum 90% of the Rx dose.
            row = table.NewRow();
            row["Item"] = "If a Radcalc point exists, make sure that it's in a high dose region (>90% of the prescription dose)";
            if (foundradcalcpoint == true && Convert.ToDouble(totalradcalcdose) < (0.9 * plan.TotalDose.Dose))
            {
                msg += "\n\nThe Radcalc point " + radcalcname + " is in a dose region where the dose is less than 90 percent of the prescription dose. Consider moving it to a higher dose region to improve accuracy.";
                row["Result"] = "Fail";
            }
            else if (emptydatareader == false)
            {
                row["Result"] = "Pass";
            }

            table.Rows.Add(row);

            // If the plan, ct or structure set ends with "bh" (what we do when we use gating) then the "Use Gated" should be checked in the plan properties. Do ther reverse check as well on the names if the box is checked.
            sql = "SELECT ExternalFieldCommon.MotionCompTechnique" +
                " FROM PlanSetup, Course, Patient, Radiation, ExternalFieldCommon " +
                " WHERE PlanSetup.CourseSer = Course.CourseSer AND Course.PatientSer = Patient.PatientSer AND PlanSetup.PlanSetupSer = Radiation.PlanSetupSer AND Radiation.RadiationSer = ExternalFieldCommon.RadiationSer AND" +
                " Patient.PatientId='" + SomeProperties.PatientId + "' AND Course.CourseId='" + SomeProperties.CourseId + "' AND PlanSetup.PlanSetupId='" + SomeProperties.PlanId + "'";
            SQLcmd = new SqlCommand(sql, connection);
            dataReader = SQLcmd.ExecuteReader();

            string MotionCompTechnique = "";
            emptydatareader = false;
            if (!dataReader.HasRows)
            {
                emptydatareader = true;
            }

            while (dataReader.Read())
            {
                MotionCompTechnique = dataReader.GetValue(0).ToString();
            }
            dataReader.Close();

            // If "Use Gated" is checked, make sure "bh" (not case sensitive) is at the end of the plan name, CT and SS name.
            row = table.NewRow();
            row["Item"] = "If \"Use Gated\" is checked, make sure \"bh\" (not case sensitive) is at the end of the plan ID, CT and structure set ID";
            bool addcomma = false;
            if (MotionCompTechnique == "GATING" && (!plan.Id.ToString().ToLower().EndsWith("bh") || !plan.StructureSet.Id.ToString().ToLower().EndsWith("bh") || !plan.StructureSet.Image.Id.ToString().ToLower().EndsWith("bh")))
            {
                msg += "\n\nThe plan properties has \"Use Gated\" checked, but the following don't end with \"BH\":";
                if ((!plan.Id.ToString().ToLower().EndsWith("bh")))
                {
                    msg += " plan name";
                    addcomma = true;
                }
                if ((!plan.StructureSet.Image.Id.ToString().ToLower().EndsWith("bh")))
                {
                    if (addcomma == true)
                    {
                        msg += ", CT";
                    }
                    else
                    {
                        msg += " CT";
                        addcomma = true;
                    }

                }
                if ((!plan.StructureSet.Id.ToString().ToLower().EndsWith("bh")))
                {
                    if (addcomma == true)
                    {
                        msg += ", structure set";
                    }
                    else
                    {
                        msg += " structure set name";
                        addcomma = true;
                    }
                }
                msg += ".";
                row["Result"] = "Fail";
            }
            else if (emptydatareader == false)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Now do the opposite, if a bh is found on the end of any of the three items, make sure gating is checked.
            row = table.NewRow();
            row["Item"] = "If \"bh\" (not case sensitive) is found at the end of any of the plan ID, CT or structure set ID, then \"Use Gated\" is checked";
            if (plan.Id.ToString().ToLower().EndsWith("bh") || (plan.StructureSet.Image.Id.ToString().ToLower().EndsWith("bh")) || (plan.StructureSet.Id.ToString().ToLower().EndsWith("bh")))
            {
                if (MotionCompTechnique != "GATING")
                {
                    msg += "\n\nOne or more of the plan name, CT name or structure set name end in BH (implying that it's a breath hold technique), but \"Use Gated\" is not checked in the plan properties. Is this correct?";
                    row["Result"] = "Fail";
                }
                else
                {
                    row["Result"] = "Pass";
                }
            }
            else if (emptydatareader == false)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);

            // Check that all setup field DRRs have the "Bones" parameter sets applied (Basically the window and level of the DRR).
            sql = "SELECT Radiation.RadiationId, ExternalField.DRRTemplateFileName, ExternalFieldCommon.SetupFieldFlag" +
                " FROM PlanSetup, Course, Patient, Radiation, ExternalField, ExternalFieldCommon " +
                " WHERE PlanSetup.CourseSer = Course.CourseSer AND Course.PatientSer = Patient.PatientSer AND PlanSetup.PlanSetupSer = Radiation.PlanSetupSer AND Radiation.RadiationSer = ExternalField.RadiationSer AND" +
                " Radiation.RadiationSer = ExternalFieldCommon.RadiationSer AND" +
                " Patient.PatientId='" + SomeProperties.PatientId + "' AND Course.CourseId='" + SomeProperties.CourseId + "' AND PlanSetup.PlanSetupId='" + SomeProperties.PlanId + "'";
            SQLcmd = new SqlCommand(sql, connection);
            dataReader = SQLcmd.ExecuteReader();

            row = table.NewRow();
            row["Item"] = "Check that all setup fields have either the \"Bones\" or \"ANT kv\" DRR setting applied.";

            emptydatareader = false;
            if (!dataReader.HasRows)
            {
                emptydatareader = true;
            }

            bool badDRR = false;
            while (dataReader.Read())
            {
                string RadiationId = dataReader.GetValue(0).ToString();
                string DRRTemplateFileName = dataReader.GetValue(1).ToString();
                string SetupFieldFlag = dataReader.GetValue(2).ToString();

                if (SetupFieldFlag == "1" && RadiationId != "CBCT" && (DRRTemplateFileName != "Bones.dps" && DRRTemplateFileName != "ANT kV.dps"))
                {
                    msg += "\n\nThe setup field \"" + RadiationId + "\" DRR did not have the expected \"Bones\" or \"ANT kV\" DRR setting applied. The DRR setting used was: " + DRRTemplateFileName + ".";
                    badDRR = true;
                }

            }
            dataReader.Close();

            if (badDRR == true)
            {
                row["Result"] = "Fail";
            }
            else if (emptydatareader == false)
            {
                row["Result"] = "Pass";
            }
            table.Rows.Add(row);


            // Check that the POST field of any RSC+AX plans has the "Extended" option selected for gantry 177, 178, 179 or 180. For left sided plans, gantry angles of 181, 182 and 183 should also have it checked.
            row = table.NewRow();
            row["Item"] = "Check that the POST field of any SC+AX plans has the correct \"extended\' setting based on the gantry angle";
            int extendedcheckpassed = 2;
            if (plan.Id.ToLower().Contains("rsc+ax")) // This implies that it's a right "McGill" technique.
            {
                sql = "SELECT Radiation.RadiationId, ExternalField.GantryRtnExt, ExternalField.GantryRtn, ExternalFieldCommon.SetupFieldFlag" +
                " FROM PlanSetup, Course, Patient, Radiation, ExternalField, ExternalFieldCommon " +
                " WHERE PlanSetup.CourseSer = Course.CourseSer AND Course.PatientSer = Patient.PatientSer AND PlanSetup.PlanSetupSer = Radiation.PlanSetupSer AND Radiation.RadiationSer = ExternalField.RadiationSer AND Radiation.RadiationSer = ExternalFieldCommon.RadiationSer AND" +
                " Patient.PatientId='" + SomeProperties.PatientId + "' AND Course.CourseId='" + SomeProperties.CourseId + "' AND PlanSetup.PlanSetupId='" + SomeProperties.PlanId + "'";
                SQLcmd = new SqlCommand(sql, connection);
                dataReader = SQLcmd.ExecuteReader();

                while (dataReader.Read())
                {
                    string RadiationId = dataReader.GetValue(0).ToString(); // The field ID
                    string GantryRtnExt = dataReader.GetValue(1).ToString(); // Two characters, one for the start and one for the stop angle. Each character is either "E" or "N". Ex: EE, EN, NE, or NN.
                    string GantryRtn = dataReader.GetValue(2).ToString(); // The gantry angle
                    string SetupFieldFlag = dataReader.GetValue(3).ToString(); // A flag (0 or 1) that indicates whether or not the field is a setup field.

                    if (RadiationId.ToLower().Contains("post") && SetupFieldFlag == "0")
                    {
                        if (Convert.ToDouble(GantryRtn) <= 180 && Convert.ToDouble(GantryRtn) >= 175)
                        {
                            if (GantryRtnExt != "EN")
                            {
                                msg += "\n\nThis plan is a right SC+AX plan. POST fields with angles less than or equal to 180 should have the \"Extended\" option in the field properties is selected, but it's not.";
                                extendedcheckpassed = 0;
                            }
                        }
                        if (Convert.ToDouble(GantryRtn) > 180 && Convert.ToDouble(GantryRtn) <= 185)
                        {
                            if (GantryRtnExt != "NN")
                            {
                                msg += "\n\nThis plan is a right SC+AX plan. POST fields with angles above 180 should not have the \"Extended\" option in the field properties is selected, but it is.";
                                extendedcheckpassed = 0;
                            }
                        }
                    }
                }
                dataReader.Close();
            }
            // Now for left sided plans
            else if (plan.Id.ToLower().Contains("lsc+ax")) // This implies that it's a left "McGill" technique.
            {
                sql = "SELECT Radiation.RadiationId, ExternalField.GantryRtnExt, ExternalField.GantryRtn, ExternalFieldCommon.SetupFieldFlag" +
                " FROM PlanSetup, Course, Patient, Radiation, ExternalField, ExternalFieldCommon " +
                " WHERE PlanSetup.CourseSer = Course.CourseSer AND Course.PatientSer = Patient.PatientSer AND PlanSetup.PlanSetupSer = Radiation.PlanSetupSer AND Radiation.RadiationSer = ExternalField.RadiationSer AND Radiation.RadiationSer = ExternalFieldCommon.RadiationSer AND" +
                " Patient.PatientId='" + SomeProperties.PatientId + "' AND Course.CourseId='" + SomeProperties.CourseId + "' AND PlanSetup.PlanSetupId='" + SomeProperties.PlanId + "'";
                SQLcmd = new SqlCommand(sql, connection);
                dataReader = SQLcmd.ExecuteReader();

                while (dataReader.Read())
                {
                    string RadiationId = dataReader.GetValue(0).ToString(); // The field ID
                    string GantryRtnExt = dataReader.GetValue(1).ToString(); // Two characters, one for the start and one for the stop angle. Each character is either "E" or "N". Ex: EE, EN, NE, or NN.
                    string GantryRtn = dataReader.GetValue(2).ToString(); // The gantry angle
                    string SetupFieldFlag = dataReader.GetValue(3).ToString(); // A flag (0 or 1) that indicates whether or not the field is a setup field.

                    if (RadiationId.ToLower().Contains("post") && SetupFieldFlag == "0")
                    {
                        if (Convert.ToDouble(GantryRtn) > 180 && Convert.ToDouble(GantryRtn) <= 185)
                        {
                            if (GantryRtnExt != "EN")
                            {
                                msg += "\n\nThis plan is a left SC+AX plan. POST fields with angles greater than 180 should have the \"Extended\" option in the field properties is selected, but it's not.";
                                extendedcheckpassed = 0;
                            }
                        }
                        if (Convert.ToDouble(GantryRtn) <= 180 && Convert.ToDouble(GantryRtn) >= 175)
                        {
                            if (GantryRtnExt != "NN")
                            {
                                msg += "\n\nThis plan is a left SC+AX plan. POST fields with angles less than or equal to 180 should not have the \"Extended\" option in the field properties is selected, but it is.";
                                extendedcheckpassed = 0;
                            }
                        }
                    }
                }
                dataReader.Close();
            }
            if (extendedcheckpassed == 2)
            {
                row["Result"] = "Pass";
            }
            else if (extendedcheckpassed == 0)
            {
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);



            //////////////// End of tests //////////////////

            // Close connection to DB.
            try
            {
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


            // Write back current message and datatable
            SomeProperties.MsgString = msg;
            SomeProperties.MsgDataTable = table;
        }
    }
}
