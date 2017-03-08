﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ProteoformSuiteInternal;
using System.IO;

namespace ProteoformSuite
{
    public partial class TopDown : Form
    {
        private static Color[] colors = new Color[20];
        private static List<string> mods = new List<string>();

        public TopDown()
        {
            InitializeComponent();
        }

        public void load_dgv()
        {
            DisplayUtility.FillDataGridView(dgv_TD_proteoforms, Lollipop.proteoform_community.topdown_proteoforms.Where(p => !p.targeted).ToList());
            load_colors();
            load_ptm_colors();
        }

        public void load_topdown()
        {
            if (ready_for_top_down())
            {
                run_the_gamut();
            }
            else
            {
                MessageBox.Show("Go back and load in top-down results.");
            }
            cmbx_td_or_e_proteoforms.DataSource = new BindingList<string>() { "TopDown Proteoforms", "Experimental Proteoforms" };
            cmbx_td_or_e_proteoforms.SelectedIndex = 0;
        }

        private bool ready_for_top_down()
        {
            return (Lollipop.top_down_hits.Count == 0 && Lollipop.input_files.Any(f => f.purpose == Purpose.TopDown));
        }

        private bool ptm_list_ready()
        {
            return (Lollipop.input_files.Where(f => f.purpose == Purpose.PtmList).Count() > 0);
        }
        private void run_the_gamut()
        {
            if (Lollipop.top_down_hits.Count == 0) read_in_topdown();
            if (Lollipop.top_down_hits.Count > 0)
            {
                Lollipop.proteoform_community.topdown_proteoforms = new TopDownProteoform[0];
                clear_lists();
                Lollipop.aggregate_td_hits();
                if (Lollipop.proteoform_community.topdown_proteoforms.Where(p => p.targeted).Count() > 0) bt_targeted_td_relations.Enabled = true;
                tb_tdProteoforms.Text = Lollipop.proteoform_community.topdown_proteoforms.Where(p => !p.targeted).ToArray().Length.ToString();
                load_dgv();
                bt_td_relations.Enabled = true;
            }
        }

        private void read_in_topdown()
        {
            if (ready_for_top_down())
            {
                if (ptm_list_ready())
                {
                    Lollipop.read_in_td_hits();
                }

                else
                {
                    MessageBox.Show("Go back to Load Results and load in a ptm list.");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Go back to Load Results and load in top-down results.");
                return;
            }
        }

        private void bt_load_td_Click(object sender, EventArgs e)
        {
            run_the_gamut();
        }

        private void clear_lists()
        {
            Lollipop.td_relations.Clear();
            foreach (Proteoform p in Lollipop.proteoform_community.experimental_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.etd);
            foreach (Proteoform p in Lollipop.proteoform_community.theoretical_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.ttd);
            foreach (Proteoform p in Lollipop.proteoform_community.experimental_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.ettd);
            foreach (Proteoform p in Lollipop.proteoform_community.topdown_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.ettd);
            foreach (Proteoform p in Lollipop.proteoform_community.topdown_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.etd);
            foreach (Proteoform p in Lollipop.proteoform_community.topdown_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.ttd);
            dgv_TD_proteoforms.DataSource = null;
            dgv_TD_proteoforms.Rows.Clear();
        }

        private void bt_td_relations_Click(object sender, EventArgs e)
        {
            if (Lollipop.proteoform_community.experimental_proteoforms.Length > 0)
            {
                clear_lists();
                Lollipop.make_td_relationships();
                tb_exp_proteoforms.Text = Lollipop.proteoform_community.experimental_proteoforms.Where(exp => exp.etd_match_count == 0 & exp.accepted).ToList().Count.ToString();
                load_dgv();
                bt_check_fragmented_e.Enabled = true;
            }
            else
            {
                MessageBox.Show("Go back and aggregate experimental proteoforms.");
            }
        }

        private void bt_targeted_td_relations_Click(object sender, EventArgs e)
        {
            Lollipop.td_relations.Where(r => ((TopDownProteoform)r.connected_proteoforms[0]).targeted).ToList().Clear();
            foreach (Proteoform p in Lollipop.proteoform_community.experimental_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.ettd);
            foreach (Proteoform p in Lollipop.proteoform_community.topdown_proteoforms) p.relationships.RemoveAll(r => r.relation_type == ProteoformComparison.ettd);
            Lollipop.make_targeted_td_relationships();
        }

        private void dgv_TD_proteoforms_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                dgv_TD_family.DataSource = null;
                if (this.dgv_TD_proteoforms.Rows[e.RowIndex].DataBoundItem is TopDownProteoform)
                {
                    TopDownProteoform p = (TopDownProteoform)this.dgv_TD_proteoforms.Rows[e.RowIndex].DataBoundItem;
                    if (p.relationships != null)
                    {
                        DisplayUtility.FillDataGridView(dgv_TD_family, p.relationships);  //show T-TD and E-TD relationsj
                    }
                    get_proteoform_sequence(p);
                }

                else
                {
                    ExperimentalProteoform exp = (ExperimentalProteoform)this.dgv_TD_proteoforms.Rows[e.RowIndex].DataBoundItem;
                    if (exp.family != null)
                    {
                        //DisplayUtility.FillDataGridView(dgv_TD_family, exp.family.relations.Where(r => r.relation_type == ProteoformComparison.etd || r.relation_type == ProteoformComparison.et || r.relation_type == ProteoformComparison.ettd).ToList());  //show E-TD or ET relations (identified)
                        DisplayUtility.FillDataGridView(dgv_TD_family, exp.relationships.Where(r => r.relation_type == ProteoformComparison.etd || r.relation_type == ProteoformComparison.et || r.relation_type == ProteoformComparison.ettd).ToList());  //show E-TD or ET relations (identified)

                    }
                }
            }
        }

