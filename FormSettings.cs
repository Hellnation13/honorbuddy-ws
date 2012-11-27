using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace com.peec.webservice
{
    public partial class FormSettings : Form
    {
        public FormSettings()
        {
            InitializeComponent();
        }


        private void FormSettings_Load(object sender, EventArgs e)
        {
            pgSettings.SelectedObject = WSSettings.Instance;
        }

        private void pgSettings_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (pgSettings.SelectedObject != null && pgSettings.SelectedObject is WSSettings)
                ((WSSettings)pgSettings.SelectedObject).Save();
        }

        private void f_secretKey_TextChanged(object sender, EventArgs e)
        {

        }

        private void FormSettings_Load_1(object sender, EventArgs e)
        {

        }

    }
}
