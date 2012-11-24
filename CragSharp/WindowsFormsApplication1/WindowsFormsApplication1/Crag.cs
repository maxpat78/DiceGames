using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;

// Per essere compatibile con .NET 2.0, Tuple diventa questa struttura...
public struct Tripla {
    public int Punti;
    public string Nome;
    public string Data;

    public Tripla(int x, string y, string z)
    {
        Punti = x;
        Nome = z;
        Data = y;
    }
    public static int ComparaTripla(Tripla a, Tripla b)
    {
        if (a.Punti == b.Punti) return 0;
        if (a.Punti > b.Punti) return 1;
        return -1;
    }
};

namespace Crag
{
    public partial class CragDialog : Form
    {
        // Un dado diventa positivo se va salvato, altrimenti è negativo
        int[] Dadi;
        int[] puntiMano;
        int[] puntiSalvati; // -1 = punto non assegnato
        int tiriRimasti, maniRimaste;
        bool puntoAnnotabile;
        bool inizioGioco;
        bool nascondiPunti = false;
        Random rng = new Random();
        List<Label> colonnaPunti = new List<Label>();
        List<Tripla> tabellaPunteggi = new List<Tripla>(); // compatibile .NET 2.0
        string nomeGiocatore = "";
        string filePunti = "crag.txt";
        Timer tm = new Timer();
        int animCount;
        int[] animRow;

        public CragDialog()
        {
            InitializeComponent();
        }

        private void nuovoGioco()
        {
            puntiMano = new int[13];
            puntiSalvati = new int[13];
            maniRimaste = 13;
            puntoAnnotabile = false;
            inizioGioco = true;
            for (int i = 0; i < puntiSalvati.Length; i++)
                puntiSalvati[i] = -1;
            resetDadiPuntiTiri();
            aggiornaTabellone();
        }

        private void resetDadiPuntiTiri()
        {
            tiriRimasti = 2;
            Dadi = new int[] { -1, -1, -1 };
            for (int i = 0; i < puntiMano.Length; i++)
                puntiMano[i] = -1;
        }

        private void caricaPunti()
        {
            if (!File.Exists(filePunti))
                return;

            System.IO.StreamReader sr = new StreamReader(filePunti);
            string riga;

            while ( (riga=sr.ReadLine()) != null )
            {
                string[] s = riga.Split(';');
                // punti;data;nome
                tabellaPunteggi.Add(new Tripla(int.Parse(s[0]), s[1], s[2]));
            }
        }

        private void salvaPunti()
        {
            int totale = int.Parse(colonnaPunti[13].Text);

            if (textBox1.Text == "")
                textBox1.Text = "???";

            tabellaPunteggi.Add(new Tripla(totale, DateTime.Now.ToShortDateString(), textBox1.Text));
            tabellaPunteggi.Sort(Tripla.ComparaTripla);
            tabellaPunteggi.Reverse();

            if (tabellaPunteggi.Count > 10)
                tabellaPunteggi.RemoveRange(10, tabellaPunteggi.Count - 10);

            StreamWriter sw = new StreamWriter(filePunti);

            foreach (Tripla t in tabellaPunteggi)
            {
                sw.WriteLine(t.Punti.ToString() + ";" + t.Data + ";" + t.Nome);
            }

            sw.Close();
        }

        private void animaDadi()
        {
            animCount = 0;

            for (int i = 0; i < animRow.Length; i++)
            {
                if (Dadi[i] > 0) // il massimo num. di giri è calcolato sui dadi che devono girare!
                    continue;
                animRow[i] = rng.Next(12, 40); // num. di giri del singolo dado
                animCount = Math.Max(animCount, animRow[i]); // numero massimo di giri
            }

            tm.Start();
        }

        private void aggiornaTabellone()
        {
            for (int i = 0; i < puntiSalvati.Length; i++)
            {
                if ((puntiMano[i] == -1 || nascondiPunti) && puntiSalvati[i] == -1)
                {
                    colonnaPunti[i].Text = "";
                    continue;
                }

                if (puntiSalvati[i] == -1 && !nascondiPunti)
                {
                    colonnaPunti[i].Text = puntiMano[i].ToString();
                    colonnaPunti[i].ForeColor = Color.Red;
                    colonnaPunti[i].Font = new Font(colonnaPunti[i].Font, FontStyle.Bold);
                }

            }

            int subtotale=0;
            for (int i = 0; i < puntiSalvati.Length; i++)
                subtotale += (puntiSalvati[i] > 0) ? puntiSalvati[i] : 0;
            colonnaPunti[13].Text = subtotale.ToString();
            aggiornaManiTiri();
        }

