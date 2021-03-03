using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Windows.Forms;

namespace ADGroups
{
    public partial class FrmMain : Form
    {
        private int _groupsRetrieved;

        private List<Group> _groups;

        public FrmMain()
        {
            InitializeComponent();
        }

        private void btnGetGroups_Click(object sender, EventArgs e)
        {
            // Reset all to default
            dataGridView.DataSource = null;
            _groupsRetrieved = 0;
            btnGetGroups.Enabled = false;

            var domain = txtDomain.Text.Trim();

            if (!bgWorkerGroups.IsBusy)
                bgWorkerGroups.RunWorkerAsync(domain);
        }

        private void bgWorkerGroups_DoWork(object sender, DoWorkEventArgs e)
        {
            var domain = (string)e.Argument;

            if (_groups == null)
                _groups = new List<Group>();

            // Create principal context
            var principalContext = new PrincipalContext(ContextType.Domain, domain);
            //principalContext.ValidateCredentials("Administrator", "Pass99");

            using (principalContext)
            {
                bgWorkerGroups.ReportProgress(0, "Please wait! Searching all groups...");

                using (var groupPrincipal = new GroupPrincipal(principalContext))
                {
                    using (var principalSearcher = new PrincipalSearcher(groupPrincipal))
                    {
                        var principals = principalSearcher.FindAll().OfType<GroupPrincipal>();
                        var groupPrincipals = principals as GroupPrincipal[] ?? principals.ToArray();

                        foreach (var gp in groupPrincipals)
                        {
                            // Check for duplicates and change friendly name accordingly
                            var dupCount = _groups.Count(@group => @group.DistinguishedName.Equals(gp.DistinguishedName));

                            if (dupCount.Equals(0))
                            {
                                _groups.Add(new Group(gp.SamAccountName, gp.DistinguishedName));
                            }
                            else
                            {
                                var samAccountName = string.Format("{0}{1}", gp.SamAccountName, dupCount);
                                _groups.Add(new Group(samAccountName, gp.DistinguishedName));
                            }

                            _groupsRetrieved++;
                            bgWorkerGroups.ReportProgress(0, string.Format("Retrieved group '{0}' ({1}/{2})", gp.Name, _groupsRetrieved, groupPrincipals.Count()));
                        }
                    }
                }

                // Sort and assign end results
                _groups.Sort((a, b) => String.Compare(a.FriendlyName, b.FriendlyName, StringComparison.OrdinalIgnoreCase));
                e.Result = _groups;
            }
        }

        private void bgWorkerGroups_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lblStatus.Text = (string)e.UserState;
        }

        private void bgWorkerGroups_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                lblStatus.Text = "Error was reported.";
                MessageBox.Show("Error:- " + e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (_groupsRetrieved > 0)
                {
                    var message = string.Format(_groupsRetrieved.Equals(1) ? "{0} group was imported successfully!" : "{0} groups were imported successfully!", _groupsRetrieved);
                    lblStatus.Text = message;

                    dataGridView.DataSource = e.Result;
                }
                else
                    lblStatus.Text = "No group found!";
            }

            btnGetGroups.Enabled = true;
        }

        private void lblStatus_MouseHover(object sender, EventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void lblStatus_MouseLeave(object sender, EventArgs e)
        {
            Cursor = Cursors.Default;
        }

        private void lblStatus_Click(object sender, EventArgs e)
        {
            Process.Start("http://lnkd.in/bJ3eyHY");
        }
    }

    /// <summary>
    /// Represents Group of AD.
    /// </summary>
    public class Group
    {
        public Group(string samName, string dn)
        {
            FriendlyName = samName;
            DistinguishedName = dn;
        }

        public string FriendlyName { get; set; }
        public string DistinguishedName { get; set; }
    }
}
