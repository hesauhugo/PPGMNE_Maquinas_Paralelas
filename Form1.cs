using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace MaquinasParalelas
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void cmd_calcular_Click(object sender, EventArgs e)
        {
            UPMSV1 obj = new UPMSV1();
            obj.LerArquivos(txt_caminho.Text);
            obj.CarregaVariaveis();
            obj.Restricao01();
            obj.Restricao02();
            obj.Restricao03();
            obj.Restricao04();
            obj.Restricao05();
            obj.Restricao06();
            obj.Restricao07();
            obj.Restricao08();
            obj.model.Write("UPMS.lp");
            obj.model.Optimize();
            obj.model.Write("UPMSSolucao.sol");

        }
    }
}
