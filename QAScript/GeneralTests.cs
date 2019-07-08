using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using VMS.TPS.Common.Model.API;

namespace QAScript
{
    public static class GeneralTests
    {
        public static void RunGeneralTests(Patient patient, Course course, PlanSetup plan)
        {
            //////////////////////////////////////////////
            // The main body of common code starts here //
            //////////////////////////////////////////////

            // Every new class needs to do these same first steps which is to load in the msg and the datatable from their propertes and write them back at the end of the code.
            string msg = SomeProperties.MsgString;
            DataTable table = SomeProperties.MsgDataTable;
            DataRow row;

            // Check primary ref point equals plan ID
            row = table.NewRow();
            row["Item"] = "The primary ref point equals the plan ID";
            if (plan.PrimaryReferencePoint.Id != plan.Id)
            {
                msg += "\n\nPrimary reference point \"" + plan.PrimaryReferencePoint.Id + "\" does not have the same name as the plan (\"" + plan.Id + "\").";
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check the the precribed isodose line is 100.
            row = table.NewRow();
            row["Item"] = "The precribed isodose line is 100";
            if (plan.PrescribedPercentage != 1)
            {
                msg += "\n\nThe prescribed percentage is not 100%. Please make sure this is intentional.";
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check Beam isocenters are all the same
            row = table.NewRow();
            row["Item"] = "Beam isocenters are all the same";
            var xiso = plan.Beams.First().IsocenterPosition.x;
            var yiso = plan.Beams.First().IsocenterPosition.y;
            var ziso = plan.Beams.First().IsocenterPosition.z;
            var IsoNotEqual = 0;

            var listofbeams = plan.Beams;
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsocenterPosition.x != xiso)
                {
                    IsoNotEqual = 1;
                }
                if (scan.IsocenterPosition.y != yiso)
                {
                    IsoNotEqual = 1;
                }
                if (scan.IsocenterPosition.z != ziso)
                {
                    IsoNotEqual = 1;
                }

            }
            if (IsoNotEqual == 1)
            {
                msg += "\n\nOne or more of the beams have different isocenters.";
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check that the machine is the same for all beams
            row = table.NewRow();
            row["Item"] = "The machine name is the same for all beams";
            var Machine = plan.Beams.First().ExternalBeam.Id;
            var MachineMatchIssue = 0;
            foreach (Beam scan in listofbeams)
            {
                if (scan.ExternalBeam.Id != Machine)
                {
                    MachineMatchIssue = 1;
                }
            }
            if (MachineMatchIssue == 1)
            {
                msg += "\n\nThe machine is not the same for all beams.";
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check that the jaw setting of each beam is at least 3 cm in x and y for all control points.
            row = table.NewRow();
            row["Item"] = "The jaw setting of each beam is at least 3 cm in x and y for all control points";
            foreach (Beam scan in listofbeams)
            {
                double SmallestFS = 400;
                var listofCP = scan.ControlPoints;
                foreach (ControlPoint cp in listofCP)
                {
                    double XFS;
                    double YFS;
                    double X1 = cp.JawPositions.X1;
                    double X2 = cp.JawPositions.X2;
                    double Y1 = cp.JawPositions.Y1;
                    double Y2 = cp.JawPositions.Y2;
                    GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                    if (XFS < SmallestFS)
                    {
                        SmallestFS = XFS;
                    }
                    if (YFS < SmallestFS)
                    {
                        SmallestFS = YFS;
                    }
                }
                if (SmallestFS < 30)
                {
                    msg += "\n\nField \"" + scan.Id + "\"contains an X or Y jaw setting smaller than 3 cm (at least one control point has a jaw width of " + SmallestFS / 10 + " cm).";
                    row["Result"] = "Fail";
                }
            }
            table.Rows.Add(row);

            // Check that the X Jaw setting is no greater than 20 cm for a beam of type "ARC" or "SRS ARC".
            row = table.NewRow();
            row["Item"] = "The X Jaw setting is no greater than 20 cm for a beam of type \"ARC\" or \"SRS ARC\"";
            int FoundXFSTooBig = 0;
            foreach (Beam scan in listofbeams)
            {
                double XFS = 0;
                double YFS = 0;
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    var listofCP = scan.ControlPoints;
                    foreach (ControlPoint cp in listofCP)
                    {
                        double X1 = cp.JawPositions.X1;
                        double X2 = cp.JawPositions.X2;
                        double Y1 = cp.JawPositions.Y1;
                        double Y2 = cp.JawPositions.Y2;
                        GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                        if (XFS > 200) //FS is in mm.
                        {
                            FoundXFSTooBig = 1;
                        }
                    }
                }

                if (FoundXFSTooBig == 1)
                {
                    msg += "\n\nField \"" + scan.Id.ToString() + "\" has an X jaw setting greater than 20 cm.";
                    row["Result"] = "Fail";
                    FoundXFSTooBig = 0;
                }
            }
            table.Rows.Add(row);

            // For Fields of type "ARC" and "SRS ARC", check that collimator angle is not zero
            row = table.NewRow();
            row["Item"] = "For Fields of type \"ARC\" and \"SRS ARC\", check that collimator angle is not zero";
            foreach (Beam scan in listofbeams)
            {
                int BadColAngle = 0;
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    var listofCP = scan.ControlPoints;
                    foreach (ControlPoint cp in listofCP)
                    {
                        if (cp.CollimatorAngle == 0)
                        {
                            BadColAngle = 1;
                        }
                    }
                }
                if (BadColAngle == 1)
                {
                    msg += "\n\nField \"" + scan.Id.ToString() + "\" is an arc and has a collimator setting of zero.";
                    row["Result"] = "Fail";
                    BadColAngle = 0;
                }
            }
            table.Rows.Add(row);

            // Check that SU fields have a 15x15 cm2 jaw setting and CBCT fields have a 10x10 cm2 jaw setting
            row = table.NewRow();
            row["Item"] = "Check that setup fields have a 15x15 cm2 jaw setting and the CBCT field has a 10x10 cm2 jaw setting";
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsSetupField == true)
                {
                    if (scan.Id.ToLower().Contains("cbct"))
                    {
                        double X1 = scan.ControlPoints.First().JawPositions.X1;
                        double X2 = scan.ControlPoints.First().JawPositions.X2;
                        double Y1 = scan.ControlPoints.First().JawPositions.Y1;
                        double Y2 = scan.ControlPoints.First().JawPositions.Y2;
                        double XFS;
                        double YFS;
                        GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                        if (XFS != 100 || YFS != 100)
                        {
                            msg += "\n\nThe CBCT setup field does not have a jaw setting of 10x10 cm2.";
                            row["Result"] = "Fail";
                        }
                    }
                    if (!scan.Id.ToLower().Contains("cbct"))
                    {
                        double X1 = scan.ControlPoints.First().JawPositions.X1;
                        double X2 = scan.ControlPoints.First().JawPositions.X2;
                        double Y1 = scan.ControlPoints.First().JawPositions.Y1;
                        double Y2 = scan.ControlPoints.First().JawPositions.Y2;
                        double XFS;
                        double YFS;
                        GetFieldSize(X1, X2, Y1, Y2, out XFS, out YFS);
                        if (XFS != 150 || YFS != 150)
                        {
                            msg += "\n\nThe setup field \"" + scan.Id + "\" does not have a jaw setting of 15x15 cm2.";
                            row["Result"] = "Fail";
                        }
                    }

                }
            }
            table.Rows.Add(row);