        private void aggiornaManiTiri()
        {
            label2.Text = "Tiri rimasti: " + tiriRimasti.ToString() + " - Combinazioni rimaste: " + maniRimaste.ToString();
        }

        private void aggiornaDadi()
        {
            Bitmap[] bmp = { Properties.Resources.D1,
                           Properties.Resources.D2,
                           Properties.Resources.D3,
                           Properties.Resources.D4,
                           Properties.Resources.D5,
                           Properties.Resources.D6,
                           Properties.Resources.D1f,
                           Properties.Resources.D2f,
                           Properties.Resources.D3f,
                           Properties.Resources.D4f,
                           Properties.Resources.D5f,
                           Properties.Resources.D6f };

            PictureBox[] pbx = { pictureBox1, pictureBox2, pictureBox3 };

            for (int i = 0; i < Dadi.Length; i++)
            {
                int j = Dadi[i];
                if (j < 0)
                    pbx[i].Image = bmp[j * -1 - 1];
                else
                    pbx[i].Image = bmp[j - 1 + 6];
            }
        }

        private int[] trovaMultipli(int[] dadi, int n)
        {
            int[] multipli = new int[3];
            int j = 0;

            for (int i = dadi.Length - 1; i >= 0; i--)
                if (dadi[i] >= n)
                    multipli[j++] = i+1; // multiplo più alto

            return multipli;
        }

        private void calcolaPuntiMano()
        {
            int[] dadiPerValore = new int[6];

            for (int i = 0; i < Dadi.Length; i++)
                dadiPerValore[Math.Abs(Dadi[i]) - 1]++;

            // Assi
            puntiMano[0] = dadiPerValore[0];
            // Due
            puntiMano[1] = dadiPerValore[1]*2;
            // Tre
            puntiMano[2] = dadiPerValore[2]*3;
            // Quattro
            puntiMano[3] = dadiPerValore[3]*4;
            // Cinque
            puntiMano[4] = dadiPerValore[4]*5;
            // Sei
            puntiMano[5] = dadiPerValore[5]*6;
            // Scala dispari (1,3,5)
            puntiMano[6] = (dadiPerValore[0] == 1 && dadiPerValore[2] == 1 && dadiPerValore[4] == 1) ? 20 : 0;
            // Scala pari (2,4,6)
            puntiMano[7] = (dadiPerValore[1] == 1 && dadiPerValore[3] == 1 && dadiPerValore[5] == 1) ? 20 : 0;
            // Scala 1-3
            puntiMano[8] = (dadiPerValore[0] == 1 && dadiPerValore[1] == 1 && dadiPerValore[2] == 1) ? 20 : 0;
            // Scala 4-6
            puntiMano[9] = (dadiPerValore[3] == 1 && dadiPerValore[4] == 1 && dadiPerValore[5] == 1) ? 20 : 0;
            // Tris (3x)
            puntiMano[10] = (trovaMultipli(dadiPerValore, 3)[0] > 0) ? 25 : 0;
            // Tredici (2-5-6 o 3-4-6)
            puntiMano[11] = ( (dadiPerValore[1] == 1 && dadiPerValore[4] == 1 && dadiPerValore[5] == 1) ||
            (dadiPerValore[2] == 1 && dadiPerValore[3] == 1 && dadiPerValore[5] == 1) )? 26 : 0;
            // Crag (1-6-6, 3-5-5 o 5-4-4)
            puntiMano[12] = ((trovaMultipli(dadiPerValore, 2)[0] == 6 && dadiPerValore[0] == 1) ||
                (trovaMultipli(dadiPerValore, 2)[0] == 5 && dadiPerValore[2] == 1) ||
                (trovaMultipli(dadiPerValore, 2)[0] == 4 && dadiPerValore[4] == 1)) ? 50 : 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] labels = {"Uno","Due","Tre","Quattro","Cinque","Sei",
                               "Scala dispari","Scala pari","Scala inferiore", "Scala superiore",
                               "Tris","Tredici","Crag","TOTALE"};
            int y=40, Height=20;
            
