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

namespace PowerYahtzee
{
    public partial class PowerYahtzeeDialog : Form
    {
        int[] Dadi;
        int[] puntiMano;
        int[] puntiSalvati; // -1 = punto non assegnato
        int tiriRimasti, maniRimaste;
        bool puntoAnnotabile;
        bool doppiaAnnotazione;
        bool inizioGioco;
        bool nascondiPunti = false;
        Random rng = new Random();
        List<Label> colonnaPunti = new List<Label>();
        List<Tripla> tabellaPunteggi = new List<Tripla>(); // compatibile .NET 2.0
        string nomeGiocatore = "";
        string filePunti = "pyahtzee.txt";
        Timer tm = new Timer();
        int animCount;
        int[] animRow;

        public PowerYahtzeeDialog()
        {
            InitializeComponent();
        }

        private void nuovoGioco()
        {
            // Il sesto dado è il dado Power: 1...3: punti x1 ... x3;
            // 4:Double 5:Freeze 6:Power
            puntiMano = new int[24];
            puntiSalvati = new int[24];
            resetDadiPuntiTiri();
            maniRimaste = 21;
            puntoAnnotabile = false;
            doppiaAnnotazione = false;
            inizioGioco = true;
            for (int i = 0; i < puntiSalvati.Length; i++)
                puntiSalvati[i] = -1;
            aggiornaTabellone();
            button1.Enabled = true;
        }