            // Check to make sure normalization is appplied
            row = table.NewRow();
            row["Item"] = "Check that some normalization is appplied to 3D plans and that RapiArc plans have the usual \"100% of the dose covers 95% of Target Volume\"";
            int hasarc = 0;
            foreach (Beam scan in listofbeams)
            {
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    hasarc = 1;
                }
            }
            if (hasarc == 1)
            {
                if (plan.PlanNormalizationMethod != "100.00% covers 95.00% of Target Volume")
                {
                    msg += "\n\nArc technique detected, but the normalization is not set to the usual \"100.00% covers 95.00% of Target Volume\". Was this intentional?";
                    row["Result"] = "Fail";
                }
            }
            if (hasarc == 0)
            {
                if (plan.PlanNormalizationMethod == "No plan normalization")
                {
                    msg += "\n\nPlan is not normalized.";
                    row["Result"] = "Fail";
                }
            }
            table.Rows.Add(row);

            // Check to make sure that RA beams alternate direction
            row = table.NewRow();
            row["Item"] = "Make sure that plans with arcs do not have all arcs sweep in the same direction as this would waste time at the delivery";
            int CW = 0;
            int CCW = 0;
            int DiffinCWvsCCW;
            foreach (Beam scan in listofbeams)
            {
                if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                {
                    if (scan.GantryDirection.ToString() == "Clockwise")
                    {
                        CW = CW + 1;
                    }
                    if (scan.GantryDirection.ToString() == "CounterClockwise")
                    {
                        CCW = CCW + 1;
                    }
                }
            }
            DiffinCWvsCCW = CW - CCW;
            if (DiffinCWvsCCW < -1.5 || DiffinCWvsCCW > 1.5)
            {
                msg += "\n\nThe difference in the number of clockwise arcs compared to counterclockwise arcs is high. The treatment time may not be optimal.";
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check that SU images have the correct gantry angle based on field name and patient orientation.
            row = table.NewRow();
            row["Item"] = "Check that SU images have the correct gantry angle based on field name and patient orientation. It checks for \"ANT\", \"POST\", \"LT\", \"RT\" and \"CBCT\" then looks for the correct gantry angle. It also checks that the collimator angle is zero.";
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsSetupField == true)
                {
                    if (scan.ControlPoints.First().CollimatorAngle != 0)
                    {
                        msg += "\n\nFor the setup field \"" + scan.Id + "\", the colimator angle is not zero.";
                        row["Result"] = "Fail";
                    }
                    if (scan.Id.ToLower().Contains("cbct"))
                    {
                        if (scan.ControlPoints.First().GantryAngle != 0)
                        {
                            msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                            row["Result"] = "Fail";
                        }
                    }

                    //HFS
                    if (plan.TreatmentOrientation.ToString() == "HeadFirstSupine")
                    {
                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                row["Result"] = "Fail";
                            }
                        }

                    }
                    //FFS
                    if (plan.TreatmentOrientation.ToString() == "FeetFirstSupine")
                    {
                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                row["Result"] = "Fail";
                            }
                        }

                    }
                    //HFP
                    if (plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                    {

                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            msg += "\n\nPatient is prone, did you mean to lable the 'ANT Setup' field 'POST Setup' instead?";
                            row["Result"] = "Fail";
                        }
                        if (scan.Id.ToLower().Contains("post"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                row["Result"] = "Fail";
                            }
                        }

                    }
                    //FFP
                    if (plan.TreatmentOrientation.ToString() == "FeetFirstProne")
                    {
                        if (scan.Id.ToLower().Contains("ant"))
                        {
                            msg += "\n\nPatient is prone, did you mean to lable the 'ANT Setup' field 'POST Setup' instead?";
                            row["Result"] = "Fail";
                        }
                        if (scan.Id.ToLower().Contains("post"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 0)
                            {
                                msg += "\n\nFor the setup field \"" + scan.Id + "\", the Gantry angle is not zero.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("rt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 270)
                            {
                                msg += "\n\nFor the RT setup field \"" + scan.Id + "\", the Gantry angle is not 270.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("lt"))
                        {
                            if (scan.ControlPoints.First().GantryAngle != 90)
                            {
                                msg += "\n\nFor the LT setup field \"" + scan.Id + "\", the Gantry angle is not 90.";
                                row["Result"] = "Fail";
                            }
                        }
                    }
                }
            }
            table.Rows.Add(row);

            // Check that field names begin with the correct numbers based on the name of the plan
            row = table.NewRow();
            row["Item"] = "The field names begin with the correct numbers based on the name of the plan(\"1.1\" for field 1, plan 1 etc.)";
            if (plan.Id.StartsWith("FP1")) //FP stands for final plan
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("1.") == false)
                        {
                            msg += "\n\nPlan FP1 expects fields to start with '1.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("2.") == false)
                        {
                            msg += "\n\nPlan FP2 expects fields to start with '2.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP3"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("3.") == false)
                        {
                            msg += "\n\nPlan FP3 expects fields to start with '3.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP4"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("4.") == false)
                        {
                            msg += "\n\nPlan FP4 expects fields to start with '4.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("FP5"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("5.") == false)
                        {
                            msg += "\n\nPlan FP5 expects fields to start with '5.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P1")) //M represents a mod plan of plan 1
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-1.") == false)
                        {
                            msg += "\n\nPlan M1P1 expects fields to start with 'M1-1.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M2P1"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M2-1.") == false)
                        {
                            msg += "\n\nPlan M2P1 expects fields to start with 'M2-1.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M3P1"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M3-1.") == false)
                        {
                            msg += "\n\nPlan M3P1 expects fields to start with 'M3-1.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-2.") == false)
                        {
                            msg += "\n\nPlan M1P2 expects fields to start with 'M1-2.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M2P2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M2-2.") == false)
                        {
                            msg += "\n\nPlan M2P2 expects fields to start with 'M2-2.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M3P2"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M3-2.") == false)
                        {
                            msg += "\n\nPlan M3P2 expects fields to start with 'M3-2.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P3"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-3.") == false)
                        {
                            msg += "\n\nPlan M1P3 expects fields to start with 'M1-3.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M2P3"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M2-3.") == false)
                        {
                            msg += "\n\nPlan M2P3 expects fields to start with 'M2-3.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P4"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-4.") == false)
                        {
                            msg += "\n\nPlan M1P4 expects fields to start with 'M1-4.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            if (plan.Id.StartsWith("M1P5"))
            {
                foreach (Beam scan in listofbeams)
                {
                    if (scan.IsSetupField == false)
                    {
                        if (scan.Id.StartsWith("M1-5.") == false)
                        {
                            msg += "\n\nPlan M1P5 expects fields to start with 'M1-5.'";
                            row["Result"] = "Fail";
                        }
                    }
                }
            }
            table.Rows.Add(row);

            // Check names of fields (LAO/RAO etc) against gantry angles
            row = table.NewRow();
            row["Item"] = "Basic checks on field naming: i) Arcs have \"ARC\" in the name ii)Static fields have orientation \"LT\", \"ANT\", \"RAO\" etc. included. Checks for all four patient orientations.";
            foreach (Beam scan in listofbeams)
            {
                if (scan.IsSetupField == false)
                {
                    if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
                    {
                        if (scan.Id.ToLower().Contains("arc") == false)
                        {
                            msg += "\n\nFields of type \"ARC\" (\"" + scan.Id + "\") should contain \"ARC\" in the field name.";
                            row["Result"] = "Fail";
                        }
                    }
                    if (scan.Technique.Id.Equals("STATIC"))
                    {
                        if (plan.TreatmentOrientation.ToString() == "HeadFirstSupine")
                        {
                            if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("post"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    row["Result"] = "Fail";
                                }
                            }
                        }
                        if (plan.TreatmentOrientation.ToString() == "FeetFirstSupine")
                        {
                            if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("post"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    row["Result"] = "Fail";
                                }
                            }
                        }
                        if (plan.TreatmentOrientation.ToString() == "HeadFirstProne")
                        {
                            if (scan.Id.ToLower().Contains("post"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    row["Result"] = "Fail";
                                }
                            }
                        }
                        if (plan.TreatmentOrientation.ToString() == "FeetFirstProne")
                        {
                            if (scan.Id.ToLower().Contains("post"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 0)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry of zero.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 0 || scan.ControlPoints.First().GantryAngle > 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than zero, but less than 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 90)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 90.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("lao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 90 || scan.ControlPoints.First().GantryAngle > 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 90, but less than 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("ant"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 180)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 180.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rao"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 180 || scan.ControlPoints.First().GantryAngle > 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 180, but less than 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rt"))
                            {
                                if (scan.ControlPoints.First().GantryAngle != 270)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry angle of 270.";
                                    row["Result"] = "Fail";
                                }
                            }
                            if (scan.Id.ToLower().Contains("rpo"))
                            {
                                if (scan.ControlPoints.First().GantryAngle < 270 || scan.ControlPoints.First().GantryAngle > 360)
                                {
                                    msg += "\n\nField: \"" + scan.Id + "\" does not have a gantry greater than 270, but less than 360.";
                                    row["Result"] = "Fail";
                                }
                            }
                        }
                    }

                }
            }
            table.Rows.Add(row);

            // Check to make sure that heterogeneity corrections are on
            row = table.NewRow();
            row["Item"] = "Heterogeneity corrections are on";
            if (plan.PhotonCalculationOptions.ContainsKey("HeterogeneityCorrection"))
            {
                string value = plan.PhotonCalculationOptions["HeterogeneityCorrection"];
                if (value.Equals("OFF"))
                {
                    msg += "\n\nHeterogeneity corrections are OFF.";
                    row["Result"] = "Fail";
                }
            }
            table.Rows.Add(row);

            // Check MU > 5
            row = table.NewRow();
            row["Item"] = "All beams have an MU setting of greater than 5 MU";
            foreach (Beam scan in listofbeams)
            {
                if (scan.Meterset.Value < 5)
                {
                    msg += "\n\nField \"" + scan.Id + "\" has fewer than 5 MU.";
                    row["Result"] = "Fail";
                }
            }
            table.Rows.Add(row);

            // Check AAA version
            row = table.NewRow();
            row["Item"] = "For photon plans, the dose calculation algorithm is \"AAA_11031\"";
            if (plan.PhotonCalculationModel != "AAA_11031")
            {
                msg += "\n\nThe photon calculation model is expected to be: AAA_11031, but is instead: " + plan.PhotonCalculationModel;
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Check EMC version
            row = table.NewRow();
            row["Item"] = "For electron plans, the dose calculation algorithm is \"EMC_11031\"";
            bool isElectron = false;
            foreach (Beam scan in listofbeams)
            {
                if (scan.EnergyModeDisplayName.Equals("6E") || scan.EnergyModeDisplayName.Equals("9E") || scan.EnergyModeDisplayName.Equals("12E") || scan.EnergyModeDisplayName.Equals("16E") || scan.EnergyModeDisplayName.Equals("20E"))
                {
                    // Set electron flag to true
                    isElectron = true;
                }
            }

            if (isElectron == true)
            {
                if (plan.ElectronCalculationModel != "EMC_11031")
                {
                    msg += "\n\nThe electron calculation model is not set to: \"EMC_11031\"";
                    row["Result"] = "Fail";
                }
            }
            table.Rows.Add(row);

            //Check for couch structure
            row = table.NewRow();
            row["Item"] = "The couch structure is correct for the selected treatment unit and has the correct HU values";
            if (Machine.Contains("TB"))
            {
                var foundcouch = false;
                var wrongcouch = false;
                var listofstructures = plan.StructureSet.Structures;
                foreach (Structure scan in listofstructures)
                {
                    if (scan.Name.Contains("Exact IGRT Couch Top"))
                    {
                        foundcouch = true;
                        bool structHU = scan.GetAssignedHU(out double huValue);
                        if (scan.Id.ToLower().Contains("interior"))
                        {
                            if (huValue != -960)
                            {
                                msg += "\n\nVarian couch structure found, but the interior HU is set to " + huValue + " when -960 was expected.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("surface"))
                        {
                            if (huValue != -700)
                            {
                                msg += "\n\nVarian couch structure found, but the exterior HU is set to " + huValue + " when -700 was expected.";
                                row["Result"] = "Fail";
                            }
                        }
                    }
                    if (scan.Name.Contains("BrainLAB"))
                    {
                        foundcouch = true;
                        wrongcouch = false;
                    }
                }
                if (foundcouch == false)
                {
                    msg += "\n\nVarian IGRT couch structure missing.";
                    row["Result"] = "Fail";
                }
                if (wrongcouch == true)
                {
                    msg += "\n\nWrong couch structure detected.";
                    row["Result"] = "Fail";
                }
            }

            if (Machine.Contains("STX"))
            {
                var foundcouch = false;
                var wrongcouch = false;
                var listofstructures = plan.StructureSet.Structures;
                foreach (Structure scan in listofstructures)
                {
                    if (scan.Name.Contains("BrainLAB"))
                    {
                        foundcouch = true;
                        bool structHU = scan.GetAssignedHU(out double huValue);
                        if (scan.Id.ToLower().Contains("interior"))
                        {
                            if (huValue != -850)
                            {
                                msg += "\n\nBrainLAB couch structure found, but the interior HU is set to " + huValue + " when -850 was expected.";
                                row["Result"] = "Fail";
                            }
                        }
                        if (scan.Id.ToLower().Contains("surface"))
                        {
                            if (huValue != -300)
                            {
                                msg += "\n\nBrainLAB couch structure found, but the exterior HU is set to " + huValue + " when -300 was expected.";
                                row["Result"] = "Fail";
                            }
                        }
                    }
                    if (scan.Name.Contains("Exact IGRT Couch Top"))
                    {
                        foundcouch = true;
                        wrongcouch = true;
                    }
                }
                if (foundcouch == false)
                {
                    msg += "\n\nBrainLAB couch structure missing, make sure patient is in the \"U-frame\" mask.";
                    row["Result"] = "Fail";
                }
                if (wrongcouch == true)
                {
                    msg += "\n\nWrong couch structure detected.";
                    row["Result"] = "Fail";
                }
            }
            table.Rows.Add(row);

            //Checks name of CT against name of structure set
            row = table.NewRow();
            row["Item"] = "The name of the structure set matches the name of the CT";
            if (plan.StructureSet.Id != plan.StructureSet.Image.Id)
            {
                msg += "\n\nCT name and structure set name do not match.";
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            //Checks grid size is 0.1 cm for lung SBRT (Dose per fx > 8 and FS < 10 cm with "lung" in plan name) and brain cases.
            row = table.NewRow();
            row["Item"] = "The calculation grid size is 0.1 cm for lung SBRT (Dose per fx > 9 and FS < 10 cm with \"lung\" in plan name) and brain cases";

            var foundoptics = false; // Brain (optic structures found in structure set)
            var listofstructures2 = plan.StructureSet.Structures;
            foreach (Structure scan in listofstructures2)
            {
                if (scan.Id.ToLower().Contains("optic") && (scan.IsEmpty == false))
                {
                    foundoptics = true;
                }
            }
            if (foundoptics == true)
            {
                if (plan.PhotonCalculationOptions.ContainsKey("CalculationGridSizeInCM"))
                {
                    string value = plan.PhotonCalculationOptions["CalculationGridSizeInCM"];
                    if (value != "0.1")
                    {
                        msg += "\n\nThe plan contains optic structures, but the calculation grid size is not 0.1 cm. Is this intentional?";
                        row["Result"] = "Fail";
                    }
                }
            }

            if (plan.Id.ToLower().Contains("lung")) // Lung SBRT
            {
                if (plan.UniqueFractionation.PrescribedDosePerFraction.Dose > 9)
                {
                    if (plan.PhotonCalculationOptions.ContainsKey("CalculationGridSizeInCM"))
                    {
                        string value = plan.PhotonCalculationOptions["CalculationGridSizeInCM"];
                        if (value != "0.1")
                        {
                            int FSlessthanten = 0;
                            foreach (Beam scan in listofbeams)
                            {
                                if (scan.IsSetupField == false)
                                {
                                    var listofCP = scan.ControlPoints;
                                    foreach (ControlPoint cp in listofCP)
                                    {
                                        double X1 = cp.JawPositions.X1;
                                        double X2 = cp.JawPositions.X2;
                                        double Y1 = cp.JawPositions.Y1;
                                        double Y2 = cp.JawPositions.Y2;
                                        GetFieldSize(X1, X2, Y1, Y2, out double XFS, out double YFS);
                                        if (XFS < 100) //FS is in mm.
                                        {
                                            FSlessthanten = 1;
                                        }
                                        if (YFS < 100)
                                        {
                                            FSlessthanten = 1;
                                        }
                                    }
                                }
                            }
                            if (FSlessthanten == 1)
                            {
                                msg += "\n\nThe plan might be a lung SBRT case but the calculation grid size is not 0.1 cm. Is this intentional?";
                                row["Result"] = "Fail";
                            }
                        }
                    }
                }
            }
            table.Rows.Add(row);

            // Test for FFDA (tray present)
            row = table.NewRow();
            row["Item"] = "Electron beams all have a tray ID defined";
            var traysmissing = 0;
            foreach (Beam scan in listofbeams)
            {
                if (scan.EnergyModeDisplayName.Contains("E"))
                {
                    if (scan.Trays.Count() == 0)
                    {
                        if (traysmissing == 0)
                        {
                            traysmissing = 1;
                        }

                    }
                }
            }
            if (traysmissing == 1)
            {
                msg += "\n\nThe tray ID is not set in one or more electron block properties";
                row["Result"] = "Fail";
            }
            table.Rows.Add(row);

            // Test the grid size for electron plans. If it has 6 MeV then it should be 0.1 cm. Otherwise 0.15 cm.
            row = table.NewRow();
            row["Item"] = "The calculation grid size for electron plans is  0.1 cm if the plan has 6 MeV otherwise it should be 0.15 cm.";

            var foundelectrons = 0;
            var found6MeV = 0;
            foreach (Beam scan in listofbeams)
            {
                if (scan.EnergyModeDisplayName.Contains("E"))
                {
                    foundelectrons = 1;
                }
                if (scan.EnergyModeDisplayName.Contains("6E"))
                {
                    found6MeV = 1;
                }
            }

            if (foundelectrons.Equals(1) && found6MeV.Equals(1))
            {
                if (plan.ElectronCalculationOptions.ContainsKey("CalculationGridSizeInCM"))
                {
                    string value = plan.ElectronCalculationOptions["CalculationGridSizeInCM"];
                    if (value != "0.10")
                    {
                        msg += "\n\nThe plan is an electron plan with a 6 MeV beam. The calculation grid size should be 0.1 cm. It is currently set to " + value + " cm.";
                        row["Result"] = "Fail";
                    }
                }
            }
            if (foundelectrons.Equals(1) && found6MeV.Equals(0))
            {
                if (plan.ElectronCalculationOptions.ContainsKey("CalculationGridSizeInCM"))
                {
                    string value = plan.ElectronCalculationOptions["CalculationGridSizeInCM"];
                    if (value != "0.15")
                    {
                        msg += "\n\nThe plan is an electron plan with only energies greater than 6 MeV. The calculation grid size should be 0.15 cm. It is currently set to " + value + " cm.";
                        row["Result"] = "Fail";
                    }
                }
            }
            table.Rows.Add(row);

            // Look for arcs that sweep in the wrong direction such that they enter through the far side of the patient.
            // I'm just going to leave this one for now as it's quite a bit of work for something you can just see by looking at the plan.

            //row = table.NewRow();
            //row["Item"] = "xyz";
            // Get the laterial location of BODY center of mass
            //double bodyxcenter; // This is from the DICOM origin, not the Eclipse user origin. Same for the beam iso through. Probably just subtract the two and look for control points on each side. Need to program orientation though.
            //foreach (Structure str in listofstructures2)
            //{
            //    if (str.Id.Contains("LENS_R"))
            //    {
            //        bodyxcenter = str.CenterPoint.x;
            //    }
            //}

            //foreach (Beam scan in listofbeams)
            //{
            //    if (scan.Technique.Id.Equals("ARC") || scan.Technique.Id.Equals("SRS ARC"))
            //    {
            //        var listofCP = scan.ControlPoints;
            //        foreach (ControlPoint cp in listofCP)
            //        {

            //        }
            //    }
            //}




            // Write back current message and datatable
            SomeProperties.MsgString = msg;
            SomeProperties.MsgDataTable = table;

        }

        // Some method to get the field size.
        public static void GetFieldSize(double X1, double X2, double Y1, double Y2, out double XFS, out double YFS)
        {
            XFS = X2 - X1;
            YFS = Y2 - Y1;
        }
    }
}
