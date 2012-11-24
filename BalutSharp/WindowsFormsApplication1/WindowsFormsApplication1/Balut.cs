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


namespace Balut
{
    public partial class BalutDialog : Form
    {
        int[] Dadi;
        int[] puntiMano, puntiBonus;
        int[,] puntiSalvati; // -1 = punto non assegnato
        int tiriRimasti, maniRimaste, granTotale = 0, numBalut = 0;
        bool puntoAnnotabile;
        bool inizioGioco;
        bool nascondiPunti = false;
        Random rng = new Random();
        List<Label> colonnaPunti = new List<Label>();
        List<Label> colonnaTotali = new List<Label>();
        List<Tripla> tabellaPunteggi = new List<Tripla>(); // compatibile .NET 2.0
        string nomeGiocatore = "";
        string filePunti = "balut.txt";
        Timer tm = new Timer();
        int animCount;
        int[] animRow;

        public BalutDialog()
        {
            InitializeComponent();
        }

        private void nuovoGioco()
        {
            puntiMano = new int[7];
            puntiBonus = new int[7];
            puntiSalvati = new int[7, 5];
            resetDadiPuntiTiri();
            maniRimaste = 28;
            puntoAnnotabile = false;
            inizioGioco = true;
            for (int i = 0; i < puntiMano.Length; i++)
            {
                puntiBonus[i] = 0;
                puntiMano[i] = -1;
            }
            for (int i = 0; i < 7; i++)
                for (int j=0; j < 5; j++)
                    puntiSalvati[i, j] = -1;
            aggiornaTabellone();
        }