        private void get_proteoform_sequence(TopDownProteoform p)
        {
            rtb_sequence.Text = p.sequence + "\n";
            rtb_sequence.SelectionStart = 0;
            rtb_sequence.SelectionLength = p.sequence.Length;
            rtb_sequence.SelectionColor = Color.Black;
            rtb_sequence.ZoomFactor = 3;

            int length = p.sequence.Length + 1;

            foreach (Ptm ptm in p.ptm_list)
            {
                int i;
                try { i = mods.IndexOf(ptm.modification.id); }
                catch { i = 0; } //just make color blue if > 20 unique PTMs
                Color color = colors[i];

                rtb_sequence.SelectionStart = ptm.position;
                rtb_sequence.SelectionLength = 1;
                rtb_sequence.SelectionColor = color;
            }

            foreach (string description in p.ptm_list.Select(ptm => ptm.modification.id).Distinct())
            {
                int i;
                try { i = mods.IndexOf(description); }
                catch { i = 0; }
                Color color = colors[i];

                rtb_sequence.AppendText("\n" + description);
                rtb_sequence.SelectionStart = length;
                rtb_sequence.SelectionLength = description.Length + 1;
                rtb_sequence.SelectionColor = colors[i];
                length += description.Length + 1;
            }
        }

        private static void load_colors()
        {
            colors[0] = Color.Blue;
            colors[1] = Color.Red;
            colors[2] = Color.Orange;
            colors[3] = Color.Green;
            colors[4] = Color.Purple;
            colors[5] = Color.Gold;
            colors[6] = Color.DeepPink;
            colors[7] = Color.ForestGreen;
            colors[8] = Color.DarkBlue;
            colors[9] = Color.DarkOrange;
            colors[10] = Color.OrangeRed;
            colors[11] = Color.Magenta;
            colors[12] = Color.DeepSkyBlue;
            colors[13] = Color.DarkSlateGray;
            colors[14] = Color.DarkSalmon;
            colors[15] = Color.DarkTurquoise;
            colors[16] = Color.Aqua;
            colors[17] = Color.DarkOliveGreen;
            colors[18] = Color.Fuchsia;
            colors[19] = Color.HotPink;
        }

        private static void load_ptm_colors()
        {
            List<Ptm> ptm = new List<Ptm>();
            foreach (TopDownProteoform p in Lollipop.proteoform_community.topdown_proteoforms)
            {
                ptm.AddRange(p.ptm_list);
            }
            IEnumerable<string> unique_ptm = ptm.Select(p => p.modification.id).Distinct();
            mods = unique_ptm.ToList();
        }

        private void TopDown_Load(object sender, EventArgs e)
        {

        }


        private void cmbx_td_or_e_proteoforms_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbx_td_or_e_proteoforms.SelectedItem.ToString() == "TopDown Proteoforms" && Lollipop.proteoform_community.topdown_proteoforms.Where(p => !p.targeted).ToList().Count > 0)
                DisplayUtility.FillDataGridView(dgv_TD_proteoforms, Lollipop.proteoform_community.topdown_proteoforms.Where(p => !p.targeted).ToList());
            else if (cmbx_td_or_e_proteoforms.SelectedItem.ToString() == "Experimental Proteoforms" && Lollipop.proteoform_community.experimental_proteoforms.Length > 0)
                DisplayUtility.FillDataGridView(dgv_TD_proteoforms, Lollipop.proteoform_community.experimental_proteoforms.Where(exp => exp.accepted).ToList());
            // DisplayUtility.FillDataGridView(dgv_TD_proteoforms, Lollipop.proteoform_community.experimental_proteoforms.Where(exp => exp.accepted && exp.etd_match_count == 0).ToList());

        }

        private void bt_check_fragmented_e_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Please select raw files.");
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "My Thermo Raw Files";
            openFileDialog1.Filter = "Raw Files (*.raw) | *.raw";
            openFileDialog1.Multiselect = true;

            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                RawFileReader.check_fragmented_experimentals(enter_input_files(openFileDialog1.FileNames, new List<string> { ".raw" }, Purpose.RawFile));
                dgv_TD_proteoforms.Refresh();
                MessageBox.Show("Successfully checked if experimentals were fragmented.");
            }
            else return;
        }


        private List<InputFile> enter_input_files(string[] files, IEnumerable<string> acceptable_extensions, Purpose purpose)
        {
            List<InputFile> files_added = new List<InputFile>();
            foreach (string enteredFile in files)
            {
                if (!Lollipop.input_files.Where(f => f.purpose == purpose).Any(f => f.complete_path == enteredFile))
                {
                    InputFile file = new InputFile(enteredFile, purpose);
                    Lollipop.input_files.Add(file);
                    files_added.Add(file);
                }
            }
            return files_added;
        }
    }
}