        private void resetDadiPuntiTiri()
        {
            tiriRimasti = 3;
            Dadi = new int[] { -1, -1, -1, -1, -1, -1 };
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
            int totale = int.Parse(colonnaPunti[23].Text);

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
            for (int i = 0; i < puntiSalvati.Length-1; i++)
            {
                if (i == 7 || i == 8 || i == 24)
                    continue;

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
            for (int i = 0; i < 7; i++)
                subtotale += (puntiSalvati[i] > 0) ? puntiSalvati[i] : 0;
            colonnaPunti[7].Text = subtotale.ToString();

            int bonus1 = 0;
            if (subtotale >= 150) bonus1 =  50;
            if (subtotale >= 200) bonus1 = 100;
            if (subtotale >= 300) bonus1 = 200;
            subtotale += bonus1;
            colonnaPunti[8].Text = bonus1.ToString();

            for (int i = 9; i < 24; i++)
                subtotale += (puntiSalvati[i] > 0) ? puntiSalvati[i] : 0;
            colonnaPunti[23].Text = subtotale.ToString();

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

            Bitmap[] bmp2 = { Properties.Resources.D1X,
                           Properties.Resources.D2X,
                           Properties.Resources.D3X,
                           Properties.Resources.D22,
                           Properties.Resources.DST,
                           Properties.Resources.DPO,
                           Properties.Resources.D1Xf,
                           Properties.Resources.D2Xf,
                           Properties.Resources.D3Xf,
                           Properties.Resources.D22f,
                           Properties.Resources.DSTf,
                           Properties.Resources.DPOf };

            PictureBox[] pbx = { pictureBox1, pictureBox2, pictureBox3,
                                 pictureBox4, pictureBox5 };

            for (int i = 0; i < Dadi.Length-1; i++)
            {
                int j = Dadi[i];
                if (j < 0)
                    pbx[i].Image = bmp[j * -1 - 1];
                else
                    pbx[i].Image = bmp[j - 1 + 6];
            }

            if (Dadi[5] < 0)
                pictureBox7.Image = bmp2[Dadi[5] * -1 - 1];
            else
                pictureBox7.Image = bmp2[Dadi[5] -1 + 6];
        }

        private int sommaDadi(int[] dadi)
        {
            int somma = 0;
            for (int i = 0; i < 6; i++)
                somma += dadi[i] * (i + 1);
            return somma;
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

        private bool trovaScala4(int[] dadi)
        {
            if (dadi[2] < 1 || dadi[3] < 1) // no dadi comuni 3 & 4
                return false;

            if (dadi[0] > 0 && dadi[1] > 0 || dadi[1] > 0 && dadi[4] > 0 || dadi[4] > 0 && dadi[5] > 0)
                return true;

            return false;
        }

        private int trovaScala(int[] dadi)
        {
            for (int i = 1; i < 5; i++)
                if (dadi[i] == 0) // no seq. comune 2-3-4-5
                    return 0;
            if (dadi[0] == 1)
                return 5;
            if (dadi[5] == 1)
                return 6;
            return 0;
        }

        private void calcolaPuntiMano()
        {
            //Dadi = new int[] { 5,5,5,5,5,6 };

            int[] dadiPerValore = new int[6];

            for (int i = 0; i < Dadi.Length - 1; i++)
                dadiPerValore[Math.Abs(Dadi[i]) - 1]++;

            int Power = Math.Abs(Dadi[5]);

            int isYahtzee = trovaMultipli(dadiPerValore, 5)[0];

            // Assi
            puntiMano[0] = dadiPerValore[0];
            // Due
            puntiMano[1] = dadiPerValore[1] * 2;
            // Tre
            puntiMano[2] = dadiPerValore[2] * 3;
            // Quattro
            puntiMano[3] = dadiPerValore[3] * 4;
            // Cinque
            puntiMano[4] = dadiPerValore[4] * 5;
            // Sei
            puntiMano[5] = dadiPerValore[5] * 6;

            // Choice (migliore dei precedenti)
            int Max = 0;
            for (int i = 0; i < 6; i++)
                Max = (dadiPerValore[i]*(i+1) > Max) ? dadiPerValore[i]*(i+1) : Max;
            puntiMano[6] = Max;

            // N.B. nel regolamento ufficiale non è chiaro se, nella Doppia Coppia e nel Full,
            // Coppie e Tris possano essere uguali: qui si assume di no, dato che non si fa
            // menzione del "Jolly"

            // Doppia Coppia (2x+2y)
            puntiMano[9] = (trovaMultipli(dadiPerValore, 2)[1] > 0) ? sommaDadi(dadiPerValore) : 0;
            // 1° e 2° Tris (3x)
            puntiMano[10] = puntiMano[11] = (trovaMultipli(dadiPerValore, 3)[0] > 0) ? sommaDadi(dadiPerValore) : 0;
            // 1° e 2° Poker (4x)
            puntiMano[12] = puntiMano[13] = (trovaMultipli(dadiPerValore, 4)[0] > 0) ? sommaDadi(dadiPerValore) : 0;
            // Full (3x + 2y)
            int Tris = trovaMultipli(dadiPerValore, 3)[0];
            int Coppia = trovaMultipli(dadiPerValore, 2)[1];
            // Poiché il Tris contiene una Coppia, o sono diversi, o 3x + 2x = Yahtzee
            puntiMano[14] = (Tris > 0 && Coppia > 0)? 25 : 0;
            // 1a e 2a Scala di 4 (1...4, 2...5, 3...6)
            puntiMano[15] = puntiMano[16] = (trovaScala4(dadiPerValore)) ? 30 : 0;
            // 1a e 2a Scala di 5 (1...5 o 2...6)
            puntiMano[17] = puntiMano[18] = (trovaScala(dadiPerValore) > 0) ? 40 : 0;
            // Chance
            puntiMano[19] = sommaDadi(dadiPerValore);
            // 1° Yahtzee
            puntiMano[20] = (isYahtzee > 0) ? 50 : 0;
            // 2° Yahtzee
            // NB: se esce 2+2, dobbiamo annotare un 2°Y valido!!!
            puntiMano[21] = (isYahtzee > 0) ? 100 : 0;
            // 3° Yahtzee
            puntiMano[22] = (isYahtzee > 0) ? 150 : 0;

            // Anche Double e Power raddoppiano e triplicano
            if (Power == 4) Power = 2;
            if (Power == 6) Power = 3;

            if (Power < 4)
                for (int i = 0; i < 23; i++)
                    puntiMano[i] *= Power;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] labels = {"Assi","Due","Tre","Quattro","Cinque","Sei","Uno a scelta",
                               "Subtotale","Bonus",
                               "Doppia coppia","1° Tris","2° Tris","1° Poker","2° Poker",
                               "Full","1° Scala corta","2° Scala corta","1° Scala lunga","2° Scala lunga","Chance",
                               "1° Yahtzee","2° Yahtzee","3° Yahtzee","TOTALE"};
            int y = 5, Height = 20;

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
                if (i == 7 || i == 8 || i == 23)
                {
                    la1.Font = new Font(la1.Font, FontStyle.Bold);
                    la1.BackColor = la2.BackColor = Color.LightGray;
                    la2.ForeColor = Color.Black;
                    la2.Font = new Font(la2.Font, FontStyle.Regular);
                }
                else
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

            if (Math.Abs(Dadi[5]) == 5) // Power: Freeze (fine turno)
                tiriRimasti = 1;

            if (--tiriRimasti <= 0)
                button1.Enabled = false;

            if (Math.Abs(Dadi[5]) == 6 && tiriRimasti == 0)
            { // Power: Triple Power (3° rilancio)
                button1.Enabled = true;
                Dadi[5] = 6;
                tiriRimasti = 0;
            }

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

            // Se azzeriamo uno Yahtzee, occorre cominciare dall'ultimo
            // Se lo annotiamo, dal primo
            if ((i == 20 || i == 21) && puntiMano[i] == 0 && puntiSalvati[22] < 0)
                i = 22;
            if (i == 20 && puntiMano[i] == 0 && puntiSalvati[21] < 0)
                i = 21;
            if ((i == 21 || i == 22) && puntiMano[i] > 0 && puntiSalvati[20] < 0)
                i = 20;
            if (i == 22 && puntiMano[i] > 0 && puntiSalvati[21] < 0)
                i = 21;

            puntiSalvati[i] = puntiMano[i];
            puntoAnnotabile = false;
            inizioGioco = true; // impedisce di cliccare i dadi
            colonnaPunti[i].Text = puntiMano[i].ToString();
            colonnaPunti[i].ForeColor = Color.Black;
            colonnaPunti[i].Font = new Font(la.Font, FontStyle.Regular);
            if (Math.Abs(Dadi[5]) == 4 && !doppiaAnnotazione) // Power == Double
            {
                puntoAnnotabile = true;
                doppiaAnnotazione = true;
                if (--maniRimaste > 0)
                    return;
                else
                    salvaPunti();
            }
            doppiaAnnotazione = false;
            resetDadiPuntiTiri();
            aggiornaTabellone();
            button1.Enabled = true;
            if (--maniRimaste == 0)
                salvaPunti();
            aggiornaManiTiri();
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            List<PictureBox> pbx = new List<PictureBox>{ pictureBox1, pictureBox2, pictureBox3,
                                 pictureBox4, pictureBox5, pictureBox7 };

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