        private void resetDadiPuntiTiri()
        {
            tiriRimasti = 3;
            Dadi = new int[] { -1, -1, -1, -1, -1 };
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
            int totale = int.Parse(label20.Text);

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

        private bool rigaCompleta(int i)
        {
            for (int b = 0; b < 4; b++)
                if (puntiSalvati[i, b] < 0)
                    return false;
            return true;
        }

        private void creaColonnaTotali()
        {
            string[] labels = { "Subtotale", "Bonus", "TOTALE" };
            int x=215, y = 90, Height = 20;

            for (int i = 0; i < labels.Length; i++)
            {
                y = 72;
                x += 55;
                for (int j = 0; j < 8; j++)
                {
                    y += Height - 1;
                    Label la1;
                    la1 = new Label();
                    la1.TextAlign = ContentAlignment.MiddleRight;
                    la1.FlatStyle = FlatStyle.Flat;
                    la1.Location = new Point(x, y);
                    la1.Size = new Size(56, Height);
                    if (j==0)
                        la1.Text = labels[i];
                    la1.BackColor = Color.LightGray;
                    la1.BorderStyle = BorderStyle.FixedSingle;
                    if (j != 0)
                        colonnaTotali.Add(la1);
                    Controls.Add(la1);
                }
            }
        }
		
        private void aggiornaManiTiri()
        {
            label2.Text = "Tiri rimasti: " + tiriRimasti.ToString() + " - Combinazioni rimaste: " + maniRimaste.ToString();
        }

        private void aggiornaTabellone()
        {
            int subtot, grantot=0;

            for (int i = 0; i < puntiMano.Length; i++) // le 7 combinazioni
            {
                subtot = 0;

                for (int j = 0; j < 5; j++) // le 5 colonne
                {
                    if (i == 6 && j == 4) // ignora il JP Balut
                        continue;

                    if ((puntiMano[i] == -1 || nascondiPunti) && puntiSalvati[i, j] == -1)
                    {
                        colonnaPunti[i * 5 + j].Text = "";
                        continue;
                    }

                    if (puntiSalvati[i, j] == -1 && !nascondiPunti)
                    {
                        if (j == 4) // col. JP: si aggiorna solo se soddisfa i requisiti minimi
                        {
                            // BUG CORRETTO: passando da un Full di 6 con 4 (26p) a 3x6+5+1 (24p)
                            // il JP su Full e Choice non veniva cancellato!!!
                            colonnaPunti[i * 5 + j].Text = ""; // cancella eventuali residui

                            if (rigaCompleta(i)) // 1) riga non completa
                                continue;

                            switch (i) // 2) punteggio minimo
                            {
                                case 0: // 4
                                    if (puntiMano[i] < 16) continue;
                                    break;
                                case 1: // 5
                                    if (puntiMano[i] < 20) continue;
                                    break;
                                case 2: // 6
                                    if (puntiMano[i] < 24) continue;
                                    break;
                                case 3: // Scale
                                    if (puntiMano[i] < 20) continue;
                                    break;
                                case 4: // Full
                                    if (puntiMano[i] < 22) continue;
                                    break;
                                case 5: // Choice
                                    if (puntiMano[i] < 25) continue;
                                    break;
                                case 6: // Balut: non c'è JP!
                                    continue;
                            }
                        }

                        colonnaPunti[i * 5 + j].Text = puntiMano[i].ToString();
                        colonnaPunti[i * 5 + j].ForeColor = Color.Red;
                        colonnaPunti[i * 5 + j].Font = new Font(colonnaPunti[i * 5 + j].Font, FontStyle.Bold);
                    }

                    if (j == 4) // non somma la colonna Jackpot
                        continue;
                    subtot += puntiSalvati[i, j];
                    if (puntiSalvati[i, j] == -1)
                        subtot++;
                }

                colonnaTotali[i].Text = subtot.ToString();

                switch (i)
                {
                    case 0: // Bonus 4
                        if (subtot > 51)
                            puntiBonus[i] = 2;
                        if (puntiSalvati[i, 4] > -1)
                            if (puntiBonus[i] > 0)
                                puntiBonus[i] = 4;
                            else
                                puntiBonus[i] = -4;
                        colonnaTotali[7].Text = puntiBonus[i].ToString();
                        colonnaTotali[14].Text = (subtot + puntiBonus[i]).ToString();
                        break;
                    case 1: // Bonus 5
                        if (subtot > 64)
                            puntiBonus[i] = 2;
                        if (puntiSalvati[i, 4] > -1)
                            if (puntiBonus[i] > 0)
                                puntiBonus[i] = 4;
                            else
                                puntiBonus[i] = -4;
                        colonnaTotali[8].Text = puntiBonus[i].ToString();
                        colonnaTotali[15].Text = (subtot + puntiBonus[i]).ToString();
                        break;
                    case 2: // Bonus 6
                        if (subtot > 77)
                            puntiBonus[i] = 2;
                        if (puntiSalvati[2, 4] > -1)
                            if (puntiBonus[i] > 0)
                                puntiBonus[i] = 4;
                            else
                                puntiBonus[i] = -4;
                        colonnaTotali[9].Text = puntiBonus[i].ToString();
                        colonnaTotali[16].Text = (subtot + puntiBonus[i]).ToString();
                        break;
                    case 3: // Bonus Scale
                        if (puntiSalvati[i,0] > 0 &&
                            puntiSalvati[i,1] > 0 &&
                            puntiSalvati[i,2] > 0 &&
                            puntiSalvati[i,3] > 0)
                            puntiBonus[i] = 4;
                        if (puntiSalvati[i, 4] > -1)
                            if (puntiBonus[i] > 0)
                                puntiBonus[i] = 8;
                            else
                                puntiBonus[i] = -8;
                        colonnaTotali[10].Text = puntiBonus[i].ToString();
                        colonnaTotali[17].Text = (subtot + puntiBonus[i]).ToString();
                        break;
                    case 4: // Bonus Full
                        if (puntiSalvati[i,0] > 0 &&
                            puntiSalvati[i,1] > 0 &&
                            puntiSalvati[i,2] > 0 &&
                            puntiSalvati[i,3] > 0)
                            puntiBonus[i] = 3;
                        if (puntiSalvati[i, 4] > -1)
                            if (puntiBonus[i] > 0)
                                puntiBonus[i] = 6;
                            else
                                puntiBonus[i] = -6;
                        colonnaTotali[11].Text = puntiBonus[i].ToString();
                        colonnaTotali[18].Text = (subtot + puntiBonus[i]).ToString();
                        break;
                    case 5: // Bonus Choice
                        if (subtot > 99)
                            puntiBonus[i] = 2;
                        if (puntiSalvati[i, 4] > -1)
                            if (puntiBonus[i] > 0)
                                puntiBonus[i] = 4;
                            else
                                puntiBonus[i] = -4;
                        colonnaTotali[12].Text = puntiBonus[i].ToString();
                        colonnaTotali[19].Text = (subtot + puntiBonus[i]).ToString();
                        break;
                    case 6: // Bonus Balut
                        int bonusBalut = 0;
                        switch (numBalut)
                        { 
                            case 1:
                                bonusBalut = 3;
                                break;
                            case 2:
                                bonusBalut = 8;
                                break;
                            case 3:
                                bonusBalut = 12;
                                break;
                            case 4:
                                bonusBalut = 16;
                                break;
                        }
                        colonnaTotali[13].Text = bonusBalut.ToString();
                        colonnaTotali[20].Text = (subtot + bonusBalut).ToString();
                        grantot += bonusBalut;
                        break;
                }

                grantot += subtot + puntiBonus[i];
            }

            aggiornaManiTiri();

            int bonusTot = -7 + (grantot / 50) * 1;
            label19.Text = bonusTot.ToString();
            granTotale = grantot + bonusTot;
            label20.Text = granTotale.ToString();
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

            PictureBox[] pbx = { pictureBox1, pictureBox2, pictureBox3,
                                 pictureBox4, pictureBox5 };

            for (int i = 0; i < Dadi.Length; i++)
            {
                int j = Dadi[i];
                if (j < 0)
                    pbx[i].Image = bmp[j * -1 - 1];
                else
                    pbx[i].Image = bmp[j - 1 + 6];
            }
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

        private int trovaScala(int[] dadi)
        {
            for (int i = 1; i < Dadi.Length; i++)
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
            //switch (tiriRimasti)
            //{
            //    case 2:
            //        Dadi = new int[] { 6, 6, 6, 4, 4 };
            //        break;
            //    case 1:
            //        Dadi = new int[] { 6, 6, 6, 5, 1 };
            //        break;
            //}
            int[] dadiPerValore = new int[6];

            for (int i = 0; i < Dadi.Length; i++)
                dadiPerValore[Math.Abs(Dadi[i]) - 1]++;

            // Quattro
            puntiMano[0] = dadiPerValore[3]*4;
            // Cinque
            puntiMano[1] = dadiPerValore[4]*5;
            // Sei
            puntiMano[2] = dadiPerValore[5]*6;
            // Scale (di 5: 1...5 o 2...6)
            puntiMano[3] = (trovaScala(dadiPerValore) > 0) ? sommaDadi(dadiPerValore) : 0;
            // Full (3x + 2y)
            int Tris = trovaMultipli(dadiPerValore, 3)[0];
            int Coppia = trovaMultipli(dadiPerValore, 2)[1];
            // Poiché il Tris contiene una Coppia, o sono diversi, o 3x + 2x
            puntiMano[4] = ((Tris > 0 && Coppia > 0)) ? sommaDadi(dadiPerValore) : 0;
            // Choice
            puntiMano[5] = sommaDadi(dadiPerValore);
            // Balut
            puntiMano[6] = (trovaMultipli(dadiPerValore, 5)[0] > 0) ? 20 + sommaDadi(dadiPerValore) : 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] labels = {"Quattro","Cinque","Sei","Scale","Full","Somma","BALUT"};
            int x=93, y=90, Height=20;

            for (int i = 0; i < labels.Length; i++)
            {
                x = 93;
                y += Height - 1;
                for (int j = 0; j < 5; j++)
                {
                    if (j == 4 && i == 6) // non disegna la casella JP x Balut
                        continue;
                    Label la1, la2;
                    la1 = new Label();
                    la2 = new Label();
                    la2.TextAlign = ContentAlignment.MiddleRight;
                    la1.FlatStyle = la2.FlatStyle = FlatStyle.Flat;
                    la1.Location = new Point(10, y);
                    la2.Location = new Point(x, y);
                    la1.Size = new Size(84, Height);
                    la2.Size = new Size(35, Height);
                    la1.Text = labels[i];
                    la1.BackColor = la2.BackColor = Color.White;
                    la1.BorderStyle = la2.BorderStyle = BorderStyle.FixedSingle;
                    la2.Click += new System.EventHandler(this.colonnaPunti_Click);
                    la2.Cursor = Cursors.Hand;
                    colonnaPunti.Add(la2);
                    if (j == 0)
                        Controls.Add(la1);
                    Controls.Add(la2);
                    x += 34;
                }
            }
            creaColonnaTotali();
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
            inizioGioco = false;

            if (maniRimaste == 0)
                nuovoGioco();

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

            int x = i / 5, y = i % 5;

            if (puntiSalvati[x, y] > -1) // impedisce il click su vuota
                return;

            // Si può cliccare su JP solo se è stata riempita
            // (=in presenza dei requisiti)
            if (y == 4 && la.Text == "")
                return;

            if (x == 6 && puntiMano[x] > 0)
                numBalut++; // annotato un nuovo Balut valido

            puntiSalvati[x, y] = puntiMano[x];
            puntoAnnotabile = false;
            inizioGioco = true; // impedisce di cliccare i dadi
            la.Text = puntiMano[x].ToString();
            la.ForeColor = Color.Black;
            la.Font = new Font(la.Font, FontStyle.Regular);
            resetDadiPuntiTiri();
            aggiornaTabellone();
            button1.Enabled = true;
            if (y != 4) // Il JP non fa parte delle mani obbligatorie
                maniRimaste--;
            if (maniRimaste == 0)
                salvaPunti();
            aggiornaManiTiri();
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            List<PictureBox> pbx = new List<PictureBox>{ pictureBox1, pictureBox2, pictureBox3,
                                 pictureBox4, pictureBox5 };

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