            for (int i = 0; i < labels.Length; i++)
            {
                Label la1 = new Label();
                Label la2 = new Label();
                la2.TextAlign = ContentAlignment.MiddleRight;
                la1.FlatStyle = la2.FlatStyle = FlatStyle.Flat;
                la1.Location = new Point(10, y);
                la2.Location = new Point(93, y);
                la1.Size = new Size(84, Height);
                la2.Size = new Size(30, Height);
                la1.Text = labels[i];
                la1.BackColor = la2.BackColor = Color.White;
                la1.BorderStyle = la2.BorderStyle = BorderStyle.FixedSingle;
                if (i == 13)
                {
                    la1.Font = new Font(la1.Font, FontStyle.Bold);
                    la1.BackColor = la2.BackColor = Color.LightGray;
                    la2.ForeColor = Color.Black;
                    la2.Font = new Font(la2.Font, FontStyle.Regular);
                }
                else // cliccabile se non è una casella di riepilogo
                {
                    la2.Click += new System.EventHandler(this.colonnaPunti_Click);
                    la2.Cursor = Cursors.Hand;
                }
                colonnaPunti.Add(la2);
                Controls.Add(la1);
                Controls.Add(la2);
                y += Height - 1;
            }

            nuovoGioco();
            caricaPunti();

            tm.Tick += new EventHandler(tm_Tick);
            tm.Interval = 50;
            animRow = new int[Dadi.Length];
        }

        void tm_Tick(object sender, EventArgs e)
        {
            if (--animCount < 0)
            {
                tm.Stop();
                calcolaPuntiMano();
                aggiornaTabellone();
                puntoAnnotabile = true;
                return;
            }

            for (int i = 0; i < Dadi.Length; i++)
                if (Dadi[i] < 0 && animRow[i]-- > 0)
                    Dadi[i] = -1 * rng.Next(1, 7);
            aggiornaDadi();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (maniRimaste == 0)
                nuovoGioco();
			
            inizioGioco = false;

            bool nessunRilancio = true;

            for (int i = 0; i < Dadi.Length; i++)
                if (Dadi[i] < 0)
                    nessunRilancio = false;

            if (nessunRilancio) return;

            if (--tiriRimasti == 0)
                button1.Enabled = false;

            aggiornaManiTiri();
            
            animaDadi();
        }

        private void colonnaPunti_Click(object sender, EventArgs e)
        {
            if (!puntoAnnotabile)
                return;

            int i = colonnaPunti.FindIndex(
                delegate(Label l)
                {
                    return (Label)sender == l;
                });

            if (i < 0) return;

            Label la = ((Label)sender);

            if (puntiSalvati[i] > -1)
                return;

            puntiSalvati[i] = puntiMano[i];
            puntoAnnotabile = false;
            inizioGioco = true; // impedisce di cliccare i dadi
            la.Text = puntiMano[i].ToString();
            la.ForeColor = Color.Black;
            la.Font = new Font(la.Font, FontStyle.Regular);
            resetDadiPuntiTiri();
            aggiornaTabellone();
            button1.Enabled = true;
            if (--maniRimaste == 0)
                salvaPunti();
            aggiornaManiTiri();
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            List<PictureBox> pbx = new List<PictureBox>{ pictureBox1, pictureBox2, pictureBox3 };

            int i = pbx.FindIndex(
                delegate(PictureBox pbd)
                {
                    return (PictureBox)sender == pbd;
                });

            if (i < 0 || inizioGioco) return;
            Dadi[i] *= -1;
            aggiornaDadi();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            nuovoGioco();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form fm = new Form1();
            fm.Show();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            nomeGiocatore = textBox1.Text;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Form fm = new Form2();
            int y = 5, Height = 20;

            for (int i = 0; i < tabellaPunteggi.Count; i++)
            {
                // Posizione
                Label la1 = new Label();
                la1.Text = (i+1).ToString();
                la1.Location = new Point(10, y);
                la1.Size = new Size(25, Height);

                // Nome 
                Label la2 = new Label();
                la2.Text = tabellaPunteggi[i].Nome;
                la2.Location = new Point(34, y);
                la2.Size = new Size(120, Height);

                // Data
                Label la3 = new Label();
                la3.Text = tabellaPunteggi[i].Data;
                la3.Location = new Point(153, y);
                la3.Size = new Size(75, Height);

                // Punti
                Label la4 = new Label();
                la4.Text = tabellaPunteggi[i].Punti.ToString();
                la4.Location = new Point(227, y);
                la4.Size = new Size(75, Height);
                la4.TextAlign = ContentAlignment.TopRight;

                la1.BackColor = la2.BackColor = la3.BackColor = la4.BackColor = Color.White;
                la1.BorderStyle = la2.BorderStyle = la3.BorderStyle = la4.BorderStyle = BorderStyle.FixedSingle;
   
                fm.Controls.Add(la1);
                fm.Controls.Add(la2);
                fm.Controls.Add(la3);
                fm.Controls.Add(la4);
                y += Height - 1;
            }
            fm.Show();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            nascondiPunti = !nascondiPunti;
            aggiornaTabellone();
        }
    }
}

